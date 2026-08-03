Imports System.IO
Imports System.IO.Compression
Imports System.Diagnostics
Imports System.Net
Imports System.Net.Http
Imports System.Reflection
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.Json
Imports System.Threading

Friend NotInheritable Class AvailableUpdate
    Public Property CurrentVersion As SemanticVersion
    Public Property LatestVersion As SemanticVersion
    Public Property Release As GitHubRelease
    Public Property PackageAsset As GitHubReleaseAsset
    Public Property ChecksumAsset As GitHubReleaseAsset
End Class

Friend NotInheritable Class StagedUpdate
    Public Property Version As SemanticVersion
    Public Property StagingDirectory As String
    Public Property PayloadDirectory As String
    Public Property ExecutableName As String
End Class

Friend NotInheritable Class GitHubUpdateService
    Private Const RepositoryOwner As String = "brucemurphy"
    Private Const RepositoryName As String = "DISM-Lab"
    Private Const ExecutableName As String = "DISM Lab.exe"
    Private Const ReleaseAssetPrefix As String = "DISM-Lab-v"
    Private Shared ReadOnly LatestReleaseUri As New Uri($"https://api.github.com/repos/{RepositoryOwner}/{RepositoryName}/releases/latest")
    Private Shared ReadOnly Client As HttpClient = CreateHttpClient()

    Public Function GetCurrentVersion() As SemanticVersion
        Dim assemblyVersion = Assembly.GetEntryAssembly()?.GetName().Version
        If assemblyVersion Is Nothing Then
            Throw New InvalidOperationException("Unable to determine the current application version.")
        End If

        Return New SemanticVersion(assemblyVersion.Major, assemblyVersion.Minor, Math.Max(assemblyVersion.Build, 0))
    End Function

    Public Async Function CheckForUpdateAsync(cancellationToken As System.Threading.CancellationToken) As Task(Of AvailableUpdate)
        Using response = Await Client.GetAsync(LatestReleaseUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            If response.StatusCode = HttpStatusCode.NotFound Then
                Return Nothing
            End If

            response.EnsureSuccessStatusCode()

            Using responseStream = Await response.Content.ReadAsStreamAsync(cancellationToken)
                Dim release = Await JsonSerializer.DeserializeAsync(Of GitHubRelease)(responseStream, cancellationToken:=cancellationToken)
                If release Is Nothing OrElse release.IsDraft OrElse release.IsPrerelease Then
                    Return Nothing
                End If

                Dim latestVersion As SemanticVersion
                If Not SemanticVersion.TryParse(release.TagName, latestVersion) Then
                    Throw New InvalidDataException($"The latest GitHub release tag '{release.TagName}' is not a supported semantic version.")
                End If

                Dim currentVersion = GetCurrentVersion()
                If latestVersion.CompareTo(currentVersion) <= 0 Then
                    Return Nothing
                End If

                Dim packageName = $"{ReleaseAssetPrefix}{latestVersion}-win-x64.zip"
                Dim checksumName = packageName & ".sha256"
                Dim assets = If(release.Assets, New List(Of GitHubReleaseAsset)())
                Dim packageAsset = assets.FirstOrDefault(Function(asset) String.Equals(asset.Name, packageName, StringComparison.OrdinalIgnoreCase))
                Dim checksumAsset = assets.FirstOrDefault(Function(asset) String.Equals(asset.Name, checksumName, StringComparison.OrdinalIgnoreCase))
                If packageAsset Is Nothing OrElse checksumAsset Is Nothing Then
                    Throw New InvalidDataException($"Release {release.TagName} does not contain the required portable ZIP and SHA-256 checksum assets.")
                End If

                ValidateDownloadUri(packageAsset.DownloadUrl)
                ValidateDownloadUri(checksumAsset.DownloadUrl)

                Return New AvailableUpdate With {
                    .CurrentVersion = currentVersion,
                    .LatestVersion = latestVersion,
                    .Release = release,
                    .PackageAsset = packageAsset,
                    .ChecksumAsset = checksumAsset
                }
            End Using
        End Using
    End Function

    Public Async Function DownloadAndStageAsync(update As AvailableUpdate,
                                                progress As IProgress(Of String),
                                                cancellationToken As System.Threading.CancellationToken) As Task(Of StagedUpdate)
        ArgumentNullException.ThrowIfNull(update)

        Dim stagingDirectory = Path.Combine(Path.GetTempPath(), "DISM-Lab", "Updates", $"v{update.LatestVersion}-{Guid.NewGuid():N}")
        Dim packagePath = Path.Combine(stagingDirectory, update.PackageAsset.Name)
        Dim checksumPath = Path.Combine(stagingDirectory, update.ChecksumAsset.Name)
        Dim payloadDirectory = Path.Combine(stagingDirectory, "payload")

        Try
            Directory.CreateDirectory(stagingDirectory)
            progress?.Report("Downloading update package...")
            Await DownloadFileAsync(update.PackageAsset.DownloadUrl, packagePath, cancellationToken)
            Await DownloadFileAsync(update.ChecksumAsset.DownloadUrl, checksumPath, cancellationToken)

            progress?.Report("Verifying SHA-256 checksum...")
            Await VerifyChecksumAsync(packagePath, checksumPath, cancellationToken)

            progress?.Report("Preparing update files...")
            Directory.CreateDirectory(payloadDirectory)
            Await ExtractArchiveSafelyAsync(packagePath, payloadDirectory, cancellationToken)

            Dim stagedExecutable = Path.Combine(payloadDirectory, ExecutableName)
            If Not File.Exists(stagedExecutable) Then
                Throw New InvalidDataException($"The update package does not contain the expected application executable '{ExecutableName}'.")
            End If

            Return New StagedUpdate With {
                .Version = update.LatestVersion,
                .StagingDirectory = stagingDirectory,
                .PayloadDirectory = payloadDirectory,
                .ExecutableName = ExecutableName
            }
        Catch
            Try
                If Directory.Exists(stagingDirectory) Then
                    Directory.Delete(stagingDirectory, recursive:=True)
                End If
            Catch
            End Try
            Throw
        End Try
    End Function

    Public Sub LaunchStagedUpdate(stagedUpdate As StagedUpdate, installationDirectory As String)
        ArgumentNullException.ThrowIfNull(stagedUpdate)
        If String.IsNullOrWhiteSpace(installationDirectory) OrElse Not Directory.Exists(installationDirectory) Then
            Throw New DirectoryNotFoundException("The application installation directory could not be found.")
        End If

        Dim stagedExecutable = Path.Combine(stagedUpdate.PayloadDirectory, stagedUpdate.ExecutableName)
        If Not File.Exists(stagedExecutable) Then
            Throw New FileNotFoundException("The staged application executable could not be found.", stagedExecutable)
        End If

        Dim scriptPath = Path.Combine(stagedUpdate.StagingDirectory, "apply-update.ps1")
        File.WriteAllText(scriptPath, CreateInstallerScript(), New UTF8Encoding(encoderShouldEmitUTF8Identifier:=False))

        Dim startInfo As New ProcessStartInfo("powershell.exe") With {
            .UseShellExecute = False,
            .CreateNoWindow = True,
            .WorkingDirectory = stagedUpdate.StagingDirectory
        }
        startInfo.ArgumentList.Add("-NoLogo")
        startInfo.ArgumentList.Add("-NoProfile")
        startInfo.ArgumentList.Add("-NonInteractive")
        startInfo.ArgumentList.Add("-ExecutionPolicy")
        startInfo.ArgumentList.Add("Bypass")
        startInfo.ArgumentList.Add("-File")
        startInfo.ArgumentList.Add(scriptPath)
        startInfo.ArgumentList.Add("-ProcessId")
        startInfo.ArgumentList.Add(Environment.ProcessId.ToString(Globalization.CultureInfo.InvariantCulture))
        startInfo.ArgumentList.Add("-SourceDirectory")
        startInfo.ArgumentList.Add(stagedUpdate.PayloadDirectory)
        startInfo.ArgumentList.Add("-DestinationDirectory")
        startInfo.ArgumentList.Add(Path.GetFullPath(installationDirectory))
        startInfo.ArgumentList.Add("-ExecutableName")
        startInfo.ArgumentList.Add(stagedUpdate.ExecutableName)
        startInfo.ArgumentList.Add("-StagingDirectory")
        startInfo.ArgumentList.Add(stagedUpdate.StagingDirectory)

        If Process.Start(startInfo) Is Nothing Then
            Throw New InvalidOperationException("The update installer could not be started.")
        End If
    End Sub

    Private Shared Function CreateInstallerScript() As String
        Return String.Join(Environment.NewLine, {
            "param(",
            "    [Parameter(Mandatory=$true)][int]$ProcessId,",
            "    [Parameter(Mandatory=$true)][string]$SourceDirectory,",
            "    [Parameter(Mandatory=$true)][string]$DestinationDirectory,",
            "    [Parameter(Mandatory=$true)][string]$ExecutableName,",
            "    [Parameter(Mandatory=$true)][string]$StagingDirectory",
            ")",
            "$ErrorActionPreference = 'Stop'",
            "$backupDirectory = Join-Path $StagingDirectory 'backup'",
            "$backedUpFiles = [System.Collections.Generic.List[string]]::new()",
            "$createdFiles = [System.Collections.Generic.List[string]]::new()",
            "$errorDirectory = Join-Path $env:LOCALAPPDATA 'DISM_Lab'",
            "$errorPath = Join-Path $errorDirectory 'update-error.txt'",
            "try {",
            "    Wait-Process -Id $ProcessId -ErrorAction SilentlyContinue",
            "    Start-Sleep -Milliseconds 500",
            "    New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null",
            "    $sourceRoot = [System.IO.Path]::GetFullPath($SourceDirectory).TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar",
            "    foreach ($sourceFile in Get-ChildItem -LiteralPath $SourceDirectory -File -Recurse) {",
            "        $relativePath = $sourceFile.FullName.Substring($sourceRoot.Length)",
            "        $destinationPath = Join-Path $DestinationDirectory $relativePath",
            "        $destinationParent = Split-Path -Parent $destinationPath",
            "        New-Item -ItemType Directory -Path $destinationParent -Force | Out-Null",
            "        if (Test-Path -LiteralPath $destinationPath -PathType Leaf) {",
            "            $backupPath = Join-Path $backupDirectory $relativePath",
            "            New-Item -ItemType Directory -Path (Split-Path -Parent $backupPath) -Force | Out-Null",
            "            Copy-Item -LiteralPath $destinationPath -Destination $backupPath -Force",
            "            $backedUpFiles.Add($relativePath)",
            "        } else {",
            "            $createdFiles.Add($destinationPath)",
            "        }",
            "        Copy-Item -LiteralPath $sourceFile.FullName -Destination $destinationPath -Force",
            "    }",
            "    $executablePath = Join-Path $DestinationDirectory $ExecutableName",
            "    Start-Process -FilePath $executablePath -WorkingDirectory $DestinationDirectory",
            "    Remove-Item -LiteralPath $errorPath -Force -ErrorAction SilentlyContinue",
            "    Remove-Item -LiteralPath $StagingDirectory -Recurse -Force -ErrorAction SilentlyContinue",
            "} catch {",
            "    foreach ($createdFile in $createdFiles) {",
            "        Remove-Item -LiteralPath $createdFile -Force -ErrorAction SilentlyContinue",
            "    }",
            "    foreach ($relativePath in $backedUpFiles) {",
            "        $backupPath = Join-Path $backupDirectory $relativePath",
            "        $destinationPath = Join-Path $DestinationDirectory $relativePath",
            "        Copy-Item -LiteralPath $backupPath -Destination $destinationPath -Force -ErrorAction SilentlyContinue",
            "    }",
            "    New-Item -ItemType Directory -Path $errorDirectory -Force | Out-Null",
            "    ($_ | Out-String) | Set-Content -LiteralPath $errorPath -Encoding UTF8",
            "    $executablePath = Join-Path $DestinationDirectory $ExecutableName",
            "    if (Test-Path -LiteralPath $executablePath -PathType Leaf) {",
            "        Start-Process -FilePath $executablePath -WorkingDirectory $DestinationDirectory -ErrorAction SilentlyContinue",
            "    }",
            "    exit 1",
            "}"})
    End Function

    Private Shared Function CreateHttpClient() As HttpClient
        Dim client = New HttpClient() With {
            .Timeout = TimeSpan.FromMinutes(10)
        }
        client.DefaultRequestHeaders.UserAgent.ParseAdd("DISM-Lab-Updater/1.0")
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json")
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28")
        Return client
    End Function

    Private Shared Sub ValidateDownloadUri(downloadUrl As String)
        Dim uri As Uri = Nothing
        If Not Uri.TryCreate(downloadUrl, UriKind.Absolute, uri) OrElse
           uri.Scheme <> Uri.UriSchemeHttps OrElse
           Not String.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase) Then
            Throw New InvalidDataException("A release asset contains an invalid download URL.")
        End If
    End Sub

    Private Shared Async Function DownloadFileAsync(downloadUrl As String,
                                                    destinationPath As String,
                                                    cancellationToken As System.Threading.CancellationToken) As Task
        Using response = Await Client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            response.EnsureSuccessStatusCode()
            Using destination = New FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync:=True)
                Await response.Content.CopyToAsync(destination, cancellationToken)
            End Using
        End Using
    End Function

    Private Shared Async Function VerifyChecksumAsync(packagePath As String,
                                                       checksumPath As String,
                                                       cancellationToken As System.Threading.CancellationToken) As Task
        Dim checksumContents = (Await File.ReadAllTextAsync(checksumPath, cancellationToken)).Trim()
        Dim expectedHash = checksumContents.Split({" "c, ControlChars.Tab, ControlChars.Cr, ControlChars.Lf}, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
        If expectedHash Is Nothing OrElse expectedHash.Length <> 64 OrElse Not expectedHash.All(AddressOf Uri.IsHexDigit) Then
            Throw New InvalidDataException("The release checksum file is invalid.")
        End If

        Dim actualHash As String
        Using packageStream = New FileStream(packagePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync:=True)
            Dim hashBytes = Await SHA256.HashDataAsync(packageStream, cancellationToken)
            actualHash = Convert.ToHexString(hashBytes)
        End Using

        If Not String.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase) Then
            Throw New InvalidDataException("The downloaded update failed SHA-256 verification and will not be installed.")
        End If
    End Function

    Private Shared Async Function ExtractArchiveSafelyAsync(packagePath As String,
                                                            destinationDirectory As String,
                                                            cancellationToken As System.Threading.CancellationToken) As Task
        Dim destinationRoot = Path.GetFullPath(destinationDirectory)
        If Not destinationRoot.EndsWith(Path.DirectorySeparatorChar) Then
            destinationRoot &= Path.DirectorySeparatorChar
        End If

        Using archive = ZipFile.OpenRead(packagePath)
            For Each entry In archive.Entries
                cancellationToken.ThrowIfCancellationRequested()
                Dim destinationPath = Path.GetFullPath(Path.Combine(destinationRoot, entry.FullName))
                If Not destinationPath.StartsWith(destinationRoot, StringComparison.OrdinalIgnoreCase) Then
                    Throw New InvalidDataException("The update archive contains an unsafe file path.")
                End If

                If String.IsNullOrEmpty(entry.Name) Then
                    Directory.CreateDirectory(destinationPath)
                    Continue For
                End If

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath))
                Using source = entry.Open()
                    Using destination = New FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync:=True)
                        Await source.CopyToAsync(destination, cancellationToken)
                    End Using
                End Using
            Next
        End Using
    End Function
End Class

Imports System.Diagnostics
Imports System.Threading.Tasks

Public Class AdkInstallationWindow
    Private _installationSuccessful As Boolean = False
    Private _installationCancelled As Boolean = False

    ' Events for status updates
    Public Event StatusChanged(header As String, detail As String)
    Public Event DismIndicatorStart()
    Public Event DismIndicatorStop()

    Public ReadOnly Property InstallationSuccessful As Boolean
        Get
            Return _installationSuccessful
        End Get
    End Property

    Public ReadOnly Property InstallationCancelled As Boolean
        Get
            Return _installationCancelled
        End Get
    End Property

    Private Sub RaiseStatusUpdate(header As String, detail As String)
        RaiseEvent StatusChanged(header, detail)
    End Sub

    Private Sub TitleBar_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
        If e.ChangedButton = MouseButton.Left Then
            Me.DragMove()
        End If
    End Sub

    Private Sub CloseButton_Click(sender As Object, e As RoutedEventArgs)
        _installationCancelled = True
        Me.DialogResult = False
        Me.Close()
    End Sub

    Private Sub CancelButton_Click(sender As Object, e As RoutedEventArgs)
        _installationCancelled = True
        Me.DialogResult = False
        Me.Close()
    End Sub

    Private Async Sub InstallButton_Click(sender As Object, e As RoutedEventArgs)
        ' Hide buttons during installation
        If ButtonPanel IsNot Nothing Then
            ButtonPanel.Visibility = Visibility.Collapsed
        End If

        ' Show progress
        ProgressPanel.Visibility = Visibility.Visible

        ' ✅ Start DISM indicator when installation actually begins
        RaiseEvent DismIndicatorStart()

        Try
            ' First, check if winget is available
            ProgressText.Text = "Checking for Windows Package Manager (winget)..."
            RaiseStatusUpdate("Checking System", "Verifying winget…")
            Await Task.Delay(500)

            If Not Await IsWingetAvailableAsync() Then
                RaiseStatusUpdate("Installation Failed", "Windows Package Manager (winget) not available")

                MessageBox.Show(
                    "Windows Package Manager (winget) is not available on this system." & Environment.NewLine & Environment.NewLine &
                    "Please install the Windows ADK PE Add-on manually from:" & Environment.NewLine &
                    "https://learn.microsoft.com/en-us/windows-hardware/get-started/adk-install",
                    "Installation Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error)

                _installationCancelled = True
                Me.DialogResult = False
                Me.Close()
                Return
            End If

            RaiseStatusUpdate("winget Ready", "Package manager ready")
            Await Task.Delay(500)

            ' Install Windows ADK first (required dependency)
            ProgressText.Text = "Installing Windows ADK (this may take several minutes)..."
            RaiseStatusUpdate("Installing...", "Installing Windows ADK…")

            Dim adkInstalled = Await InstallPackageAsync("Microsoft.WindowsADK")

            If Not adkInstalled Then
                RaiseStatusUpdate("ADK Installation Failed", "Base package installation unsuccessful")

                Dim retry = MessageBox.Show(
                    "Windows ADK installation failed or was cancelled." & Environment.NewLine & Environment.NewLine &
                    "The ADK is required before installing the PE Add-on. Would you like to try again?",
                    "Installation Failed",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning)

                If retry = MessageBoxResult.Yes Then
                    ' Show buttons again for retry
                    If ButtonPanel IsNot Nothing Then
                        ButtonPanel.Visibility = Visibility.Visible
                    End If
                    ProgressPanel.Visibility = Visibility.Collapsed
                    RaiseStatusUpdate("Ready to Retry", "Waiting for user action...")

                    ' ✅ Stop DISM indicator on retry
                    RaiseEvent DismIndicatorStop()
                    Return
                Else
                    _installationCancelled = True
                    Me.DialogResult = False
                    Me.Close()
                    Return
                End If
            End If

            RaiseStatusUpdate("Windows ADK Installed", "ADK installed")
            Await Task.Delay(800)

            ' Install WinPE Add-on
            ProgressText.Text = "Installing Windows ADK PE Add-on (this may take several minutes)..."
            RaiseStatusUpdate("Installing...", "Installing WinPE Add-on")

            Dim peAddonInstalled = Await InstallPackageAsync("Microsoft.WindowsADK.WinPEAddon")

            If Not peAddonInstalled Then
                RaiseStatusUpdate("PE Add-on Failed", "WinPE components installation unsuccessful")

                Dim retry = MessageBox.Show(
                    "Windows ADK PE Add-on installation failed or was cancelled." & Environment.NewLine & Environment.NewLine &
                    "Would you like to try again?",
                    "Installation Failed",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning)

                If retry = MessageBoxResult.Yes Then
                    ' Show buttons again for retry
                    If ButtonPanel IsNot Nothing Then
                        ButtonPanel.Visibility = Visibility.Visible
                    End If
                    ProgressPanel.Visibility = Visibility.Collapsed
                    RaiseStatusUpdate("Ready to Retry", "Waiting for user action...")

                    ' ✅ Stop DISM indicator on retry
                    RaiseEvent DismIndicatorStop()
                    Return
                Else
                    _installationCancelled = True
                    Me.DialogResult = False
                    Me.Close()
                    Return
                End If
            End If

            ' Success
            ProgressText.Text = "Installation completed successfully!"
            InstallProgress.IsIndeterminate = False
            InstallProgress.Value = 100

            RaiseStatusUpdate("Installation Complete", "ADK setup complete")
            Await Task.Delay(1500)

            MessageBox.Show(
                "Windows ADK and PE Add-on have been installed successfully!" & Environment.NewLine & Environment.NewLine &
                "You can now create WinPE images.",
                "Installation Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information)

            _installationSuccessful = True
            Me.DialogResult = True
            Me.Close()

        Catch ex As Exception
            RaiseStatusUpdate("Installation Error", ex.Message)

            MessageBox.Show(
                "An error occurred during installation:" & Environment.NewLine & Environment.NewLine &
                ex.Message,
                "Installation Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error)

            ' Show buttons again on error
            If ButtonPanel IsNot Nothing Then
                ButtonPanel.Visibility = Visibility.Visible
            End If
            ProgressPanel.Visibility = Visibility.Collapsed

            _installationCancelled = True
            Me.DialogResult = False
        Finally
            ' ✅ Always stop DISM indicator when installation completes or fails
            RaiseEvent DismIndicatorStop()
        End Try
    End Sub

    Private Async Function IsWingetAvailableAsync() As Task(Of Boolean)
        Try
            Dim psi As New ProcessStartInfo With {
                .FileName = "winget.exe",
                .Arguments = "--version",
                .UseShellExecute = False,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True,
                .CreateNoWindow = True
            }

            Using proc As Process = Process.Start(psi)
                Await proc.WaitForExitAsync()
                Return proc.ExitCode = 0
            End Using
        Catch
            Return False
        End Try
    End Function

    Private Async Function InstallPackageAsync(packageId As String) As Task(Of Boolean)
        Try
            Dim psi As New ProcessStartInfo With {
                .FileName = "winget.exe",
                .Arguments = $"install --id {packageId} --accept-package-agreements --accept-source-agreements",
                .UseShellExecute = False,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True,
                .CreateNoWindow = True
            }

            Using proc As Process = Process.Start(psi)
                ' Read output in real-time (optional - for debugging)
                Dim outputTask = proc.StandardOutput.ReadToEndAsync()
                Dim errorTask = proc.StandardError.ReadToEndAsync()

                Await proc.WaitForExitAsync()

                Dim output = Await outputTask
                Dim errorOutput = Await errorTask

                ' Log output for debugging
                Debug.WriteLine($"winget install {packageId} - Exit Code: {proc.ExitCode}")
                Debug.WriteLine($"Output: {output}")
                If Not String.IsNullOrWhiteSpace(errorOutput) Then
                    Debug.WriteLine($"Error: {errorOutput}")
                End If

                ' Exit code 0 means success
                ' Exit code -1978335189 (0x8A15000B) means "No applicable update found" but package may already be installed
                Return proc.ExitCode = 0 OrElse proc.ExitCode = -1978335189
            End Using
        Catch ex As Exception
            Debug.WriteLine($"Exception installing {packageId}: {ex.Message}")
            Return False
        End Try
    End Function
End Class

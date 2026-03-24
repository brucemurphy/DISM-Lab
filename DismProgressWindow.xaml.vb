Imports System.Windows
Imports System.Diagnostics
Imports System.Text.RegularExpressions
Imports System.Threading

Namespace YourNamespace
    Partial Public Class DismProgressWindow
        Inherits Window

        Private _process As Process
        Private _cts As CancellationTokenSource
        Private _completed As Boolean = False
        Private _progressStarted As Boolean = False

        Public Sub New()
            InitializeComponent()
            AddHandler CancelButton.Click, AddressOf CancelButton_Click
            AddHandler CloseButton.Click, AddressOf CloseButton_Click
            ProgressBar.Value = 0
            ProgressBar.Visibility = Visibility.Collapsed
        End Sub

        Public Sub StartDismProcess(wimPath As String, idx As Integer)
            If String.IsNullOrWhiteSpace(wimPath) Then
                AppendLine("No WIM path provided.")
                Return
            End If

            ProgressBar.Visibility = Visibility.Visible
            ProgressBar.IsIndeterminate = True
            ProgressBar.Value = 0
            _progressStarted = False

            Dim args = $"/English /Get-WimInfo /WimFile:""" & wimPath & """ /Index:" & idx
            AppendLine("Starting: dism.exe " & args)
            _cts = New CancellationTokenSource()

            Dim psi As New ProcessStartInfo("dism.exe", args) With {
                .UseShellExecute = False,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True,
                .CreateNoWindow = True
            }

            Try
                _process = New Process() With {.StartInfo = psi, .EnableRaisingEvents = True}
                AddHandler _process.OutputDataReceived, AddressOf OnOutput
                AddHandler _process.ErrorDataReceived, AddressOf OnError
                AddHandler _process.Exited, AddressOf OnExited

                If Not _process.Start() Then
                    AppendLine("Failed to start dism.exe process.")
                    ProgressBar.IsIndeterminate = False
                    ProgressBar.Value = 0
                    Return
                End If

                _process.BeginOutputReadLine()
                _process.BeginErrorReadLine()
            Catch ex As Exception
                AppendLine("Process start exception: " & ex.Message)
                ProgressBar.IsIndeterminate = False
            End Try
        End Sub

        Private Sub OnOutput(sender As Object, e As DataReceivedEventArgs)
            If e.Data Is Nothing Then Return
            Dispatcher.BeginInvoke(Sub()
                                       AppendLine(e.Data)
                                       TryUpdateProgress(e.Data)
                                   End Sub)
        End Sub

        Private Sub OnError(sender As Object, e As DataReceivedEventArgs)
            If e.Data Is Nothing Then Return
            Dispatcher.BeginInvoke(Sub()
                                       AppendLine("[ERR] " & e.Data)
                                       TryUpdateProgress(e.Data)
                                   End Sub)
        End Sub

        Private Sub OnExited(sender As Object, e As EventArgs)
            Dispatcher.BeginInvoke(Sub()
                                       _completed = True
                                       AppendLine($"Process exited with code {_process.ExitCode}.")
                                       If _process.ExitCode = 0 AndAlso ProgressBar.Value < 100 Then
                                           ProgressBar.IsIndeterminate = False
                                           ProgressBar.Value = 100
                                       End If
                                       CancelButton.IsEnabled = False
                                       CloseButton.Content = "Close"
                                   End Sub)
        End Sub

        Private Sub TryUpdateProgress(line As String)
            ' Match forms like "12.3% completed" or "12% completed"
            Dim m = Regex.Match(line, "(\d{1,3}(\.\d+)?)%\s*completed", RegexOptions.IgnoreCase)
            If Not m.Success Then
                ' Fallback: sometimes DISM prints just "xx.x%" alone
                m = Regex.Match(line, "(\d{1,3}(\.\d+)?)%", RegexOptions.IgnoreCase)
            End If
            If m.Success Then
                Dim pctText = m.Groups(1).Value
                Dim pct As Double
                If Double.TryParse(pctText, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, pct) Then
                    If pct >= ProgressBar.Minimum AndAlso pct <= ProgressBar.Maximum Then
                        If Not _progressStarted Then
                            ProgressBar.IsIndeterminate = False
                            _progressStarted = True
                        End If
                        ProgressBar.Value = pct
                    End If
                End If
            End If
        End Sub

        Private Sub CancelButton_Click(sender As Object, e As RoutedEventArgs)
            If _completed Then
                Close()
                Return
            End If
            CancelButton.IsEnabled = False
            AppendLine("Cancellation requested...")
            Try
                _cts?.Cancel()
                If _process IsNot Nothing AndAlso Not _process.HasExited Then
                    _process.Kill(True)
                    AppendLine("Process terminated.")
                End If
            Catch ex As Exception
                AppendLine("Cancellation error: " & ex.Message)
            End Try
        End Sub

        Private Sub CloseButton_Click(sender As Object, e As RoutedEventArgs)
            Close()
        End Sub

        Private Sub AppendLine(text As String)
            OutputTextBox.AppendText(text & Environment.NewLine)
            OutputTextBox.ScrollToEnd()
        End Sub

        Protected Overrides Sub OnClosed(e As EventArgs)
            MyBase.OnClosed(e)
            Try
                If _process IsNot Nothing AndAlso Not _process.HasExited Then
                    _process.Kill(True)
                End If
            Catch
            End Try
            _cts?.Dispose()
        End Sub
    End Class
End Namespace
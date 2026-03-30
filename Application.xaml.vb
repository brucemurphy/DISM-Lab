Imports System.Diagnostics
Imports System.Security.Principal
Imports System.Windows

Partial Public Class Application
    Inherits System.Windows.Application

    Protected Overrides Sub OnStartup(e As StartupEventArgs)
        If Not IsRunningAsAdministrator() Then
            Try
                Dim exePath = Process.GetCurrentProcess().MainModule.FileName
                Dim psi As New ProcessStartInfo(exePath) With {
                    .UseShellExecute = True,
                    .Verb = "runas"
                }
                Process.Start(psi)
            Catch ex As Exception
                MessageBox.Show("Automatic elevation failed. Please restart this application as Administrator." & vbCrLf & ex.Message,
                                "Elevation Required",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error)
            End Try
            Shutdown()
            Return
        End If

        Dim splash = New SplashScreen("SplashLab.png")
        splash.Show(False)
        System.Threading.Thread.Sleep(1500)
        splash.Close(TimeSpan.Zero)

        MyBase.OnStartup(e)
    End Sub

    Private Shared Function IsRunningAsAdministrator() As Boolean
        Try
            Dim identity = WindowsIdentity.GetCurrent()
            Dim principal = New WindowsPrincipal(identity)
            Return principal.IsInRole(WindowsBuiltInRole.Administrator)
        Catch
            Return False
        End Try
    End Function
End Class

Partial Public Class Application
    Inherits System.Windows.Application

    Protected Overrides Sub OnStartup(e As StartupEventArgs)
        Dim splash = New SplashScreen("SplashLab.png")
        splash.Show(False)
        System.Threading.Thread.Sleep(1500)
        splash.Close(TimeSpan.Zero)

        MyBase.OnStartup(e)
    End Sub
End Class

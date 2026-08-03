Imports System.IO
Imports System.Text.Json

Friend NotInheritable Class UserSettings
    Public Property IsRealTimeModeEnabled As Boolean = False
End Class

Friend NotInheritable Class UserSettingsStore
    Private Shared ReadOnly SyncRoot As New Object()
    Private Shared ReadOnly SettingsDirectory As String = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DISM_Lab")
    Private Shared ReadOnly SettingsPath As String = Path.Combine(SettingsDirectory, "settings.json")
    Private Shared ReadOnly SerializerOptions As New JsonSerializerOptions With {
        .WriteIndented = True
    }

    Private Sub New()
    End Sub

    Public Shared Function Load() As UserSettings
        SyncLock SyncRoot
            Try
                If Not File.Exists(SettingsPath) Then
                    Return New UserSettings()
                End If

                Dim json = File.ReadAllText(SettingsPath)
                Dim settings = JsonSerializer.Deserialize(Of UserSettings)(json)
                If settings Is Nothing Then
                    Return New UserSettings()
                End If

                Return settings
            Catch ex As Exception
                Debug.WriteLine("Failed to load user settings: " & ex.Message)
                Return New UserSettings()
            End Try
        End SyncLock
    End Function

    Public Shared Sub Save(settings As UserSettings)
        ArgumentNullException.ThrowIfNull(settings)

        SyncLock SyncRoot
            Dim temporaryPath = SettingsPath & ".tmp"
            Try
                Directory.CreateDirectory(SettingsDirectory)
                File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, SerializerOptions))
                File.Move(temporaryPath, SettingsPath, overwrite:=True)
            Catch ex As Exception
                Debug.WriteLine("Failed to save user settings: " & ex.Message)
                Throw
            Finally
                Try
                    File.Delete(temporaryPath)
                Catch ex As Exception
                    Debug.WriteLine("Failed to clean up temporary settings file: " & ex.Message)
                End Try
            End Try
        End SyncLock
    End Sub
End Class

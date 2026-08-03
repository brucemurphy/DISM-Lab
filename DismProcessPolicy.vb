Imports System.Diagnostics
Imports System.IO
Imports System.Text.Json

Friend NotInheritable Class DismProcessMeasurement
    Private Shared ReadOnly LogSyncRoot As New Object()
    Private Shared ReadOnly MetricsDirectory As String = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DISM_Lab",
        "Logs")
    Private Shared ReadOnly MetricsPath As String = Path.Combine(MetricsDirectory, "dism-performance.jsonl")

    Private ReadOnly _process As Process
    Private ReadOnly _arguments As String
    Private ReadOnly _enhancedModeEnabled As Boolean
    Private ReadOnly _appliedPriority As String
    Private ReadOnly _startedAtUtc As DateTimeOffset
    Private ReadOnly _stopwatch As Stopwatch
    Private _completed As Boolean

    Friend Sub New(process As Process, arguments As String, enhancedModeEnabled As Boolean, appliedPriority As String)
        _process = process
        _arguments = arguments
        _enhancedModeEnabled = enhancedModeEnabled
        _appliedPriority = appliedPriority
        _startedAtUtc = DateTimeOffset.UtcNow
        _stopwatch = Stopwatch.StartNew()
    End Sub

    Public Sub Complete(exitCode As Integer)
        If _completed Then Return
        _completed = True
        _stopwatch.Stop()

        Dim processorTimeMs As Double? = Nothing
        Dim peakWorkingSetBytes As Long? = Nothing
        Try
            processorTimeMs = _process.TotalProcessorTime.TotalMilliseconds
            peakWorkingSetBytes = _process.PeakWorkingSet64
        Catch ex As Exception
            Debug.WriteLine("Failed to read DISM performance counters: " & ex.Message)
        End Try

        Dim record = New DismPerformanceRecord With {
            .StartedAtUtc = _startedAtUtc,
            .ElapsedMilliseconds = _stopwatch.Elapsed.TotalMilliseconds,
            .ProcessorTimeMilliseconds = processorTimeMs,
            .PeakWorkingSetBytes = peakWorkingSetBytes,
            .ExitCode = exitCode,
            .EnhancedModeEnabled = _enhancedModeEnabled,
            .AppliedPriority = _appliedPriority,
            .Arguments = _arguments
        }

        Try
            SyncLock LogSyncRoot
                Directory.CreateDirectory(MetricsDirectory)
                File.AppendAllText(MetricsPath, JsonSerializer.Serialize(record) & Environment.NewLine)
            End SyncLock
        Catch ex As Exception
            Debug.WriteLine("Failed to write DISM performance measurement: " & ex.Message)
        End Try
    End Sub

    Private NotInheritable Class DismPerformanceRecord
        Public Property StartedAtUtc As DateTimeOffset
        Public Property ElapsedMilliseconds As Double
        Public Property ProcessorTimeMilliseconds As Double?
        Public Property PeakWorkingSetBytes As Long?
        Public Property ExitCode As Integer
        Public Property EnhancedModeEnabled As Boolean
        Public Property AppliedPriority As String
        Public Property Arguments As String
    End Class
End Class

Friend NotInheritable Class DismProcessPolicy
    Private Sub New()
    End Sub

    Public Shared Function Apply(process As Process, arguments As String) As DismProcessMeasurement
        ArgumentNullException.ThrowIfNull(process)

        Dim enhancedModeEnabled = UserSettingsStore.Load().IsRealTimeModeEnabled
        Dim appliedPriority = "OS default"

        Try
            If enhancedModeEnabled Then
                process.PriorityClass = ProcessPriorityClass.AboveNormal
            End If
            appliedPriority = process.PriorityClass.ToString()
        Catch ex As Exception
            Debug.WriteLine("Failed to configure DISM priority: " & ex.Message)
        End Try

        Return New DismProcessMeasurement(process, arguments, enhancedModeEnabled, appliedPriority)
    End Function
End Class

Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Threading.Tasks

Partial Public Class DiskSelectionWindow
    Inherits Window
    Public Property SelectedDisk As DiskInfo
    Private ReadOnly _diskProvider As Func(Of List(Of DiskInfo))

    Public Sub New(disks As IEnumerable(Of DiskInfo), Optional diskProvider As Func(Of List(Of DiskInfo)) = Nothing)
        InitializeComponent()
        AddHandler SourceInitialized, AddressOf DiskSelectionWindow_SourceInitialized
        _diskProvider = diskProvider

        UpdateDiskList(disks)
    End Sub

    Private Sub UpdateDiskList(disks As IEnumerable(Of DiskInfo))
        Dim usbDisks As List(Of DiskInfo)

        If disks Is Nothing Then
            usbDisks = New List(Of DiskInfo)()
        Else
            usbDisks = disks.Where(Function(d) d IsNot Nothing AndAlso d.IsUsb) _
                             .OrderBy(Function(d) d.Number) _
                             .ToList()
        End If

        DiskListView.ItemsSource = usbDisks
        Dim hasItems = usbDisks.Count > 0
        DiskListView.IsEnabled = hasItems
        EmptyStateText.Visibility = If(hasItems, Visibility.Collapsed, Visibility.Visible)

        DiskListView.SelectedIndex = If(hasItems, 0, -1)
    End Sub

    Private Sub OkButton_Click(sender As Object, e As RoutedEventArgs)
        If Not DiskListView.IsEnabled Then
            MessageBox.Show("No removable USB disks are available.", Title, MessageBoxButton.OK, MessageBoxImage.Information)
            Return
        End If

        Dim disk = TryCast(DiskListView.SelectedItem, DiskInfo)
        If disk Is Nothing Then
            MessageBox.Show("Select a disk to continue.", Title, MessageBoxButton.OK, MessageBoxImage.Information)
            Return
        End If

        SelectedDisk = disk
        DialogResult = True
    End Sub

    Private Sub CancelButton_Click(sender As Object, e As RoutedEventArgs)
        DialogResult = False
    End Sub

    Private Sub DiskListView_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs)
        OkButton_Click(sender, e)
    End Sub

    Private Async Sub RefreshDisksButton_Click(sender As Object, e As RoutedEventArgs)
        If _diskProvider Is Nothing Then
            MessageBox.Show("Unable to refresh disks because enumeration is unavailable.", Title, MessageBoxButton.OK, MessageBoxImage.Information)
            Return
        End If

        RefreshDisksButton.IsEnabled = False
        DiskListView.IsEnabled = False
        EmptyStateText.Visibility = Visibility.Collapsed

        Try
            Dim latestDisks = Await Task.Run(_diskProvider)
            UpdateDiskList(latestDisks)
        Catch ex As Exception
            MessageBox.Show("Unable to refresh disks: " & ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error)
            UpdateDiskList(Nothing)
        Finally
            RefreshDisksButton.IsEnabled = True
        End Try
    End Sub

    Private Sub DiskSelectionWindow_SourceInitialized(sender As Object, e As EventArgs)
        Try
            Dim hwnd = New System.Windows.Interop.WindowInteropHelper(Me).Handle
            Dim useImmersiveDarkMode As Integer = 20
            Dim value As Integer = 1
            DwmSetWindowAttribute(hwnd, useImmersiveDarkMode, value, Marshal.SizeOf(value))
        Catch
            ' Ignore failures; fall back to default chrome
        End Try
    End Sub

    <DllImport("dwmapi.dll", PreserveSig:=True)>
    Private Shared Function DwmSetWindowAttribute(hwnd As IntPtr, attr As Integer, ByRef attrValue As Integer, attrSize As Integer) As Integer
    End Function
End Class

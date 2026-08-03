Imports System.Text.Json.Serialization

Friend NotInheritable Class GitHubRelease
    <JsonPropertyName("tag_name")>
    Public Property TagName As String

    <JsonPropertyName("name")>
    Public Property Name As String

    <JsonPropertyName("body")>
    Public Property Body As String

    <JsonPropertyName("html_url")>
    Public Property HtmlUrl As String

    <JsonPropertyName("draft")>
    Public Property IsDraft As Boolean

    <JsonPropertyName("prerelease")>
    Public Property IsPrerelease As Boolean

    <JsonPropertyName("assets")>
    Public Property Assets As List(Of GitHubReleaseAsset)
End Class

Friend NotInheritable Class GitHubReleaseAsset
    <JsonPropertyName("name")>
    Public Property Name As String

    <JsonPropertyName("browser_download_url")>
    Public Property DownloadUrl As String

    <JsonPropertyName("size")>
    Public Property Size As Long
End Class

Friend Structure SemanticVersion
    Implements IComparable(Of SemanticVersion)

    Public Sub New(major As Integer, minor As Integer, patch As Integer)
        Me.Major = major
        Me.Minor = minor
        Me.Patch = patch
    End Sub

    Public ReadOnly Property Major As Integer
    Public ReadOnly Property Minor As Integer
    Public ReadOnly Property Patch As Integer

    Public Shared Function TryParse(value As String, ByRef version As SemanticVersion) As Boolean
        If String.IsNullOrWhiteSpace(value) Then
            Return False
        End If

        Dim normalized = value.Trim()
        If normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase) Then
            normalized = normalized.Substring(1)
        End If

        normalized = normalized.Split("+"c)(0).Split("-"c)(0)
        Dim components = normalized.Split("."c)
        If components.Length <> 3 Then
            Return False
        End If

        Dim major As Integer
        Dim minor As Integer
        Dim patch As Integer
        If Not Integer.TryParse(components(0), major) OrElse
           Not Integer.TryParse(components(1), minor) OrElse
           Not Integer.TryParse(components(2), patch) OrElse
           major < 0 OrElse minor < 0 OrElse patch < 0 Then
            Return False
        End If

        version = New SemanticVersion(major, minor, patch)
        Return True
    End Function

    Public Function CompareTo(other As SemanticVersion) As Integer Implements IComparable(Of SemanticVersion).CompareTo
        Dim result = Major.CompareTo(other.Major)
        If result <> 0 Then
            Return result
        End If

        result = Minor.CompareTo(other.Minor)
        If result <> 0 Then
            Return result
        End If

        Return Patch.CompareTo(other.Patch)
    End Function

    Public Overrides Function ToString() As String
        Return $"{Major}.{Minor}.{Patch}"
    End Function
End Structure

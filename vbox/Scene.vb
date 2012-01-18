Public Class Scene

    Private _lengthStr As String
    Private _minutes As Integer

    Public Property SceneNumber As Integer
    Public Property SceneText As String
    Public Property NicheText As String

    ''' <summary>
    ''' Lenght of scene in minutes and seconds (as string mm:ss)
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property Length As String
        Get
            Return _lengthStr
        End Get
        Set(ByVal value As String)
            _lengthStr = value
            _minutes = -1
        End Set
    End Property

    ''' <summary>
    ''' Lenght of scene in minutes.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Property Minutes As Integer
        Get
            If _minutes = -1 Then
                _minutes = ToMinutes(_lengthStr)
            End If
            Return _minutes
        End Get
        Set(ByVal value As Integer)
            _minutes = value
            _lengthStr = value.ToString() + ":00"
        End Set
    End Property

    Public Shared Function ToMinutes(ByVal time As String) As Integer
        Dim hours As Integer = 0
        Dim minutes As Integer = 0
        Dim Seconds As Integer = 0
        Dim part = time.Split(":")
        Select Case (part.Length)
            Case 1
                Integer.TryParse(part(0), minutes)
            Case 2
                Integer.TryParse(part(0), minutes)
                Integer.TryParse(part(1), Seconds)
            Case 3
                Integer.TryParse(part(0), hours)
                Integer.TryParse(part(1), minutes)
                Integer.TryParse(part(2), Seconds)
        End Select
        Return hours * 60 + minutes + If(Seconds >= 30, 1, 0)
    End Function
End Class

Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports Common_vb.My.Collections
Imports System.Diagnostics

Module Program
    Dim title As String
    Dim studio As String
    Dim director As String = "NA"
    Dim length As Integer
    Dim niches As String
    Dim starList As List(Of String) = New List(Of String)
    Dim sceneList As List(Of Scene) = New List(Of Scene)
    Dim nicheTable As CountingDictionary
    Dim nicheFile As String = "C:\Data\niche.txt"

    Sub Main()
        Dim input As UInt32
        While (1)
            Console.Write("Input a number: ")
            Dim line As String = Console.ReadLine()
            Try
                input = UInt32.Parse(line, System.Globalization.NumberStyles.HexNumber)

            Catch ex As Exception

                Console.WriteLine("Failed to parse number")
            End Try
            Console.WriteLine(Encode(input))
        End While
    End Sub

    Sub vbox()
        nicheTable = New CountingDictionary(100)
        nicheTable.Load(nicheFile)
        ProcessInput(Console.In)
        nicheTable.Save(nicheFile)
    End Sub

    Sub ProcessInput(ByVal input As TextReader)
        Dim line As String

        If (SeekPattern(input, "^Rate it") IsNot Nothing) Then
            title = input.ReadLine()
            If (title IsNot Nothing) Then
                GetStars(input)
                GetNiches(input)
                line = input.ReadLine()
                If GetDirector(line) Then
                    line = input.ReadLine()
                End If
                GetStudio(line)
            End If
        End If

        line = SeekPattern(input, "^Scene \d")
        While line IsNot Nothing
            ProcessScene(line, input)
            line = SeekPattern(input, "^Scene \d")
        End While

        If Not String.IsNullOrEmpty(title) Then
            PrintEntry(Console.Out)
        End If
    End Sub

    Sub GetStars(ByVal input As TextReader)
        Dim line As String = input.ReadLine()
        line = Regex.Replace(line, "^Starring: ", "")
        Dim stars As String() = line.Split(",")
        For Each star In stars
            starList.Add(star.Trim)
        Next
    End Sub

    Sub GetNiches(ByVal input As TextReader)
        Dim line As String = input.ReadLine()
        If line IsNot Nothing Then
            niches = line.Replace("Niches: ", "")
            For Each niche In niches.Split(",")
                niche = niche.Trim()
                nicheTable.Add(niche)
            Next
        End If
    End Sub

    Function GetDirector(ByVal line As String) As Boolean
        If line IsNot Nothing AndAlso line.StartsWith("Director: ") Then
            director = line.Replace("Director: ", "").Trim()
            Return True
        End If
        Return False
    End Function

    Function GetStudio(ByVal line As String) As Boolean
        If line IsNot Nothing AndAlso line.StartsWith("Studio: ") Then
            studio = line.Replace("Studio: ", "").Trim()
            Return True
        End If
        Return False
    End Function

    Function TryGetField(ByVal line As String,
                         ByVal tag As String,
                         ByRef field As String) As Boolean
        If line IsNot Nothing AndAlso line.Trim.StartsWith(tag) Then
            field = line.Replace(tag, "").Trim()
            Return True
        End If
        Return False
    End Function

    Function SeekPattern(ByVal input As TextReader, ByVal pattern As String)
        Dim line As String = input.ReadLine()
        While line IsNot Nothing
            If Regex.IsMatch(line, pattern) Then
                Return line
            End If
            line = input.ReadLine()
        End While
        Return Nothing
    End Function

    Private Sub ProcessScene(ByVal line As String, ByVal input As TextReader)
        ' Line => "^Scene \d"
        Dim scene As New Scene
        Integer.TryParse(line.Substring(6), scene.SceneNumber)

        ' Process Performers.
        line = SeekPattern(input, "^Starring: ")
        If line IsNot Nothing Then
            line = line.Replace("Starring: ", "")
            scene.SceneText = line
            For Each star In line.Split(",")
                starList.Remove(star.Trim())
            Next
        End If

        ' Process Length.
        line = input.ReadLine()
        If line IsNot Nothing Then line = line.Replace("Length: ", "")
        scene.Length = line

        ' Process Niche.
        line = input.ReadLine()
        If line IsNot Nothing Then
            line = line.Replace("Niches: ", "")
            scene.NicheText = line
            For Each niche In line.Split(",")
                nicheTable.Add(niche.Trim())
            Next
        End If
        sceneList.Add(scene)
    End Sub

    Private Sub PrintEntry(ByVal output As TextWriter)
        output.WriteLine("# {0} (@VB)", title)
        output.WriteLine("c NR 200x {0}, {1} {{{2}}}", studio, director, SceneTotal)
        For Each scene In sceneList
            output.WriteLine("X <{0}> {1} [{2}]",
                             scene.SceneNumber, scene.SceneText, scene.Length)
            ' WriteList(output, scene.NicheText)
        Next
        Dim starCnt = 0
        Dim newLine As Boolean = False
        For Each star In starList
            newLine = True
            Select Case (starCnt)
                Case 0
                    output.Write("x {0}", star)
                Case 1
                    output.Write(", {0}", star)
                Case 2
                    output.WriteLine(", {0}", star)
                    newLine = False
            End Select
            starCnt = (starCnt + 1) Mod 3
        Next
        If (newLine) Then output.WriteLine()
        ' output.WriteLine("t {0}", niches)
        WriteList(output, "t", niches)
        output.WriteLine("s W4:vbox/{0:yyMMdd}", DateTime.Now)
        output.WriteLine()
    End Sub

    Public Sub WriteList(ByVal output As TextWriter,
                         ByVal tag As String,
                         ByVal list As String)
        Dim lines = FormatList(list.Split(","))
        For Each line In lines
            output.WriteLine("{1} {0}", line, tag)
        Next
    End Sub

    Public Function FormatList(ByVal list As IEnumerable(Of String), _
                               Optional ByVal lineLength As Integer = 72) As List(Of String)
        Dim result As New List(Of String)
        Dim buffer As New StringBuilder(lineLength)
        Dim needComma = False
        For Each item In list
            item = item.Trim()
            If buffer.Length > 0 And buffer.Length + item.Length > lineLength Then
                result.Add(buffer.ToString())
                buffer.Clear()
                needComma = False
            Else
                buffer.Append(If(needComma, ", ", "") + item)
                needComma = True
            End If
        Next
        If buffer.Length > 0 Then
            result.Add(buffer.ToString())
        End If
        Return result
    End Function

    Private Function SceneTotal() As Integer
        Return sceneList.Sum(Function(h) h.Minutes)
    End Function

    Private Function Encode(ByVal val As UInt32) As String
        Dim result As String = String.Empty
        Dim lastBit As UInt32 = val And &H1
        Dim run As UInt32
        Dim runLength = 1
        Dim bitPos As Integer = 1
        Dim bit As UInt32
        While bitPos < 32
            bit = (val >> bitPos) And &H1
            If bit = lastBit Then
                ' The run continues.
                runLength += 1
                bitPos += 1
            Else
                ' This bit starts a new run, ending the previous run
                If runLength >= 5 Then
                    ' Encode current run length.
                    result &= EncodeRun(lastBit, runLength)
                    lastBit = bit
                    runLength = 1
                    bitPos += 1
                Else
                    ' Encode a verbatim nibble.
                    If (bitPos <= 28) Then
                        Dim shift As Integer = CInt((bitPos / 4)) * 4
                        run = (val >> shift) And &HF
                        result &= String.Format("{0:x}", run)
                        bitPos = shift + 4
                        If bitPos < 32 Then
                            lastBit = (val >> bitPos) And &H1
                            runLength = 1
                            bitPos += 1
                        Else
                        End If
                    Else
                        ' Get the last few verbatim bits.
                        Dim mask = bitMask(32 - bitPos)
                        run = (val >> bitPos) & mask
                        result = result & String.Format("{0:x}", run)
                        Return result
                    End If
                End If
            End If
        End While
        result &= EncodeRun(lastBit, runLength)

        Return result
    End Function

    Public Function bitMask(ByVal bits As Integer) As UInt32
        Return Math.Pow(2, bits) - 1
    End Function

    'Private Function EncodeRun(ByVal run As UInteger,
    '                           ByVal runLength As Integer) As String
    '    Dim result As String
    '    If runLength <= 4 Then
    '        Debug.Assert(run <= bitMask(runLength))
    '        result = String.Format("{0:x}", run)
    '    Else
    '        Debug.Assert(run = 0 OrElse run = 1)
    '        result = EncodeRun(run = 1, String.Format("{0:x}", runLength))
    '    End If

    '    Return result

    'End Function

    Private debug = True

    Private Function EncodeRun(ByVal bit As Integer, ByVal runLength As Integer) As String
        If (debug) Then
            Return String.Format("[{0}:{1}]", bit, runLength)
        End If

        If runLength = 0 Then Return String.Empty

        Dim result As New StringBuilder(32)
        Dim hexrun As String = String.Format("{0:x}", runLength)
        Dim start As Integer = If(bit = 1, Asc("G"), Asc("g"))
        For Each digit As Char In hexrun
            Dim val As Integer
            If (digit >= "0" AndAlso digit <= "9") Then
                val = Asc(digit) - Asc("0")
            End If
            If (digit >= "a" AndAlso digit <= "f") Then
                val = 10 + Asc(digit) - Asc("a")
            End If
            result.Append(Chr(start + val))
        Next
        Return result.ToString()
    End Function

End Module


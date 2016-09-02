' ReplaceText.vbs
' Copied from answer at http://stackoverflow.com/questions/1115508/batch-find-and-edit-lines-in-txt-file

Option Explicit

Const ForAppending = 8
Const TristateFalse = 0 ' the value for ASCII
Const Overwrite = True

Const WindowsFolder = 0
Const SystemFolder = 1
Const TemporaryFolder = 2

Dim FileSystem
Dim Filename, OldText, NewText
Dim OriginalFile, TempFile, Line
Dim TempFilename

If WScript.Arguments.Count = 3 Then
    Filename = WScript.Arguments.Item(0)
    OldText = WScript.Arguments.Item(1)
    NewText = WScript.Arguments.Item(2)
Else
    Wscript.Echo "Usage: ReplaceText.vbs <Filename> <OldText> <NewText>"
    Wscript.Quit
End If

Set FileSystem = CreateObject("Scripting.FileSystemObject")
Dim tempFolder: tempFolder = FileSystem.GetSpecialFolder(TemporaryFolder)
TempFilename = FileSystem.GetTempName

If FileSystem.FileExists(TempFilename) Then
    FileSystem.DeleteFile TempFilename
End If

Set TempFile = FileSystem.CreateTextFile(TempFilename, Overwrite, TristateFalse)
Set OriginalFile = FileSystem.OpenTextFile(Filename)

Do Until OriginalFile.AtEndOfStream
    Line = OriginalFile.ReadLine

    If InStr(Line, OldText) > 0 Then
        Line = Replace(Line, OldText, NewText)
    End If 

    TempFile.WriteLine(Line)
Loop

OriginalFile.Close
TempFile.Close

FileSystem.DeleteFile Filename
FileSystem.MoveFile TempFilename, Filename

Wscript.Quit

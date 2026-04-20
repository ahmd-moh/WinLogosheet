Set fso = CreateObject("Scripting.FileSystemObject")
scriptdir = fso.GetParentFolderName(WScript.ScriptFullName)
Set WshShell = CreateObject("Wscript.Shell")
WshShell.Run "powershell -ExecutionPolicy Bypass -File """ & scriptdir & "\screencap-SET2.ps1""", 0, True

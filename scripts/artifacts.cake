#load "runHelpers.cake"

/// <summary>
///  Generate the scripts which target the SQLTOOLSSERVICE binaries.
/// </summary>
/// <param name="outputRoot">The root folder where the publised (or installed) binaries are located</param>
void CreateRunScript(string outputRoot, string scriptFolder, string framework)
{
    System.IO.Directory.CreateDirectory(scriptFolder);

    if (IsRunningOnWindows())
    {       
        var coreScript = System.IO.Path.Combine(scriptFolder, "SQLTOOLSSERVICE.Core.cmd");
        var sqlToolsServicePath = System.IO.Path.Combine(System.IO.Path.GetFullPath(outputRoot), framework, "MicrosoftSqlToolsServiceLayer.exe");
        var content = new string[] {
                "SETLOCAL",
                "",
                $"\"{sqlToolsServicePath}\" %*"
            };       
        if (System.IO.File.Exists(coreScript))
        {
            System.IO.File.Delete(coreScript);
        }
        System.IO.File.WriteAllLines(coreScript, content);
    }
    else
    {        
        var coreScript = System.IO.Path.Combine(scriptFolder, "SQLTOOLSSERVICE.Core");
        var sqlToolsServicePath = System.IO.Path.Combine(System.IO.Path.GetFullPath(outputRoot), framework, "MicrosoftSqlToolsServiceLayer");
        var content = new string[] {
                "#!/bin/bash",
                "",
                $"\"{sqlToolsServicePath}\" \"$@\""
            };      
       
        if (System.IO.File.Exists(coreScript))
        {
            System.IO.File.Delete(coreScript);
        }
        System.IO.File.WriteAllLines(coreScript, content);
        Run("chmod", $"+x \"{coreScript}\"");
    }
}

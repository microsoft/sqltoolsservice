#load "runhelpers.cake"

/// <summary>
///  Generate the scripts which target the SQLTOOLSSERVICE binaries.
/// </summary>
/// <param name="outputRoot">The root folder where the publised (or installed) binaries are located</param>
void CreateRunScript(string outputRoot, string scriptFolder)
{
    if (IsRunningOnWindows())
    {       
        var coreScript = System.IO.Path.Combine(scriptFolder, "SQLTOOLSSERVICE.Core.cmd");
        var sqlToolsServicePath = System.IO.Path.Combine(System.IO.Path.GetFullPath(outputRoot), "{0}", "SQLTOOLSSERVICE");
        var content = new string[] {
                "SETLOCAL",
                "",
                $"\"{sqlToolsServicePath}\" %*"
            };       
        if (System.IO.File.Exists(coreScript))
        {
            System.IO.File.Delete(coreScript);
        }
        content[2] = String.Format(content[2], "netcoreapp1.0");
        System.IO.File.WriteAllLines(coreScript, content);
    }
    else
    {        
        var coreScript = System.IO.Path.Combine(scriptFolder, "SQLTOOLSSERVICE.Core");
        var sqlToolsServicePath = System.IO.Path.Combine(System.IO.Path.GetFullPath(outputRoot), "{1}", "SQLTOOLSSERVICE");
        var content = new string[] {
                "#!/bin/bash",
                "",
                $"{{0}} \"{sqlToolsServicePath}{{2}}\" \"$@\""
            };      
       
        if (System.IO.File.Exists(coreScript))
        {
            System.IO.File.Delete(coreScript);
        }
        content[2] = String.Format(content[2], "", "netcoreapp1.0", "");
        System.IO.File.WriteAllLines(coreScript, content);
        Run("chmod", $"+x \"{coreScript}\"");
    }
}
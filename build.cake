#addin "nuget:?package=Newtonsoft.Json&version=9.0.1"
#addin "mssql.ResX"
#addin "mssql.XliffParser"

#load "scripts/runhelpers.cake"
#load "scripts/archiving.cake"
#load "scripts/artifacts.cake"
#tool "nuget:?package=Mono.TextTransform"

using System.ComponentModel;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Cake.Common.IO;
using XliffParser;

// Basic arguments
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
// Optional arguments
var testConfiguration = Argument("test-configuration", "Debug");
var installFolder = Argument("install-path",  System.IO.Path.Combine(Environment.GetEnvironmentVariable(IsRunningOnWindows() ? "USERPROFILE" : "HOME"),
                                                                        ".sqltoolsservice", "local"));
var requireArchive = HasArgument("archive");

// Working directory
var workingDirectory = System.IO.Directory.GetCurrentDirectory();

// System specific shell configuration
var shell = IsRunningOnWindows() ? "powershell" : "bash";
var shellArgument = IsRunningOnWindows() ? "-NoProfile /Command" : "-C";
var shellExtension = IsRunningOnWindows() ? "ps1" : "sh";

/// <summary>
///  Class representing build.json
/// </summary>
public class BuildPlan
{
    public IDictionary<string, string[]> TestProjects { get; set; }
    public string BuildToolsFolder { get; set; }
    public string ArtifactsFolder { get; set; }
    public bool UseSystemDotNetPath { get; set; }
    public string DotNetFolder { get; set; }
    public string DotNetInstallScriptURL { get; set; }
    public string DotNetChannel { get; set; }
    public string DotNetVersion { get; set; }
    public string[] Frameworks { get; set; }
    public string[] Rids { get; set; }
    public string[] MainProjects { get; set; }
}

var buildPlan = JsonConvert.DeserializeObject<BuildPlan>(
    System.IO.File.ReadAllText(System.IO.Path.Combine(workingDirectory, "build.json")));

// Folders and tools
var dotnetFolder = System.IO.Path.Combine(workingDirectory, buildPlan.DotNetFolder);
var dotnetcli = buildPlan.UseSystemDotNetPath ? "dotnet" : System.IO.Path.Combine(System.IO.Path.GetFullPath(dotnetFolder), "dotnet");
var toolsFolder = System.IO.Path.Combine(workingDirectory, buildPlan.BuildToolsFolder);

var sourceFolder = System.IO.Path.Combine(workingDirectory, "src");
var testFolder = System.IO.Path.Combine(workingDirectory, "test");

var artifactFolder = System.IO.Path.Combine(workingDirectory, buildPlan.ArtifactsFolder);
var publishFolder = System.IO.Path.Combine(artifactFolder, "publish");
var logFolder = System.IO.Path.Combine(artifactFolder, "logs");
var packageFolder = System.IO.Path.Combine(artifactFolder, "package");
var scriptFolder =  System.IO.Path.Combine(artifactFolder, "scripts");

/// <summary>
///  Clean artifacts.
/// </summary>
Task("Cleanup")
    .Does(() =>
{
    if (System.IO.Directory.Exists(artifactFolder))
    {
        System.IO.Directory.Delete(artifactFolder, true);
    }
    System.IO.Directory.CreateDirectory(artifactFolder);
    System.IO.Directory.CreateDirectory(logFolder);
    System.IO.Directory.CreateDirectory(packageFolder);
    System.IO.Directory.CreateDirectory(scriptFolder);
});

/// <summary>
///  Pre-build setup tasks.
/// </summary>
Task("Setup")
	.IsDependentOn("InstallDotnet")
	.IsDependentOn("InstallXUnit")
    .IsDependentOn("PopulateRuntimes")
    .Does(() =>
{
});

/// <summary>
///  Populate the RIDs for the specific environment.
///  Use default RID (+ win7-x86 on Windows) for now.
/// </summary>
Task("PopulateRuntimes")
    .Does(() =>
{
    buildPlan.Rids = new string[]
            {
                "default", // To allow testing the published artifact
                "win7-x64",
                "win7-x86",
                "ubuntu.14.04-x64",
                "ubuntu.16.04-x64",
                "centos.7-x64",
                "rhel.7.2-x64",
                "debian.8-x64",
                "fedora.23-x64",
                "opensuse.13.2-x64",
                "sles.12.2-x64",
                "osx.10.11-x64"
            };
});

/// <summary>
/// Install dotnet if it isn't already installed
/// </summary>
Task("InstallDotnet")
    .Does(() =>
{
	// Determine if `dotnet` is installed
	var dotnetInstalled = true;
	try
	{
		Run(dotnetcli, "--info");
		Information("dotnet is already installed, will skip download/install");
	}
	catch(Win32Exception)
	{
		// If we get this exception, dotnet isn't installed
		dotnetInstalled = false;
	}

	// Install dotnet if it isn't already installed
	if (!dotnetInstalled)
	{
		var installScript = $"dotnet-install.{shellExtension}";
		System.IO.Directory.CreateDirectory(dotnetFolder);
		var scriptPath = System.IO.Path.Combine(dotnetFolder, installScript);
		using (WebClient client = new WebClient())
		{
			client.DownloadFile($"{buildPlan.DotNetInstallScriptURL}/{installScript}", scriptPath);
		}
		if (!IsRunningOnWindows())
		{
			Run("chmod", $"+x '{scriptPath}'");
		}
		var installArgs = $"-Channel {buildPlan.DotNetChannel}";
		if (!String.IsNullOrEmpty(buildPlan.DotNetVersion))
		{
		  installArgs = $"{installArgs} -Version {buildPlan.DotNetVersion}";
		}
		if (!buildPlan.UseSystemDotNetPath)
		{
			installArgs = $"{installArgs} -InstallDir {dotnetFolder}";
		}
		Run(shell, $"{shellArgument} {scriptPath} {installArgs}");
		try
		{
			Run(dotnetcli, "--info");
		}
		catch (Win32Exception)
		{
			throw new Exception(".NET CLI failed to be installed");
		}
	}
});

/// <summary>
/// Installs XUnit nuget package
Task("InstallXUnit")
	.Does(() =>
{
	// Install the tools
    var nugetPath = Environment.GetEnvironmentVariable("NUGET_EXE");
    var arguments = $"install xunit.runner.console -ExcludeVersion -NoCache -Prerelease -OutputDirectory \"{toolsFolder}\"";
    if (IsRunningOnWindows())
    {
        Run(nugetPath, arguments);
    }
    else
    {
        Run("mono", $"\"{nugetPath}\" {arguments}");
    }
});

/// <summary>
///  Restore required NuGet packages.
/// </summary>
Task("Restore")
    .IsDependentOn("Setup")
    .Does(() =>
{
    RunRestore(dotnetcli, "restore", workingDirectory)
        .ExceptionOnError("Failed to restore projects under source code folder.");
});

/// <summary>
///  Build Test projects.
/// </summary>
Task("BuildTest")
    .IsDependentOn("Setup")
    .IsDependentOn("Restore")
    .Does(() =>
{
    foreach (var pair in buildPlan.TestProjects)
    {
        foreach (var framework in pair.Value)
        {
            var project = pair.Key;
            var projectFolder = System.IO.Path.Combine(testFolder, project);
            var runLog = new List<string>();
            Run(dotnetcli, $"build --framework {framework} --configuration {testConfiguration} \"{projectFolder}\"",
                    new RunOptions
                    {
                        StandardOutputListing = runLog
                    })
                .ExceptionOnError($"Building test {project} failed for {framework}.");
            System.IO.File.WriteAllLines(System.IO.Path.Combine(logFolder, $"{project}-{framework}-build.log"), runLog.ToArray());
        }
    }
});

/// <summary>
///  Run all tests for .NET Desktop and .NET Core
/// </summary>
Task("TestAll")
    .IsDependentOn("Test")
    .IsDependentOn("TestCore")
    .Does(() =>{});

/// <summary>
///  Run tests for .NET Core (using .NET CLI).
/// </summary>
Task("TestCore")
    .IsDependentOn("Setup")
    .IsDependentOn("Restore")
    .Does(() =>
{
    var testProjects = buildPlan.TestProjects
                                .Where(pair => pair.Value.Any(framework => framework.Contains("netcoreapp")))
                                .Select(pair => pair.Key)
                                .ToList();

    foreach (var testProject in testProjects)
    {
        var logFile = System.IO.Path.Combine(logFolder, $"{testProject}-core-result.trx");
        var testWorkingDir = System.IO.Path.Combine(testFolder, testProject);
        Run(dotnetcli, $"test -f netcoreapp2.0 --logger \"trx;LogFileName={logFile}\"", testWorkingDir)
            .ExceptionOnError($"Test {testProject} failed for .NET Core.");
    }
});

/// <summary>
///  Run tests for other frameworks (using XUnit2).
/// </summary>
Task("Test")
    .IsDependentOn("Setup")
	.IsDependentOn("SRGen")
    .IsDependentOn("CodeGen")
    .IsDependentOn("BuildTest")
    .Does(() =>
{
    foreach (var pair in buildPlan.TestProjects)
    {
        foreach (var framework in pair.Value)
        {
            // Testing against core happens in TestCore
            if (framework.Contains("netcoreapp"))
            {
                continue;
            }

            var project = pair.Key;
            var frameworkFolder = System.IO.Path.Combine(testFolder, project, "bin", testConfiguration, framework);
            var runtime = System.IO.Directory.GetDirectories(frameworkFolder).First();
            var instanceFolder = System.IO.Path.Combine(frameworkFolder, runtime);

            // Copy xunit executable to test folder to solve path errors
            var xunitToolsFolder = System.IO.Path.Combine(toolsFolder, "xunit.runner.console", "tools");
            var xunitInstancePath = System.IO.Path.Combine(instanceFolder, "xunit.console.exe");
            System.IO.File.Copy(System.IO.Path.Combine(xunitToolsFolder, "xunit.console.exe"), xunitInstancePath, true);
            System.IO.File.Copy(System.IO.Path.Combine(xunitToolsFolder, "xunit.runner.utility.desktop.dll"), System.IO.Path.Combine(instanceFolder, "xunit.runner.utility.desktop.dll"), true);
            var targetPath = System.IO.Path.Combine(instanceFolder, $"{project}.dll");
            var logFile = System.IO.Path.Combine(logFolder, $"{project}-{framework}-result.xml");
            var arguments = $"\"{targetPath}\" -parallel none --logger \"trx;LogFileName={logFile}\"";
            if (IsRunningOnWindows())
            {
                Run(xunitInstancePath, arguments, instanceFolder)
                    .ExceptionOnError($"Test {project} failed for {framework}");
            }
            else
            {
                Run("mono", $"\"{xunitInstancePath}\" {arguments}", instanceFolder)
                    .ExceptionOnError($"Test {project} failed for {framework}");
            }
        }
    }
});

/// <summary>
///  Build, publish and package artifacts.
///  Targets all RIDs specified in build.json unless restricted by RestrictToLocalRuntime.
///  No dependencies on other tasks to support quick builds.
/// </summary>
Task("OnlyPublish")
    .IsDependentOn("Setup")
	.IsDependentOn("SRGen")
    .IsDependentOn("CodeGen")
    .Does(() =>
{    
    foreach (var project in buildPlan.MainProjects)
    {
        var projectFolder = System.IO.Path.Combine(sourceFolder, project);
        foreach (var framework in buildPlan.Frameworks)
        {
            foreach (var runtime in buildPlan.Rids)
            {
                var outputFolder = System.IO.Path.Combine(publishFolder, project, runtime, framework);
                var publishArguments = "publish";
                if (!runtime.Equals("default"))
                {
                    publishArguments = $"{publishArguments} --runtime {runtime}";
                }
                publishArguments = $"{publishArguments} --framework {framework} --configuration {configuration}";
                publishArguments = $"{publishArguments} --output \"{outputFolder}\" \"{projectFolder}\"";
                Run(dotnetcli, publishArguments)
                    .ExceptionOnError($"Failed to publish {project} / {framework}");
                //Setting the rpath for System.Security.Cryptography.Native.dylib library
                //Only required for mac. We're assuming the openssl is installed in /usr/local/opt/openssl
                //If that's not the case user has to run the command manually
                if (!IsRunningOnWindows() && runtime.Contains("osx"))
                {    
                    Run("install_name_tool",  "-add_rpath /usr/local/opt/openssl/lib " + outputFolder + "/System.Security.Cryptography.Native.dylib");
                }
                if (requireArchive)
                {
                    Package(runtime, framework, outputFolder, packageFolder, project.ToLower(), workingDirectory);
                }
            }
        }
        CreateRunScript(System.IO.Path.Combine(publishFolder, project, "default"), scriptFolder);
    }    
});

/// <summary>
///  Alias for OnlyPublish.
///  Targets all RIDs as specified in build.json.
/// </summary>
Task("AllPublish")
    .IsDependentOn("Restore")
    .IsDependentOn("OnlyPublish")
    .Does(() =>
{
});

/// <summary>
///  Restrict the RIDs for the local default.
/// </summary>
Task("RestrictToLocalRuntime")
    .IsDependentOn("Setup")
    .Does(() =>
{
    buildPlan.Rids = new string[] {"default"};
});

/// <summary>
///  Alias for OnlyPublish.
///  Restricts publishing to local RID.
/// </summary>
Task("LocalPublish")
    .IsDependentOn("Restore")
    .IsDependentOn("RestrictToLocalRuntime")
    .IsDependentOn("OnlyPublish")
    .Does(() =>
{
});

/// <summary>
///  Test the published binaries if they start up without errors.
///  Uses builds corresponding to local RID.
/// </summary>
Task("TestPublished")
    .IsDependentOn("Setup")
    .Does(() =>
{
    foreach (var project in buildPlan.MainProjects)
    {
        var projectFolder = System.IO.Path.Combine(sourceFolder, project);
        var scriptsToTest = new string[] {"SQLTOOLSSERVICE.Core"};//TODO
        foreach (var script in scriptsToTest)
        {
            var scriptPath = System.IO.Path.Combine(scriptFolder, script);
            var didNotExitWithError = Run($"{shell}", $"{shellArgument}  \"{scriptPath}\" -s \"{projectFolder}\" --stdio",
                                        new RunOptions
                                        {
                                            TimeOut = 10000
                                        })
                                    .DidTimeOut;
            if (!didNotExitWithError)
            {
                throw new Exception($"Failed to run {script}");
            }
        }
    }
});

/// <summary>
///  Clean install path.
/// </summary>
Task("CleanupInstall")
    .Does(() =>
{
    if (System.IO.Directory.Exists(installFolder))
    {
        System.IO.Directory.Delete(installFolder, true);
    }
    System.IO.Directory.CreateDirectory(installFolder);
});

/// <summary>
///  Quick build.
/// </summary>
Task("Quick")
    .IsDependentOn("Cleanup")
    .IsDependentOn("LocalPublish")
    .Does(() =>
{
});

/// <summary>
///  Quick build + install.
/// </summary>
Task("Install")
    .IsDependentOn("Cleanup")
    .IsDependentOn("LocalPublish")
    .IsDependentOn("CleanupInstall")
    .Does(() =>
{
    foreach (var project in buildPlan.MainProjects)
    {
        foreach (var framework in buildPlan.Frameworks)
        {
            var outputFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(publishFolder, project, "default", framework));
            var targetFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(installFolder, framework));
            // Copy all the folders
            foreach (var directory in System.IO.Directory.GetDirectories(outputFolder, "*", SearchOption.AllDirectories))
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(targetFolder, directory.Substring(outputFolder.Length + 1)));
            //Copy all the files
            foreach (string file in System.IO.Directory.GetFiles(outputFolder, "*", SearchOption.AllDirectories))
                System.IO.File.Copy(file, System.IO.Path.Combine(targetFolder, file.Substring(outputFolder.Length + 1)), true);
        }
        CreateRunScript(installFolder, scriptFolder);
    }
});

/// <summary>
///  Full build targeting all RIDs specified in build.json.
/// </summary>
Task("All")
    .IsDependentOn("Cleanup")
    .IsDependentOn("Restore")
    .IsDependentOn("TestAll")
    .IsDependentOn("AllPublish")
    //.IsDependentOn("TestPublished")
    .Does(() =>
{
});

/// <summary>
///  Full build targeting local RID.
/// </summary>
Task("Local")
    .IsDependentOn("Cleanup")
    .IsDependentOn("Restore")
    .IsDependentOn("TestAll")
    .IsDependentOn("LocalPublish")
   // .IsDependentOn("TestPublished")
    .Does(() =>
{
});

/// <summary>
///  Update the package versions within project.json files.
///  Uses depversion.json file as input.
/// </summary>
Task("SetPackageVersions")
    .Does(() =>
{
    var jDepVersion = JObject.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(workingDirectory, "depversion.json")));
    var projects = System.IO.Directory.GetFiles(sourceFolder, "project.json", SearchOption.AllDirectories).ToList();
    projects.AddRange(System.IO.Directory.GetFiles(testFolder, "project.json", SearchOption.AllDirectories));
    foreach (var project in projects)
    {
        var jProject = JObject.Parse(System.IO.File.ReadAllText(project));
        var dependencies = jProject.SelectTokens("dependencies")
                            .Union(jProject.SelectTokens("frameworks.*.dependencies"))
                            .SelectMany(dependencyToken => dependencyToken.Children<JProperty>());
        foreach (JProperty dependency in dependencies)
        {
            if (jDepVersion[dependency.Name] != null)
            {
                dependency.Value = jDepVersion[dependency.Name];
            }
        }
        System.IO.File.WriteAllText(project, JsonConvert.SerializeObject(jProject, Formatting.Indented));
    }
});

/// <summary>
/// Executes SRGen to create a resx file and associated designer C# file
/// </summary>
Task("SRGen")
	.Does(() =>
{
    var projects = System.IO.Directory.GetFiles(sourceFolder, "project.json", SearchOption.AllDirectories).ToList();
    var locTemplateDir = System.IO.Path.Combine(sourceFolder, "../localization"); 

    foreach(var project in projects) {
        var projectDir = System.IO.Path.GetDirectoryName(project);
        var localizationDir = System.IO.Path.Combine(projectDir, "Localization");
        var projectName = (new System.IO.DirectoryInfo(projectDir)).Name;
        var projectNameSpace = projectName + ".Localization";
        var projectStrings = System.IO.Path.Combine(localizationDir, "sr.strings");

        if (!System.IO.File.Exists(projectStrings))
        {
            Information("Project {0} doesn't contain 'sr.strings' file", projectName);
            continue;
        }

        var srgenPath = System.IO.Path.Combine(toolsFolder, "Microsoft.DataTools.SrGen", "lib", "netcoreapp2.0", "srgen.dll");
        var outputResx = System.IO.Path.Combine(localizationDir, "sr.resx");
        var inputXliff = System.IO.Path.Combine(localizationDir, "transXliff");
        var outputXlf = System.IO.Path.Combine(localizationDir, "sr.xlf");
        var outputCs = System.IO.Path.Combine(localizationDir, "sr.cs");

        // Delete preexisting resx and designer files
        if (System.IO.File.Exists(outputResx))
        {
            System.IO.File.Delete(outputResx);
        }
        if (System.IO.File.Exists(outputCs))
        {
            System.IO.File.Delete(outputCs);
        }

        if (!System.IO.Directory.Exists(inputXliff)) 
        {
            System.IO.Directory.CreateDirectory(inputXliff);
        }

        // Run SRGen
        var dotnetArgs = string.Format("{0} -or \"{1}\" -oc \"{2}\" -ns \"{3}\" -an \"{4}\" -cn SR -l CS -dnx \"{5}\"",
        srgenPath, outputResx, outputCs, projectName, projectNameSpace, projectStrings);
        Information("{0}", dotnetArgs);
        Run(dotnetcli, dotnetArgs)
        .ExceptionOnError("Failed to run SRGen.");

        // Update XLF file from new Resx file
        var doc = new XliffParser.XlfDocument(outputXlf);
        doc.UpdateFromSource();
        doc.Save();

        // Update ResX files from new xliff files
        var xlfDocNames = System.IO.Directory.GetFiles(inputXliff, "*.xlf", SearchOption.AllDirectories).ToList();
        foreach(var docName in xlfDocNames)
        {
            // load our language XLIFF
            var xlfDoc = new XliffParser.XlfDocument(docName);
            var xlfFile = xlfDoc.Files.Single();

            // load a language template 
            var templateFileLocation = System.IO.Path.Combine(locTemplateDir, System.IO.Path.GetFileName(docName) + ".template");
            var templateDoc = new XliffParser.XlfDocument(templateFileLocation);
            var templateFile = templateDoc.Files.Single(); 

            // iterate through our tranlation units and prune invalid units
            foreach (var unit in xlfFile.TransUnits)
            {
                // if a unit does not have a target it is invalid 
                if (unit.Target != null) {
                    templateFile.AddTransUnit(unit.Id, unit.Source, unit.Target, 0, 0);
                }
            } 

            // export modified template to RESX
            var newPath = System.IO.Path.Combine(localizationDir, System.IO.Path.GetFileName(docName));
            templateDoc.SaveAsResX(newPath.Replace("xlf","resx"));        
        }
    }
});

/// <summary>
/// Executes T4Template-based code generators
/// </summary>
Task("CodeGen")
       .Does(() =>
{
       var t4Files = GetFiles(sourceFolder + "/**/*.tt");
       foreach(var t4Template in t4Files)
       {
              TransformTemplate(t4Template, new TextTransformSettings {});
       }
});


/// <summary>
///  Default Task aliases to Local.
/// </summary>
Task("Default")
    .IsDependentOn("Local")
    .Does(() =>
{
});

/// <summary>
///  Default to Local.
/// </summary>
RunTarget(target);

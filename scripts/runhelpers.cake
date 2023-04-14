using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

/// <summary>
///  Class encompassing the optional settings for running processes.
/// </summary>
public class RunOptions
{
    /// <summary>
    ///  The working directory of the process.
    /// </summary>
    public string WorkingDirectory { get; set; }
    /// <summary>
    ///  Stream to log std out messages to
    /// </summary>
    public StreamWriter StandardOutputWriter { get; set; }
    /// <summary>
    ///  Stream to log std err messages to
    /// </summary>
    public StreamWriter StandardErrorWriter { get; set; }
    /// <summary>
    ///  Desired maximum time-out for the process
    /// </summary>
    public int TimeOut { get; set; }
}

/// <summary>
///  Wrapper for the exit code and state.
///  Used to query the result of an execution with method calls.
/// </summary>
public struct ExitStatus
{
    private string _cmd;
    private string _args;
    private int _exitCode;
    private bool _timeOut;
    /// <summary>
    ///  Default constructor when the execution potentially timed out.
    /// </summary>
    /// <param name="process">The process this status is for</param>
    /// <param name="timeOut">True if the execution timed out</param>
    public ExitStatus(string cmd, string args, int exitCode, bool timeOut = false)
    {
        this._cmd = cmd;
        this._args = args;
        this._exitCode = exitCode;
        this._timeOut = timeOut;
    }
    /// <summary>
    ///  Flag signalling that the execution timed out.
    /// </summary>
    public bool DidTimeOut { get { return _timeOut; } }
    /// <summary>
    ///  Implicit conversion from ExitStatus to the exit code.
    /// </summary>
    /// <param name="exitStatus">The exit status</param>
    /// <returns>The exit code</returns>
    public static implicit operator int(ExitStatus exitStatus)
    {
        return exitStatus._exitCode;
    }
    /// <summary>
    ///  Trigger Exception for non-zero exit code.
    /// </summary>
    /// <param name="errorMessage">The message to use in the Exception</param>
    /// <returns>The exit status for further queries</returns>
    public ExitStatus ExceptionOnError(string errorMessage)
    {
        if (this._exitCode != 0)
        {
            throw new Exception(errorMessage + $"\nCommand: {this._cmd} {this._args}");
        }
        return this;
    }
}

/// <summary>
///  Run the given executable with the given arguments.
/// </summary>
/// <param name="exec">Executable to run</param>
/// <param name="args">Arguments</param>
/// <returns>The exit status for further queries</returns>
ExitStatus Run(string exec, string args)
{
    return Run(exec, args, new RunOptions());
}

/// <summary>
///  Run the given executable with the given arguments.
/// </summary>
/// <param name="exec">Executable to run</param>
/// <param name="args">Arguments</param>
/// <param name="workingDirectory">Working directory</param>
/// <returns>The exit status for further queries</returns>
ExitStatus Run(string exec, string args, string workingDirectory)
{
    return Run(exec, args,
        new RunOptions()
        {
            WorkingDirectory = workingDirectory
        });
}

/// <summary>
///  Run the given executable with the given arguments.
/// </summary>
/// <param name="exec">Executable to run</param>
/// <param name="args">Arguments</param>
/// <param name="runOptions">Optional settings</param>
/// <returns>The exit status for further queries</returns>
ExitStatus Run(string exec, string args, RunOptions runOptions)
{
    var workingDirectory = runOptions.WorkingDirectory ?? System.IO.Directory.GetCurrentDirectory();
    var process = System.Diagnostics.Process.Start(
            new ProcessStartInfo(exec, args)
            {
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = runOptions.StandardOutputWriter != null,
                RedirectStandardError = runOptions.StandardErrorWriter != null,
            });
    if (runOptions.StandardOutputWriter != null)
    {
        process.OutputDataReceived += (s, e) =>
        {
            if (e.Data != null)
            {
                runOptions.StandardOutputWriter.WriteLine(e.Data);
            }
        };
        process.BeginOutputReadLine();
    }
    if (runOptions.StandardErrorWriter != null)
    {
        process.ErrorDataReceived += (s, e) =>
        {
            if (e.Data != null)
            {
                runOptions.StandardErrorWriter.WriteLine(e.Data);
            }
        };
        process.BeginErrorReadLine();
    }
    if (runOptions.TimeOut == 0)
    {
        process.WaitForExit();
        return new ExitStatus(exec, args, process.ExitCode);
    }
    else
    {
        bool finished = process.WaitForExit(runOptions.TimeOut);
        if (finished)
        {
            return new ExitStatus(exec, args, process.ExitCode);
        }
        else
        {
            KillProcessTree(process);
            return new ExitStatus(exec, args, 0, true);
        }
    }
}

/// <summary>
///  Run restore with the given arguments
/// </summary>
/// <param name="exec">Executable to run</param>
/// <param name="args">Arguments</param>
/// <param name="runOptions">Optional settings</param>
/// <returns>The exit status for further queries</returns>
ExitStatus RunRestore(string exec, string args, string workingDirectory)
{
    Information("Restoring packages....");
    var exitStatus = Run(exec, args, new RunOptions {
        WorkingDirectory = workingDirectory
    }).ExceptionOnError($"Error restoring packages.");

    Information("Package restore successful!");
    return exitStatus;
}

/// <summary>
///  Kill the given process and all its child processes.
/// </summary>
/// <param name="process">Root process</param>
public void KillProcessTree(Process process)
{
    // Child processes are not killed on Windows by default
    // Use TASKKILL to kill the process hierarchy rooted in the process
    if (IsRunningOnWindows())
    {
        StartProcess($"TASKKILL",
            new ProcessSettings
            {
                Arguments = $"/PID {process.Id} /T /F",
            });
    }
    else
    {
        process.Kill();
    }
}

public void DotnetPack(string outputFolder, string projectFolder, string project) {
    var logPath = System.IO.Path.Combine(logFolder, $"{project}-pack.log");
    using (var logWriter = new StreamWriter(logPath)) {
        Information($"Packaging {projectFolder}");
        Run(dotnetcli, $"pack --configuration {configuration} --output {outputFolder} \"{projectFolder}\"",
            new RunOptions
            {
                StandardOutputWriter = logWriter,
                StandardErrorWriter = logWriter
            })
        .ExceptionOnError($"Packaging {project} failed. See {logPath} for details.");
    }
}

public void DotnetPackNuspec(string outputFolder, string projectFolder, string project) {
    var logPath = System.IO.Path.Combine(logFolder, $"{project}-pack.log");
    using (var logWriter = new StreamWriter(logPath)) {
        Information($"Packaging {projectFolder}");
        Run(nugetcli, $"pack {projectFolder}\\{project}.nuspec -OutputDirectory {outputFolder}",
            new RunOptions
            {
                StandardOutputWriter = logWriter,
                StandardErrorWriter = logWriter
            })
        .ExceptionOnError($"Packaging {project} failed. See {logPath} for details.");
    }

}

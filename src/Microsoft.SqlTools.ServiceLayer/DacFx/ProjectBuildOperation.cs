//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using Microsoft.Build.Evaluation;
using Microsoft.SqlServer.Dac.Deployment;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Class to represent an in-progress build dacpac from project operation
    /// </summary>
    class ProjectBuildOperation : ITaskOperation
    {
        private CancellationTokenSource cancellation = new CancellationTokenSource();
        private bool disposed = false;
        private string buildOutput;
        private string buildError;
        private string projectFilePath;
        private string netCorePath;
        public ProjectBuildParams Parameters { get; }
        public string ErrorMessage { get; set; }

        public SqlTask SqlTask { get; set; }

        public string OperationId { get; }

        public ProjectBuildOperation(ProjectBuildParams parameters)
        {
            Validate.IsNotNull("parameters", parameters);

            this.Parameters = parameters;
            this.projectFilePath = Parameters.SqlProjectPath;
            this.netCorePath = Parameters.DotNetRootPath;
            this.OperationId = Guid.NewGuid().ToString();
        }

        public void Execute(TaskExecutionMode mode)
        {
            // use default if ADS did not provide a path
            if (string.IsNullOrEmpty(netCorePath))
            {
                // Fall back to default installation
                netCorePath = GetDefaultDotNetInstallPath();
            }

            // Validate path
            if (string.IsNullOrEmpty(netCorePath) || !File.Exists(netCorePath))
            {
                throw new ArgumentException("Dotnet exe path is not valid. Ensure that .NET core SDK is installed.");
            }

            // validate project path
            if (string.IsNullOrEmpty(projectFilePath) || !File.Exists(projectFilePath))
            {
                throw new ArgumentException("Project Path is not found of is invalid. Ensure that sqlproj is present.");
            }

            // clean output 
            CleanOutputDir(projectFilePath);

            // start build process
            ExecuteTool();

            // send back any error
            if (!string.IsNullOrEmpty(buildError))
            {
                throw new BuildFailedException($"Build Failed with Error : {buildError}");
            }
            else
            {
                string outputDacPacPath = GetOutputDacPac(projectFilePath);
                if (outputDacPacPath == null)
                {
                    throw new BuildFailedException($"Dacpac not created. Build output : {buildOutput}");
                }
            }

            // add output to task message
            if (this.SqlTask != null)
            {
                this.SqlTask.AddMessage(buildOutput);
            }
        }

        public void Cancel()
        {
            if (!this.cancellation.IsCancellationRequested)
            {
                Logger.Write(TraceEventType.Verbose, string.Format("Cancel invoked for OperationId {0}", this.OperationId));
                this.cancellation.Cancel();
            }
        }

        /// <summary>
        /// Disposes the operation.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                this.Cancel();
                disposed = true;
            }
        }

        private void ExecuteTool()
        {
            DirectoryInfo workingDirectory = GetProjectDirectory(this.projectFilePath);
            string commandLineArgs = GetCommandlineArguments(this.projectFilePath);

            FileInfo dotnet = new FileInfo(this.netCorePath);

            Debug.Assert(dotnet.Exists, "Unable to find dotnet.exe in path " + dotnet.FullName);

            ProcessStartInfo info = new ProcessStartInfo()
            {
                Arguments = commandLineArgs,
                CreateNoWindow = true,
                FileName = dotnet.FullName,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = workingDirectory.FullName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            // It turns out that it's necessary to handle the redirected stdout and stderr asynchronously
            // This is because the naive approach (using ReadToEnd on the stream) fails when the target process
            // outputs more text to stdout or stderr than the pre-allocated buffer can hold.
            // By reading the contents asynchronously we avoid this issue, at the cost of increased complexity.
            object threadSharedLock = new object();
            StringBuilder threadShared_ReceivedOutput = new StringBuilder();
            StringBuilder threadShared_ReceivedErrors = new StringBuilder();

            int interlocked_outputCompleted = 0;
            int interlocked_errorsCompleted = 0;

            if (this.cancellation.IsCancellationRequested)
            {
                throw new OperationCanceledException(this.cancellation.Token);
            }

            using (Process process = new Process())
            {
                process.StartInfo = info;
                // the OutputDataReceived delegate is called on a separate thread as output data arrives from the process
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        lock (threadSharedLock)
                        {
                            threadShared_ReceivedOutput.AppendLine(e.Data);
                        }
                    }
                    else
                    {
                        System.Threading.Interlocked.Increment(ref interlocked_outputCompleted);
                    }
                };

                // the ErrorDataReceived delegateis called on a separate thread as output data arrives from the process
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        lock (threadSharedLock)
                        {
                            threadShared_ReceivedErrors.AppendLine(e.Data);
                        }
                    }
                    else
                    {
                        System.Threading.Interlocked.Increment(ref interlocked_errorsCompleted);
                    }
                };

                Console.WriteLine("Invoking: {0} {1}", info.FileName, info.Arguments);
                process.Start();
                process.BeginOutputReadLine(); // starts a separate thread to monitor the redirected stdout
                process.BeginErrorReadLine(); // starts a separate thread to monitor the redirected stderr

                TimeSpan timeout = TimeSpan.FromMinutes(5);
                Stopwatch timer = new Stopwatch();
                timer.Start();
                process.WaitForExit(timeout.Milliseconds);

                do
                {
                    System.Threading.Thread.MemoryBarrier();
                    System.Threading.Thread.Sleep(0);
                }
                while ((interlocked_outputCompleted < 1 || interlocked_errorsCompleted < 1) && timer.Elapsed < timeout);
            }

            lock (threadSharedLock)
            {
                buildOutput = threadShared_ReceivedOutput.ToString();
                buildError = threadShared_ReceivedErrors.ToString();
            }
        }

        internal static string GetDefaultDotNetInstallPath()
        {
            string netcorepath = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                netcorepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet\\dotnet.exe");
            }
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                netcorepath = Path.Combine(Environment.GetEnvironmentVariable("HOME"), "dotnet", "dotnet");
            }
            return File.Exists(netcorepath) ? netcorepath : null;
        }

        internal static string GetCommandlineArguments(string projectFile)
        {
            string projectPath = projectFile;
            string netcoreParameter = " build /p:NetCoreBuild=true ";

            return netcoreParameter + projectPath;
        }

        internal static DirectoryInfo GetProjectDirectory(string projectFile)
        {
            FileInfo projectFileInfo = new FileInfo(projectFile);
            return projectFileInfo.Directory;
        }

        internal static string GetOutputDacPac(string projectFile)
        {
            string outpath = null;
            string dacpacName = null;
            try
            {
                ProjectCollection pc = new ProjectCollection();
                Project p = new Project(projectFile, null, "15.0", pc, ProjectLoadSettings.IgnoreMissingImports);
                outpath = p.GetProperty("OutputPath").EvaluatedValue;
                dacpacName = p.GetProperty("AssemblyName").EvaluatedValue;
            }
            catch (Exception ex)
            {
                outpath = outpath ?? "bin\\debug";
            }

            if (outpath != null)
            {
                if (!Path.IsPathFullyQualified(outpath))
                {
                    outpath = Path.Combine(GetProjectDirectory(projectFile).FullName, outpath, dacpacName + ".dacpac");
                }
            }

            return File.Exists(outpath) ? outpath : null;
        }

        internal static void CleanOutputDir(string projectFile)
        {
            string dacpacPath = GetOutputDacPac(projectFile);

            if (dacpacPath != null)
            {
                DirectoryInfo output = new FileInfo(dacpacPath).Directory;
                if (Directory.Exists(output.FullName))
                {
                    output.Delete(recursive: true);
                }
            }
        }
    }
}

//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.DacFx
{
    [TestFixture]
    public class SqlPackageTests
    {
        private const string createTableScript = "CREATE TABLE [Table1] ([Col1] INT NOT NULL, [Col2] NVARCHAR(10) NULL)";

        protected DirectoryInfo WorkingDirectory { get; set; }

        /// <summary>
        /// Sets up the Working Directory before every test run.
        /// </summary>
        [SetUp]
        public void TestInit()
        {
            WorkingDirectory = new DirectoryInfo(Path.Combine(TestContext.CurrentContext.TestDirectory, TestContext.CurrentContext.Test.Name));
            Directory.CreateDirectory(WorkingDirectory.FullName);
            Console.WriteLine($"Test working directory: {WorkingDirectory}");
        }

        /// <summary>
        /// Empties and deletes the Working Directory after each test run.
        /// </summary>
        [TearDown]
        public void TestCleanup()
        {
            Directory.Delete(WorkingDirectory.FullName, true);
        }

        /// <summary>
        /// This test verifies Extract and Publish operations with SqlPackage.
        /// </summary>
        [Test]
        public async Task SqlPackageExtractPublish()
        {
            using SqlTestDb testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, query: createTableScript, dbNamePrefix: "SqlpackageExtractPublish");

            string packagePath = Path.Combine(WorkingDirectory.FullName, $"{testDb.DatabaseName}.dacpac");
            string arguments = $"/Action:Extract /TargetFile:\"{packagePath}\" /SourceConnectionString:\"{testDb.ConnectionString}\"";
            string stdError, stdOutput;

            // Verify Extract
            ExecuteTool(WorkingDirectory.FullName, arguments, out stdError, out stdOutput);

            Assert.AreEqual(string.Empty, stdError, "SqlPackage Extract failed with error: " + stdError);
            Assert.IsTrue(stdOutput.Contains("Successfully extracted database"), "SqlPackage Extract was not successful.");
            Assert.IsTrue(File.Exists(packagePath), "Extracted package not found at: " + packagePath);

            // Verify Publish to same database
            arguments = $"/Action:Publish /SourceFile:\"{packagePath}\" /TargetConnectionString:\"{testDb.ConnectionString}\"";

            ExecuteTool(WorkingDirectory.FullName, arguments, out stdError, out stdOutput);

            Assert.AreEqual(string.Empty, stdError, "SqlPackage Publish failed with error: " + stdError);
            Assert.IsTrue(stdOutput.Contains("Successfully published database"), "SqlPackage Publish was not successful.");
        }

        /// <summary>
        /// This test verifies Export and Import operations with SqlPackage.
        /// </summary>
        [Test]
        public async Task SqlPackageExportImport()
        {
            using SqlTestDb sourceDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, query: createTableScript, dbNamePrefix: "SqlpackageExport");

            string packagePath = Path.Combine(WorkingDirectory.FullName, $"{sourceDb.DatabaseName}.bacpac");
            string arguments = $"/Action:Export /TargetFile:\"{packagePath}\" /SourceConnectionString:\"{sourceDb.ConnectionString}\"";
            string stdError, stdOutput;

            // Verify Export
            ExecuteTool(WorkingDirectory.FullName, arguments, out stdError, out stdOutput);

            Assert.AreEqual(string.Empty, stdError, "SqlPackage Export failed with error: " + stdError);
            Assert.IsTrue(stdOutput.Contains("Successfully exported database"), "SqlPackage Export was not successful.");
            Assert.IsTrue(File.Exists(packagePath), "Exported package not found at: " + packagePath);

            // Verify Import to new database
            using SqlTestDb targetDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, dbNamePrefix: "SqlpackageImport");
            arguments = $"/Action:Import /SourceFile:\"{packagePath}\" /TargetConnectionString:\"{targetDb.ConnectionString}\"";

            ExecuteTool(WorkingDirectory.FullName, arguments, out stdError, out stdOutput);

            Assert.AreEqual(string.Empty, stdError, "SqlPackage Import failed with error: " + stdError);
            Assert.IsTrue(stdOutput.Contains("Successfully imported database"), "SqlPackage Import was not successful.");
        }

        /// <summary>
        /// Executes the SqlPackage tool in the given <paramref name="workingDirectory"/> with <paramref name="arguments"/>.
        /// </summary>
        private static void ExecuteTool(string workingDirectory, string arguments, out string stdError, out string stdOutput)
        {
            // It turns out that it's necessary to handle the redirected stdout and stderr asynchronously
            // This is because the naive approach (using ReadToEnd on the stream) fails when the target process
            // outputs more text to stdout or stderr than the pre-allocated buffer can hold.
            // By reading the contents asynchronously we avoid this issue, at the cost of increased complexity.
            object threadSharedLock = new object();
            StringBuilder threadShared_ReceivedOutput = new StringBuilder();
            StringBuilder threadShared_ReceivedErrors = new StringBuilder();

            int interlocked_outputCompleted = 0;
            int interlocked_errorsCompleted = 0;

            using (Process sqlpackage = new Process())
            {
                // Set up the files necessary to run SqlPackage
                sqlpackage.StartInfo = SetupSqlpackage(workingDirectory, arguments);
                
                // the OutputDataReceived delegate is called on a separate thread as output data arrives from the process
                sqlpackage.OutputDataReceived += (sender, e) =>
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

                // the ErrorDataReceived delegate is called on a separate thread as output data arrives from the process
                sqlpackage.ErrorDataReceived += (sender, e) =>
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

                Console.WriteLine($"Invoking: sqlpackage {arguments}");
                sqlpackage.Start();
                sqlpackage.BeginOutputReadLine(); // starts a separate thread to monitor the redirected stdout
                sqlpackage.BeginErrorReadLine(); // starts a separate thread to monitor the redirected stderr

                TimeSpan timeout = TimeSpan.FromMinutes(5);
                Stopwatch timer = new Stopwatch();
                timer.Start();
                sqlpackage.WaitForExit(timeout.Milliseconds);

                do
                {
                    System.Threading.Thread.MemoryBarrier();
                    System.Threading.Thread.Sleep(0);
                }
                while ((interlocked_outputCompleted < 1 || interlocked_errorsCompleted < 1) && timer.Elapsed < timeout);
            }

            lock (threadSharedLock)
            {
                stdOutput = threadShared_ReceivedOutput.ToString();
                stdError = threadShared_ReceivedErrors.ToString();
            }

            Console.WriteLine("SqlPackage.exe Standard out:");
            Console.Write(stdOutput);
            Console.WriteLine("SqlPackage.exe Standard err:");
            Console.Write(stdError);
        }

        /// <summary>
        /// Sets up a SqlPackage process in the <paramref name="workingDirectory"/> with arguments <paramref name="arguments"/>.
        /// Runs "dotnet publish" for the current platform which publishes the necessary .NET runtime DLLs.
        /// </summary>
        /// <returns>
        /// A ProcessStartInfo object associated with the SqlPackage executable.
        /// </returns>
        private static ProcessStartInfo SetupSqlpackage(string workingDirectory, string arguments)
        {
            // Executable name and runtime identifier for dotnet publish are different per platform
            string sqlpackageExecutable, runtimeIdentifier;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                sqlpackageExecutable = "sqlpackage.exe";
                runtimeIdentifier = "win-x64";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                sqlpackageExecutable = "sqlpackage";
                runtimeIdentifier = "linux-x64";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                sqlpackageExecutable = "sqlpackage";
                runtimeIdentifier = "osx-x64";
            }
            else
            {
                // throw new NotSupportedException("SqlPackage is not supported on the current operating system: " + Environment.OSVersion.Platform);
                Assert.Ignore("SqlPackage is not supported on the current operating system: " + Environment.OSVersion.Platform);
                return null;
            }

            // Set path to Microsoft.SqlTools.ServiceLayer.csproj depending if running tests locally or in pipeline
            string projectPath = string.IsNullOrEmpty(Environment.GetEnvironmentVariable(Constants.BuildSourcesDirectory)) ? 
                @"..\..\..\..\..\src\Microsoft.SqlTools.ServiceLayer\Microsoft.SqlTools.ServiceLayer.csproj" :
                Path.Combine(Environment.GetEnvironmentVariable(Constants.BuildSourcesDirectory), @"src\Microsoft.SqlTools.ServiceLayer\Microsoft.SqlTools.ServiceLayer.csproj");
            FileInfo projectFile = new FileInfo(projectPath);
            Assert.IsTrue(projectFile.Exists, "Project not found at " + projectFile.FullName);

            ProcessStartInfo dotnet = new ProcessStartInfo()
            {
                Arguments = $"publish \"{projectFile.FullName}\" --output \"{workingDirectory}\" --runtime {runtimeIdentifier}",
                CreateNoWindow = true,
                FileName = "dotnet",
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            // Run dotnet publish to workingDirectory
            using (Process dotnetProcess = new Process())
            {
                dotnetProcess.StartInfo = dotnet;

                Console.WriteLine($"Invoking: dotnet {dotnet.Arguments}");
                dotnetProcess.Start();

                dotnetProcess.WaitForExit();
                Assert.AreEqual(0, dotnetProcess.ExitCode, "dotnet publish failed with error: " + dotnetProcess.StandardError.ReadToEnd());
            }

            string file = Path.Combine(workingDirectory, sqlpackageExecutable);
            Assert.IsTrue(File.Exists(file), "sqlpackage not found at " + file);

            // TODO zijchen: Temporary hack to copy Microsoft.Data.SqlClient.SNI.pdb to workingDirectory, remove when DacFx upgrades to MDS 2.1.0+
            File.Copy(@"C:\src\Working\DacFx_Preview_Dev\bin\x86\Debug\netstandard2.0\Microsoft.Data.SqlClient.SNI.pdb",
                Path.Combine(workingDirectory, "Microsoft.Data.SqlClient.SNI.pdb"), true);

            ProcessStartInfo sqlpackage = new ProcessStartInfo()
            {
                Arguments = arguments,
                CreateNoWindow = true,
                FileName = file,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            
            return sqlpackage;
        }
    }
}

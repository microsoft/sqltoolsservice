//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;
using System.Reflection;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.ServiceLayer.Formatter;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Moq;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Formatter
{
    public class FormatterUnitTestsBase
    {
        public FormatterUnitTestsBase()
        {
            HostMock = new Mock<IProtocolEndpoint>();
            WorkspaceServiceMock = new Mock<WorkspaceService<SqlToolsSettings>>();
            ServiceProvider = ExtensionServiceProvider.CreateDefaultServiceProvider();
            ServiceProvider.RegisterSingleService(WorkspaceServiceMock.Object);
            HostLoader.InitializeHostedServices(ServiceProvider, HostMock.Object);
            FormatterService = ServiceProvider.GetService<TSqlFormatterService>();
        }

        protected ExtensionServiceProvider ServiceProvider { get; private set; }
        protected Mock<IProtocolEndpoint> HostMock { get; private set; }
        protected Mock<WorkspaceService<SqlToolsSettings>> WorkspaceServiceMock { get; private set; }

        protected TSqlFormatterService FormatterService { get; private set; }

        protected void LoadAndFormatAndCompare(string testName, FileInfo inputFile, FileInfo baselineFile, FormatOptions options, bool verifyFormat)
        {
            string inputSql = TestUtilities.ReadTextAndNormalizeLineEndings(inputFile.FullName);
            string formattedSql = string.Empty;
            formattedSql = FormatterService.Format(inputSql, options, verifyFormat);

            formattedSql = TestUtilities.NormalizeLineEndings(formattedSql);

            string assemblyPath = GetType().GetTypeInfo().Assembly.Location;
            string directory = Path.Combine(Path.GetDirectoryName(assemblyPath), "FormatterTests");
            Directory.CreateDirectory(directory);

            FileInfo outputFile = new FileInfo(Path.Combine(directory, testName + ".out"));
            File.WriteAllText(outputFile.FullName, formattedSql);
            TestUtilities.CompareTestFiles(baselineFile, outputFile);
        }

        public FileInfo GetInputFile(string fileName)
        {
            return new FileInfo(Path.Combine(InputFileDirectory.FullName, fileName));
        }

        public FileInfo GetBaselineFile(string fileName)
        {
            return new FileInfo(Path.Combine(BaselineDirectory.FullName, fileName));
        }
        
        public DirectoryInfo BaselineDirectory
        {
            get
            {
                string d = Path.Combine(TestLocationDirectory, "BaselineFiles");
                return new DirectoryInfo(d);
            }
        }

        public DirectoryInfo InputFileDirectory
        {
            get
            {
                string d = Path.Combine(TestLocationDirectory, "TestFiles");
                return new DirectoryInfo(d);
            }
        }

        private static string TestLocationDirectory
        {
            get
            {
                return Path.Combine(RunEnvironmentInfo.GetTestDataLocation(), "TSqlFormatter");
            }
        }
    }
}
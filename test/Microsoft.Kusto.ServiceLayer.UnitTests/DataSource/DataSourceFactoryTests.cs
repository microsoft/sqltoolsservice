using System;
using System.Collections.Generic;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;
using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.DataSource.Intellisense;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.DataSource
{
    public class DataSourceFactoryTests
    {
        [TestCase(typeof(ArgumentException), "ConnectionString", "", "AzureMFA")]
        [TestCase(typeof(ArgumentException), "ConnectionString", "", "dstsAuth")]
        public void Create_Throws_Exceptions_For_InvalidAzureAccountToken(Type exceptionType, string connectionString, string azureAccountToken, string authType)
        {
            var dataSourceFactory = new DataSourceFactory();
            var connectionDetails = new ConnectionDetails
            {
                ConnectionString = connectionString,
                AccountToken = azureAccountToken,
                AuthenticationType = authType
            };
            Assert.Throws(exceptionType, () => dataSourceFactory.Create(connectionDetails, ""));
        }

        [Test]
        public void GetDefaultAutoComplete_Throws_ArgumentException_For_InvalidDataSourceType()
        {
            Assert.Throws<ArgumentException>(() =>
                DataSourceFactory.GetDefaultAutoComplete(DataSourceType.None, null, null));
        }

        [Test]
        public void GetDefaultAutoComplete_Returns_CompletionItems()
        {
            var textDocumentPosition = new TextDocumentPosition
            {
                Position = new Position()
            };
            var scriptFile = new ScriptFile("", "", "");
            var scriptParseInfo = new ScriptParseInfo();
            var documentInfo = new ScriptDocumentInfo(textDocumentPosition, scriptFile, scriptParseInfo);
            var position = new Position();

            var completionItems = DataSourceFactory.GetDefaultAutoComplete(DataSourceType.Kusto, documentInfo, position);
            Assert.AreNotEqual(0, completionItems.Length);
        }

        [Test]
        public void GetDefaultSemanticMarkers_Throws_ArgumentException_For_InvalidDataSourceType()
        {
            Assert.Throws<ArgumentException>(() =>
                DataSourceFactory.GetDefaultSemanticMarkers(DataSourceType.None, null, null, null));
        }

        [Test]
        public void GetDefaultSemanticMarkers_Returns_ScriptFileMarker()
        {
            var parseInfo = new ScriptParseInfo();
            var file = new ScriptFile("", "", "");
            var queryText = ".show databases";
            
            var semanticMarkers = DataSourceFactory.GetDefaultSemanticMarkers(DataSourceType.Kusto, parseInfo, file, queryText);
            
            Assert.AreEqual(0, semanticMarkers.Length);
        }

        [Test]
        public void ConvertToServerInfoFormat_Throws_ArgumentException_For_InvalidDataSourceType()
        {
            Assert.Throws<ArgumentException>(() =>
                DataSourceFactory.ConvertToServerInfoFormat(DataSourceType.None, null));
        }

        [Test]
        public void ConvertToServerInfoFormat_Returns_ServerInfo_With_Options()
        {
            var diagnosticsInfo = new DiagnosticsInfo
            {
                Options = new Dictionary<string, object>
                {
                    {"Key", "Object"}
                }
            };
            
            var serverInfo = DataSourceFactory.ConvertToServerInfoFormat(DataSourceType.Kusto, diagnosticsInfo);

            Assert.IsNotNull(serverInfo.Options);
            Assert.AreEqual(diagnosticsInfo.Options["Key"], serverInfo.Options["Key"]);
        }
    }
}
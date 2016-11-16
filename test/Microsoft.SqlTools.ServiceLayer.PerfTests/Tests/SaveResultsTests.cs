using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Scripts;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Tests;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.PerfTests.Tests
{
    public class SaveResultsTests
    {
        [Fact]
        public async Task TestSaveResultsToCsvTest()
        {
            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (SelfCleaningFile outputFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                const string query = Scripts.SimpleQuery;

                // Execute a query
                await Common.ConnectAsync(testBase, TestServerType.OnPrem, query, queryFile.FilePath);
                await testBase.RunQuery(queryFile.FilePath, query);
                await Common.CalculateRunTime(() => testBase.SaveAsCsv(queryFile.FilePath, outputFile.FilePath, 0, 0));
                await testBase.Disconnect(queryFile.FilePath);
            }
        }

        [Fact]
        public async Task TestSaveResultsToJsonTest()
        {
            using (SelfCleaningFile queryFile = new SelfCleaningFile())
            using (SelfCleaningFile outputFile = new SelfCleaningFile())
            using (TestBase testBase = new TestBase())
            {
                const string query = Scripts.SimpleQuery;
                const TestServerType serverType = TestServerType.OnPrem;

                // Execute a query
                await Common.ConnectAsync(testBase, serverType, query, queryFile.FilePath);
                await testBase.RunQuery(queryFile.FilePath, query);
                await Common.CalculateRunTime(() => testBase.SaveAsJson(queryFile.FilePath, outputFile.FilePath, 0, 0));
                await testBase.Disconnect(queryFile.FilePath);
            }
        }

    }
}

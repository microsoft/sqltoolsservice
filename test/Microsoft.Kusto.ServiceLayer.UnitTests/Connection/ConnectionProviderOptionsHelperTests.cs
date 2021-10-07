using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.DataSource;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.Connection
{
    public class ConnectionProviderOptionsHelperTests
    {
        [TestCase(DataSourceType.Kusto, 31)]
        [TestCase(DataSourceType.LogAnalytics, 30)]
        public void BuildConnectionProviderOptions_Returns_31_Options(DataSourceType serviceType, int expected)
        {
            Program.ServiceName = serviceType;
            var providerOptions = ConnectionProviderOptionsHelper.BuildConnectionProviderOptions();
            Assert.AreEqual(expected, providerOptions.Options.Length);
        }
    }
}
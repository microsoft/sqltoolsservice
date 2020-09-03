using Microsoft.Kusto.ServiceLayer.Connection;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.Connection
{
    public class ConnectionProviderOptionsHelperTests
    {
        [Test]
        public void BuildConnectionProviderOptions_Returns_31_Options()
        {
            var providerOptions = ConnectionProviderOptionsHelper.BuildConnectionProviderOptions();
            Assert.AreEqual(31, providerOptions.Options.Length);
        }
    }
}
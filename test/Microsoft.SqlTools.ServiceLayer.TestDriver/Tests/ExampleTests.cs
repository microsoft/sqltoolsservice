using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.TestDriver.Driver;

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Tests
{
    public class ExampleTests
    {
        /// <summary>
        /// Example test that performs a connect, then disconnect.
        /// All tests must have the same signature of returning an async Task
        /// and taking in a ServiceTestDriver as a parameter.
        /// </summary>
        public async Task ConnectDisconnectTest(ServiceTestDriver driver)
        {
            var connectParams = new ConnectParams();
            connectParams.OwnerUri = "file";
            connectParams.Connection = new ConnectionDetails();
            connectParams.Connection.ServerName = "localhost";
            connectParams.Connection.AuthenticationType = "Integrated";
            
            var result = await driver.SendRequest(ConnectionRequest.Type, connectParams);
            if (result)
            {
                await driver.WaitForEvent(ConnectionCompleteNotification.Type);

                var disconnectParams = new DisconnectParams();
                disconnectParams.OwnerUri = "file";
                var result2 = await driver.SendRequest(DisconnectRequest.Type, disconnectParams);
                if (result2)
                {
                    Console.WriteLine("success");
                }
            }
        }
    }
}

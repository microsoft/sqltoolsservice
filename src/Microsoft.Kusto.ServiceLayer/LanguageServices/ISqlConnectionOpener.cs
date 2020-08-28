using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.Kusto.ServiceLayer.LanguageServices
{
    public interface ISqlConnectionOpener
    {
        /// <summary>
        /// Virtual method used to support mocking and testing
        /// </summary>
        ServerConnection OpenServerConnection(ConnectionInfo connInfo, string featureName);
    }
}
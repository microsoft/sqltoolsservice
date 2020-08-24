using System.Composition;
using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.Kusto.ServiceLayer.LanguageServices
{
    [Export(typeof(ISqlConnectionOpener))]
    public class SqlConnectionOpener : ISqlConnectionOpener
    {
        public ServerConnection OpenServerConnection(ConnectionInfo connInfo, string featureName)
        {
            return ConnectionService.OpenServerConnection(connInfo, featureName);
        }
    }
}
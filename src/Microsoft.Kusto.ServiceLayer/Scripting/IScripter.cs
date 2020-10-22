using Microsoft.Kusto.ServiceLayer.Connection;
using Microsoft.Kusto.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.Kusto.ServiceLayer.Scripting
{
    public interface IScripter
    {
        string SelectFromTableOrView(Urn urn);
        string AlterFunction(ReliableDataSourceConnection connection, ScriptingObject scriptingObject);
        string ExecuteFunction(ReliableDataSourceConnection connection, ScriptingObject scriptingObject);
    }
}
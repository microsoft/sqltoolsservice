using Microsoft.Kusto.ServiceLayer.DataSource;
using Microsoft.Kusto.ServiceLayer.Scripting.Contracts;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.Kusto.ServiceLayer.Scripting
{
    public interface IScripter
    {
        string SelectFromTableOrView(ScriptingObject scriptingObject);
        string AlterFunction(IDataSource dataSource, ScriptingObject scriptingObject);
        string ExecuteFunction(IDataSource dataSource, ScriptingObject scriptingObject);
    }
}
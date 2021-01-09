using Kusto.Language;

namespace Microsoft.Kusto.ServiceLayer.DataSource.DataSourceIntellisense
{
    public interface IIntellisenseClient
    {
        /// <summary>
        /// SchemaState used for getting intellisense info.
        /// </summary>
        GlobalState SchemaState { get; }

        void UpdateDatabase(string databaseName);
    }
}
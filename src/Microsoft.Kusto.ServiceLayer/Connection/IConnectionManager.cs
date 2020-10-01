namespace Microsoft.Kusto.ServiceLayer.Connection
{
    public interface IConnectionManager
    {
        bool TryFindConnection(string ownerUri, out ConnectionInfo connectionInfo);
        bool Exists(string ownerUri);
        void Set(string ownerUri, ConnectionInfo connectionInfo);
        void Remove(string ownerUri);
    }
}
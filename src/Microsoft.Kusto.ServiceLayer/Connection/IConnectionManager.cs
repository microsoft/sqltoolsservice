namespace Microsoft.Kusto.ServiceLayer.Connection
{
    public interface IConnectionManager
    {
        bool TryGetValue(string ownerUri, out ConnectionInfo info);
        bool ContainsKey(string ownerUri);
        bool TryAdd(string ownerUri, ConnectionInfo connectionInfo);
        bool TryRemove(string ownerUri);
    }
}
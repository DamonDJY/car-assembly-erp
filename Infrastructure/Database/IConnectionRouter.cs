namespace CarAssemblyErp.Infrastructure.Database;

public interface IConnectionRouter
{
    string GetPrimaryConnectionString();
    string GetReplicaConnectionString();
    Task<bool> IsRecentlyWrittenAsync(string entityKey);
    Task MarkAsWrittenAsync(string entityKey, TimeSpan ttl);
}

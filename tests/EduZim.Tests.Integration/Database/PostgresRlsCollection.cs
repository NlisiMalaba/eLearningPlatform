namespace EduZim.Tests.Integration.Database;

[CollectionDefinition(Name)]
public sealed class PostgresRlsCollection : ICollectionFixture<PostgresRlsFixture>
{
    public const string Name = "postgres-rls";
}

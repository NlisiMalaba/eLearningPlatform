namespace EduZim.Tests.Integration.Support;

[CollectionDefinition(Name)]
public sealed class UnauthorizedApiCollection : ICollectionFixture<UnauthorizedApiFactory>
{
    public const string Name = "unauthorized-api";
}

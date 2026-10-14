namespace TaskApi.Tests.Integration.Infrastructure;

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "Api";
}

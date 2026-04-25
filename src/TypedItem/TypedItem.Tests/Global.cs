using System.Diagnostics.CodeAnalysis;
using Xunit;

[assembly:ExcludeFromCodeCoverage]

namespace TypedItem.Tests;

[CollectionDefinition(CosmosDbCollection.Name)]
public class CosmosDbCollection : ICollectionFixture<CosmosDbDatabaseFixture>
{
    public const string Name = "CosmosDB";
}

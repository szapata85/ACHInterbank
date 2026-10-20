using Cfa.ACHInterbank.Api.Contracts.AchResponses;
using System.Text.Json;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;

namespace Cfa.ACHInterbank.Tests;

public sealed class OpenApiGuidSchemaRegressionTests
{
    [Theory]
    [InlineData(typeof(AchResponseDetailResponse))]
    [InlineData(typeof(AchResponseStatusMappingResponse))]
    public void PublicResponseWithGuidVersion_MustExportJsonSchema(Type responseType)
    {
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        var schema = serializerOptions.GetJsonSchemaAsNode(responseType);

        Assert.NotNull(schema);
        Assert.NotNull(schema["properties"]?["version"]);
    }
}

using System.Net;
using DfT.DTRO;
using DfT.DTRO.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dft.DTRO.Tests.IntegrationTests;

public class SchemasControllerTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SchemasControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Schemas_ReturnsSchemas()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/v1/schemas");

        response.EnsureSuccessStatusCode();
        dynamic? data = JsonConvert.DeserializeObject<dynamic>(
            await response.Content.ReadAsStringAsync()
        );
        Assert.NotNull(data?.schemas);
        var schemas = (data?.schemas as JArray)?.ToObject<SchemaDefinition[]>();
        Assert.NotNull(schemas);
        Assert.NotEmpty(schemas!);
    }

    [Theory]
    [InlineData("3.1.1")]
    public async Task Get_SchemaById_SchemaExists_ReturnsSchema(string schemaVersion)
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"/v1/schemas/{schemaVersion}");

        response.EnsureSuccessStatusCode();
        dynamic? data = JsonConvert.DeserializeObject<dynamic>(
            await response.Content.ReadAsStringAsync()
        );
        Assert.Equal(schemaVersion, data?.schemaVersion.ToString());
        Assert.NotNull(data?.schema);
    }

    [Theory]
    [InlineData("0.0.0")]
    public async Task Get_SchemaById_SchemaDoesNotExist_ReturnsNotFound(string schemaVersion)
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"/v1/schemas/{schemaVersion}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
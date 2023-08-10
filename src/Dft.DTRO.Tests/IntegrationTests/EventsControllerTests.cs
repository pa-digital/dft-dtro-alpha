using System.Text;
using DfT.DTRO;
using DfT.DTRO.Models;
using DfT.DTRO.Services.Storage;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Dft.DTRO.Tests.IntegrationTests;

public class EventsControllerTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string SampleDtroJsonPath = "./DtroJsonDataExamples/proper-data.json";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IStorageService> _mockStorageService;

    public EventsControllerTests(WebApplicationFactory<Program> factory)
    {
        _mockStorageService = new Mock<IStorageService>(MockBehavior.Strict);
        _factory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton(_mockStorageService.Object);
        }));
    }

    [Fact]
    public async Task Post_Events_NoDtroIsMatchingTheCriteria_ReturnsEmptyResult()
    {
        _mockStorageService.Setup(mock => mock.FindDtros(It.IsAny<DtroEventSearch>()))
            .Returns(Task.FromResult(Array.Empty<DfT.DTRO.Models.DTRO>().ToList()));
        HttpClient client = _factory.CreateClient();

        DtroEventSearch searchCriteria =
            new() { Since = DateTime.Today, Page = 1, PageSize = 10 };
        string payload = JsonConvert.SerializeObject(searchCriteria);

        HttpResponseMessage response =
            await client.PostAsync("/v1/events", new StringContent(payload, Encoding.UTF8, "application/json"));

        response.EnsureSuccessStatusCode();
        DtroEventSearchResult? data = JsonConvert.DeserializeObject<DtroEventSearchResult>(
            await response.Content.ReadAsStringAsync()
        );
        Assert.NotNull(data);
        Assert.Equal(1, data!.Page);
        Assert.Equal(0, data.PageSize);
        Assert.Equal(0, data.TotalCount);
        Assert.Empty(data.Events);
        Assert.Empty(data.Events);
    }

    [Fact]
    public async Task Post_Search_DtroMatchingTheCriteriaExists_ReturnsMatchingDtros()
    {
        DfT.DTRO.Models.DTRO sampleDtro = await CreateDtroObject(SampleDtroJsonPath);

        _mockStorageService.Setup(mock => mock.FindDtros(It.IsAny<DtroEventSearch>()))
            .Returns(Task.FromResult(new[] { sampleDtro }.ToList()));
        HttpClient client = _factory.CreateClient();

        DtroEventSearch searchCriteria =
            new() { Since = DateTime.Today, Page = 1, PageSize = 10 };
        string payload = JsonConvert.SerializeObject(searchCriteria);
        HttpResponseMessage response =
            await client.PostAsync("/v1/events", new StringContent(payload, Encoding.UTF8, "application/json"));

        response.EnsureSuccessStatusCode();
        DtroEventSearchResult? data = JsonConvert.DeserializeObject<DtroEventSearchResult>(
            await response.Content.ReadAsStringAsync()
        );
        Assert.NotNull(data);
        Assert.Equal(1, data!.Page);
        Assert.Equal(1, data.PageSize);
        Assert.Equal(1, data.TotalCount);
        Assert.Single(data.Events);
        Assert.Equal(1585, data.Events.First().TrafficAuthorityId);
    }
}
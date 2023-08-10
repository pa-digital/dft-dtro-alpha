using System.Net;
using DfT.DTRO;
using DfT.DTRO.Models;
using DfT.DTRO.Services.Storage;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Dft.DTRO.Tests.IntegrationTests;

public class DTROsControllerTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ValidDtroJsonPath = "./DtroJsonDataExamples/proper-data.json";
    private const string ValidComplexDtroJsonPath = "./DtroJsonDataExamples/3.1.2-valid-complex-dtro.json";
    private const string InvalidDtroJsonPath = "./DtroJsonDataExamples/provision-empty.json";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IStorageService> _mockStorageService;

    public DTROsControllerTests(WebApplicationFactory<Program> factory)
    {
        _mockStorageService = new Mock<IStorageService>(MockBehavior.Strict);
        _factory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton(_mockStorageService.Object);
        }));
    }

    [Fact]
    public async Task Post_DtroIsValid_CreatesDtroAndReturnsDtroId()
    {
        _mockStorageService.Setup(mock => mock.SaveDtroAsJson(It.IsAny<Guid>(), It.IsAny<DfT.DTRO.Models.DTRO>()))
            .Returns(Task.FromResult(true));
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response =
            await client.PostAsync("/v1/dtros",
                await CreateDtroJsonPayload(ValidComplexDtroJsonPath, "3.1.2"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        DTROResponse? data = JsonConvert.DeserializeObject<DTROResponse>(await response.Content.ReadAsStringAsync());
        Assert.NotNull(data);
        Assert.IsType<Guid>(data!.Id);
        _mockStorageService.Verify(mock => mock.SaveDtroAsJson(It.IsAny<Guid>(), It.IsAny<DfT.DTRO.Models.DTRO>()));
    }
    
    [Fact]
    public async Task Post_SchemaDoesNotExist_ReturnsNotFoundError()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response =
            await client.PostAsync("/v1/dtros", await CreateDtroJsonPayload(ValidDtroJsonPath, "0.0.0"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_DtroIsInvalid_ReturnsValidationError()
    {
        _mockStorageService.Setup(mock => mock.SaveDtroAsJson(It.IsAny<Guid>(), It.IsAny<DfT.DTRO.Models.DTRO>()))
            .Returns(Task.FromResult(true));
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response =
            await client.PostAsync("/v1/dtros", await CreateDtroJsonPayload(InvalidDtroJsonPath, "3.1.1", false));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ApiErrorResponse? data = JsonConvert.DeserializeObject<ApiErrorResponse>(
            await response.Content.ReadAsStringAsync()
        );
        Assert.NotNull(data);
        Assert.Equal("Bad request", data!.Message);
        Assert.Single(data.Errors);
        Assert.Contains("Array item count 0 is less than minimum count of 1", data.Errors[0].ToString() ?? string.Empty);
    }

    [Fact]
    public async Task Put_DtroIsValid_UpdatesDtroAndReturnsDtroId()
    {
        Guid dtroId = Guid.NewGuid();
        _mockStorageService
            .Setup(mock =>
                mock.TryUpdateDtroAsJson(It.Is(dtroId, EqualityComparer<Guid>.Default),
                    It.IsAny<DfT.DTRO.Models.DTRO>()))
            .Returns(Task.FromResult(true));
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response =
            await client.PutAsync(
                $"/v1/dtros/{dtroId}",
                await CreateDtroJsonPayload(ValidDtroJsonPath, "3.1.1"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        DTROResponse? data = JsonConvert.DeserializeObject<DTROResponse>(await response.Content.ReadAsStringAsync());
        Assert.NotNull(data);
        Assert.IsType<Guid>(data!.Id);
        _mockStorageService.Verify(mock => mock.TryUpdateDtroAsJson(dtroId, It.IsAny<DfT.DTRO.Models.DTRO>()));
    }

    [Fact]
    public async Task Put_DtroDoesNotExist_ReturnsNotFoundError()
    {
        Guid notExistingDtroId = Guid.NewGuid();
        _mockStorageService
            .Setup(mock =>
                mock.TryUpdateDtroAsJson(It.IsAny<Guid>(),
                    It.IsAny<DfT.DTRO.Models.DTRO>()))
            .Returns(Task.FromResult(false));
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response =
            await client.PutAsync(
                $"/v1/dtros/{notExistingDtroId}",
                await CreateDtroJsonPayload(ValidDtroJsonPath, "3.1.1"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_DtroIsInvalid_ReturnsValidationError()
    {
        Guid dtroId = Guid.NewGuid();
        _mockStorageService
            .Setup(mock =>
                mock.TryUpdateDtroAsJson(It.Is(dtroId, EqualityComparer<Guid>.Default),
                    It.IsAny<DfT.DTRO.Models.DTRO>()))
            .Returns(Task.FromResult(true));
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response =
            await client.PutAsync(
                $"/v1/dtros/{dtroId}",
                await CreateDtroJsonPayload(InvalidDtroJsonPath, "3.1.1", false));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ApiErrorResponse? data = JsonConvert.DeserializeObject<ApiErrorResponse>(
            await response.Content.ReadAsStringAsync()
        );
        Assert.NotNull(data);
        Assert.Equal("Bad request", data!.Message);
        Assert.Single(data.Errors);
        Assert.Contains("Array item count 0 is less than minimum count of 1", data.Errors[0].ToString() ?? string.Empty);
    }

    [Fact]
    public async Task Get_DtroExists_ReturnsDtro()
    {
        DfT.DTRO.Models.DTRO sampleDtro = await CreateDtroObject(ValidDtroJsonPath);
        _mockStorageService
            .Setup(mock => mock.GetDtroById(It.Is(sampleDtro.Id, EqualityComparer<Guid>.Default)))
            .Returns(Task.FromResult(sampleDtro));
        
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"/v1/dtros/{sampleDtro.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_DtroDoesNotExist_ReturnsNotFoundError()
    {
        Guid dtroId = Guid.NewGuid();
        _mockStorageService
            .Setup(mock => mock.GetDtroById(It.Is(dtroId, EqualityComparer<Guid>.Default)))
            .Returns(Task.FromResult<DfT.DTRO.Models.DTRO?>(null));
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"/v1/dtros/{dtroId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_DtroExists_ReturnsDtro()
    {
        Guid dtroId = Guid.NewGuid();
        _mockStorageService
            .Setup(mock => mock.DeleteDtro(It.Is(dtroId, EqualityComparer<Guid>.Default), It.IsAny<DateTime?>()))
            .Returns(Task.FromResult(true));
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.DeleteAsync($"/v1/dtros/{dtroId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_DtroDoesNotExist_ReturnsNotFoundError()
    {
        Guid dtroId = Guid.NewGuid();
        _mockStorageService
            .Setup(mock => mock.DeleteDtro(It.Is(dtroId, EqualityComparer<Guid>.Default), It.IsAny<DateTime?>()))
            .Returns(Task.FromResult(false));
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.DeleteAsync($"/v1/dtros/{dtroId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DfT.DTRO.Models;
using DfT.DTRO.Models.Filtering;
using DfT.DTRO.Services.Storage;
using Microsoft.Extensions.Logging;
using Moq;

namespace Dft.DTRO.Tests;
public class MultiStorageServiceTests
{
    IMock<ILogger<MultiStorageService>> _mockLogger = new Mock<ILogger<MultiStorageService>>();
    IEnumerable<IStorageService> _mockStorageServices;

    Mock<IStorageService> _mockFileStorageService;

    public MultiStorageServiceTests()
    {
        _mockFileStorageService = new Mock<IStorageService>();
    }

    [Fact]
    public async Task FindDtros_CallsTheServiceThatCanSearch()
    {
        var serviceThatCanSearch = new Mock<IStorageService>();
        serviceThatCanSearch.SetupGet(it => it.CanSearch).Returns(true);

        var serviceThatCantSearch = new Mock<IStorageService>();
        serviceThatCantSearch.SetupGet(it => it.CanSearch).Returns(false);

        _mockStorageServices = new List<IStorageService> { serviceThatCantSearch.Object, serviceThatCanSearch.Object };

        var sut = new MultiStorageService(_mockStorageServices, _mockLogger.Object, false);

        await sut.FindDtros(new DtroSearch());

        serviceThatCanSearch.Verify(it => it.FindDtros(It.IsAny<DtroSearch>()), Times.Once);
        serviceThatCantSearch.Verify(it => it.FindDtros(It.IsAny<DtroSearch>()), Times.Never);
    }

    [Fact]
    public async Task FindDtrosForEvents_CallsTheServiceThatCanSearch()
    {
        var serviceThatCanSearch = new Mock<IStorageService>();
        serviceThatCanSearch.SetupGet(it => it.CanSearch).Returns(true);

        var serviceThatCantSearch = new Mock<IStorageService>();
        serviceThatCantSearch.SetupGet(it => it.CanSearch).Returns(false);

        _mockStorageServices = new List<IStorageService> { serviceThatCantSearch.Object, serviceThatCanSearch.Object };

        var sut = new MultiStorageService(_mockStorageServices, _mockLogger.Object, false);

        await sut.FindDtros(new DtroEventSearch());

        serviceThatCanSearch.Verify(it => it.FindDtros(It.IsAny<DtroEventSearch>()), Times.Once);
        serviceThatCantSearch.Verify(it => it.FindDtros(It.IsAny<DtroEventSearch>()), Times.Never);
    }

    [Fact]
    public void CanSearch_ReturnsTrue_WhenAnyNestedReturnsTrue()
    {
        var serviceThatCanSearch = new Mock<IStorageService>();
        serviceThatCanSearch.SetupGet(it => it.CanSearch).Returns(true);

        var serviceThatCantSearch = new Mock<IStorageService>();
        serviceThatCantSearch.SetupGet(it => it.CanSearch).Returns(false);

        _mockStorageServices = new List<IStorageService> { serviceThatCantSearch.Object, serviceThatCanSearch.Object };

        var sut = new MultiStorageService(_mockStorageServices, _mockLogger.Object, false);

        Assert.True(sut.CanSearch);
    }

    [Fact]
    public void CanSearch_ReturnsFalse_WhenAllNestedReturnFalse()
    {
        var serviceThatCantSearch = new Mock<IStorageService>();
        serviceThatCantSearch.SetupGet(it => it.CanSearch).Returns(false);

        var serviceThatCantSearchEither = new Mock<IStorageService>();
        serviceThatCantSearchEither.SetupGet(it => it.CanSearch).Returns(false);

        _mockStorageServices = new List<IStorageService> { serviceThatCantSearch.Object, serviceThatCantSearchEither.Object };

        var sut = new MultiStorageService(_mockStorageServices, _mockLogger.Object, false);

        Assert.False(sut.CanSearch);
    }

    [Fact]
    public async Task GetDtroById_ReturnsFromTheFirstService_ByDefault()
    {
        var dtro = new DfT.DTRO.Models.DTRO();
        var key = Guid.NewGuid();

        var service1 = new Mock<IStorageService>();
        service1.Setup(it => it.GetDtroById(key)).ReturnsAsync(dtro);

        var service2 = new Mock<IStorageService>();
        service2.Setup(it => it.GetDtroById(key)).ReturnsAsync(dtro);

        _mockStorageServices = new List<IStorageService> { service1.Object, service2.Object };

        var sut = new MultiStorageService(_mockStorageServices, _mockLogger.Object, false);

        var result = await sut.GetDtroById(key);

        Assert.Same(dtro, result);

        service1.Verify(it => it.GetDtroById(key));
        service2.Verify(it => it.GetDtroById(key), Times.Never);
    }

    [Fact]
    public async Task GetDtroById_ReturnsFromTheSecondService_IfFirstFails()
    {
        var dtro = new DfT.DTRO.Models.DTRO();
        var key = Guid.NewGuid();

        var service1 = new Mock<IStorageService>();
        service1.Setup(it => it.GetDtroById(key)).ThrowsAsync(new Exception());

        var service2 = new Mock<IStorageService>();
        service2.Setup(it => it.GetDtroById(key)).ReturnsAsync(dtro);

        _mockStorageServices = new List<IStorageService> { service1.Object, service2.Object };

        var sut = new MultiStorageService(_mockStorageServices, _mockLogger.Object, false);

        var result = await sut.GetDtroById(key);

        Assert.Same(dtro, result);

        service1.Verify(it => it.GetDtroById(key), Times.Once);
        service2.Verify(it => it.GetDtroById(key), Times.Once);
    }

    [Fact]
    public async Task GetDtroById_ThrowsAggregateException_WhenAllServicesFail()
    {
        var dtro = new DfT.DTRO.Models.DTRO();
        var key = Guid.NewGuid();

        var service1 = new Mock<IStorageService>();
        service1.Setup(it => it.GetDtroById(key)).ThrowsAsync(new Exception());

        var service2 = new Mock<IStorageService>();
        service2.Setup(it => it.GetDtroById(key)).ThrowsAsync(new Exception());

        _mockStorageServices = new List<IStorageService> { service1.Object, service2.Object };

        var sut = new MultiStorageService(_mockStorageServices, _mockLogger.Object, false);

        await Assert.ThrowsAsync<AggregateException>(() => sut.GetDtroById(key));

        service1.Verify(it => it.GetDtroById(key), Times.Once);
        service2.Verify(it => it.GetDtroById(key), Times.Once);
    }
    [Fact]
    public async Task DtroExists_ReturnsFromTheFirstService_ByDefault()
    {
        var dtro = new DfT.DTRO.Models.DTRO();
        var key = Guid.NewGuid();

        var service1 = new Mock<IStorageService>();
        service1.Setup(it => it.DtroExists(key)).ReturnsAsync(true);

        var service2 = new Mock<IStorageService>();
        service2.Setup(it => it.DtroExists(key)).ReturnsAsync(true);

        _mockStorageServices = new List<IStorageService> { service1.Object, service2.Object };

        var sut = new MultiStorageService(_mockStorageServices, _mockLogger.Object, false);

        var result = await sut.DtroExists(key);

        Assert.True(result);

        service1.Verify(it => it.DtroExists(key));
        service2.Verify(it => it.DtroExists(key), Times.Never);
    }

    [Fact]
    public async Task DtroExists_ReturnsFromTheSecondService_IfFirstFails()
    {
        var dtro = new DfT.DTRO.Models.DTRO();
        var key = Guid.NewGuid();

        var service1 = new Mock<IStorageService>();
        service1.Setup(it => it.DtroExists(key)).ThrowsAsync(new Exception());

        var service2 = new Mock<IStorageService>();
        service2.Setup(it => it.DtroExists(key)).ReturnsAsync(true);

        _mockStorageServices = new List<IStorageService> { service1.Object, service2.Object };

        var sut = new MultiStorageService(_mockStorageServices, _mockLogger.Object, false);

        var result = await sut.DtroExists(key);

        Assert.True(result);

        service1.Verify(it => it.DtroExists(key), Times.Once);
        service2.Verify(it => it.DtroExists(key), Times.Once);
    }

    [Fact]
    public async Task DtroExists_ThrowsAggregateException_WhenAllServicesFail()
    {
        var dtro = new DfT.DTRO.Models.DTRO();
        var key = Guid.NewGuid();

        var service1 = new Mock<IStorageService>();
        service1.Setup(it => it.DtroExists(key)).ThrowsAsync(new Exception());

        var service2 = new Mock<IStorageService>();
        service2.Setup(it => it.DtroExists(key)).ThrowsAsync(new Exception());

        _mockStorageServices = new List<IStorageService> { service1.Object, service2.Object };

        var sut = new MultiStorageService(_mockStorageServices, _mockLogger.Object, false);

        await Assert.ThrowsAsync<AggregateException>(() => sut.DtroExists(key));

        service1.Verify(it => it.DtroExists(key), Times.Once);
        service2.Verify(it => it.DtroExists(key), Times.Once);
    }

    [Fact]
    public async Task SaveDtro_WritesToAllServices()
    {
        var dtro = new DfT.DTRO.Models.DTRO();
        var key = Guid.NewGuid();

        var service1 = new Mock<IStorageService>();
        var service2 = new Mock<IStorageService>();

        _mockStorageServices = new List<IStorageService> { service1.Object, service2.Object, _mockFileStorageService.Object };

        var sut = new MultiStorageService(_mockStorageServices, _mockLogger.Object, false);

        await sut.SaveDtroAsJson(key, dtro);

        service1.Verify(it => it.SaveDtroAsJson(key, dtro), Times.Once);
        service2.Verify(it => it.SaveDtroAsJson(key, dtro), Times.Once);
        _mockFileStorageService.Verify(it => it.SaveDtroAsJson(key, dtro), Times.Once);
    }

    [Fact]
    public async Task SaveDtro_Throws_IfAnyServiceThrows()
    {
        var dtro = new DfT.DTRO.Models.DTRO();
        var key = Guid.NewGuid();

        var service1 = new Mock<IStorageService>();
        var service2 = new Mock<IStorageService>();

        service2.Setup(it => it.SaveDtroAsJson(It.IsAny<Guid>(), It.IsAny<DfT.DTRO.Models.DTRO>())).ThrowsAsync(new Exception());

        _mockStorageServices = new List<IStorageService> { service1.Object, service2.Object, _mockFileStorageService.Object };

        var sut = new MultiStorageService(_mockStorageServices, _mockLogger.Object, false);

        await Assert.ThrowsAnyAsync<Exception>(() => sut.SaveDtroAsJson(key, dtro));

        service1.Verify(it => it.SaveDtroAsJson(key, dtro), Times.Once);
        service2.Verify(it => it.SaveDtroAsJson(key, dtro), Times.Once);
        _mockFileStorageService.Verify(it => it.SaveDtroAsJson(key, dtro), Times.Never);
    }

    [Fact(Skip = "Mocking FileStorageService not working yet")]
    public async Task SaveDtro_WritesToBucketOnly_IfParameterTrue()
    {
        var dtro = new DfT.DTRO.Models.DTRO();
        var key = Guid.NewGuid();

        var service1 = new Mock<IStorageService>();
        var service2 = new Mock<IStorageService>();

        _mockStorageServices = new List<IStorageService> { service1.Object, service2.Object, _mockFileStorageService.Object };

        var sut = new MultiStorageService(_mockStorageServices, _mockLogger.Object, true);

        await sut.SaveDtroAsJson(key, dtro);

        service1.Verify(it => it.SaveDtroAsJson(key, dtro), Times.Never);
        service2.Verify(it => it.SaveDtroAsJson(key, dtro), Times.Never);
        _mockFileStorageService.Verify(it => it.SaveDtroAsJson(key, dtro), Times.Once);
    }

    [Fact]
    public async Task UpdateDtro_WritesToAllServices()
    {
        var dtro = new DfT.DTRO.Models.DTRO();
        var key = Guid.NewGuid();

        var service1 = new Mock<IStorageService>();
        var service2 = new Mock<IStorageService>();

        _mockStorageServices = new List<IStorageService> { service1.Object, service2.Object, _mockFileStorageService.Object };

        var sut = new MultiStorageService(_mockStorageServices, _mockLogger.Object, false);

        await sut.UpdateDtroAsJson(key, dtro);

        service1.Verify(it => it.UpdateDtroAsJson(key, dtro), Times.Once);
        service2.Verify(it => it.UpdateDtroAsJson(key, dtro), Times.Once);
        _mockFileStorageService.Verify(it => it.UpdateDtroAsJson(key, dtro), Times.Once);
    }

    [Fact]
    public async Task UpdateDtro_Throws_IfAnyServiceThrows()
    {
        var dtro = new DfT.DTRO.Models.DTRO();
        var key = Guid.NewGuid();

        var service1 = new Mock<IStorageService>();
        var service2 = new Mock<IStorageService>();

        service2.Setup(it => it.UpdateDtroAsJson(It.IsAny<Guid>(), It.IsAny<DfT.DTRO.Models.DTRO>())).ThrowsAsync(new Exception());

        _mockStorageServices = new List<IStorageService> { service1.Object, service2.Object, _mockFileStorageService.Object };

        var sut = new MultiStorageService(_mockStorageServices, _mockLogger.Object, false);

        await Assert.ThrowsAnyAsync<Exception>(() => sut.UpdateDtroAsJson(key, dtro));

        service1.Verify(it => it.UpdateDtroAsJson(key, dtro), Times.Once);
        service2.Verify(it => it.UpdateDtroAsJson(key, dtro), Times.Once);
        _mockFileStorageService.Verify(it => it.UpdateDtroAsJson(key, dtro), Times.Never);
    }

    [Fact(Skip = "Mocking FileStorageService not working yet")]
    public async Task UpdateDtro_WritesToBucketOnly_IfParameterTrue()
    {
        var dtro = new DfT.DTRO.Models.DTRO();
        var key = Guid.NewGuid();

        var service1 = new Mock<IStorageService>();
        var service2 = new Mock<IStorageService>();

        _mockStorageServices = new List<IStorageService> { service1.Object, service2.Object, _mockFileStorageService.Object };

        var sut = new MultiStorageService(_mockStorageServices, _mockLogger.Object, true);

        await sut.UpdateDtroAsJson(key, dtro);

        service1.Verify(it => it.UpdateDtroAsJson(key, dtro), Times.Never);
        service2.Verify(it => it.UpdateDtroAsJson(key, dtro), Times.Never);
        _mockFileStorageService.Verify(it => it.UpdateDtroAsJson(key, dtro), Times.Once);
    }

    [Fact]
    public async Task DeleteDtro_WritesToAllServices()
    {
        var dtro = new DfT.DTRO.Models.DTRO();
        var key = Guid.NewGuid();

        var service1 = new Mock<IStorageService>();
        var service2 = new Mock<IStorageService>();

        service1.Setup(it => it.DeleteDtro(It.IsAny<Guid>(), It.IsAny<DateTime?>())).ReturnsAsync(true);
        service2.Setup(it => it.DeleteDtro(It.IsAny<Guid>(), It.IsAny<DateTime?>())).ReturnsAsync(true);
        _mockFileStorageService.Setup(it => it.DeleteDtro(It.IsAny<Guid>(), It.IsAny<DateTime?>())).ReturnsAsync(true);

        _mockStorageServices = new List<IStorageService> { service1.Object, service2.Object, _mockFileStorageService.Object };

        var sut = new MultiStorageService(_mockStorageServices, _mockLogger.Object, false);

        var result = await sut.DeleteDtro(key, null);

        Assert.True(result);

        service1.Verify(it => it.DeleteDtro(key, It.IsAny<DateTime?>()), Times.Once);
        service2.Verify(it => it.DeleteDtro(key, It.IsAny<DateTime?>()), Times.Once);
        _mockFileStorageService.Verify(it => it.DeleteDtro(key, It.IsAny<DateTime?>()), Times.Once);
    }

    [Fact]
    public async Task DeleteDtro_ReturnsFalse_IfAnyServiceReturnsFalse()
    {
        var dtro = new DfT.DTRO.Models.DTRO();
        var key = Guid.NewGuid();

        var service1 = new Mock<IStorageService>();
        var service2 = new Mock<IStorageService>();

        service1.Setup(it => it.DeleteDtro(It.IsAny<Guid>(), It.IsAny<DateTime?>())).ReturnsAsync(true);
        service2.Setup(it => it.DeleteDtro(It.IsAny<Guid>(), It.IsAny<DateTime?>())).ReturnsAsync(false);
        _mockFileStorageService.Setup(it => it.DeleteDtro(It.IsAny<Guid>(), It.IsAny<DateTime?>())).ReturnsAsync(true);

        _mockStorageServices = new List<IStorageService> { service1.Object, service2.Object, _mockFileStorageService.Object };

        var sut = new MultiStorageService(_mockStorageServices, _mockLogger.Object, false);

        var result = await sut.DeleteDtro(key, null);

        Assert.False(result);

        service1.Verify(it => it.DeleteDtro(key, It.IsAny<DateTime?>()), Times.Once);
        service2.Verify(it => it.DeleteDtro(key, It.IsAny<DateTime?>()), Times.Once);
        _mockFileStorageService.Verify(it => it.DeleteDtro(key, It.IsAny<DateTime?>()), Times.Never);
    }

    [Fact(Skip = "Mocking FileStorageService not working yet")]
    public async Task DeleteDtro_WritesToBucketOnly_IfParameterTrue()
    {
        var dtro = new DfT.DTRO.Models.DTRO();
        var key = Guid.NewGuid();

        var service1 = new Mock<IStorageService>();
        var service2 = new Mock<IStorageService>();

        _mockStorageServices = new List<IStorageService> { service1.Object, service2.Object, _mockFileStorageService.Object };

        var sut = new MultiStorageService(_mockStorageServices, _mockLogger.Object, true);

        await sut.DeleteDtro(key, null);

        service1.Verify(it => it.DeleteDtro(key, It.IsAny<DateTime?>()), Times.Never);
        service2.Verify(it => it.DeleteDtro(key, It.IsAny<DateTime?>()), Times.Never);
        _mockFileStorageService.Verify(it => it.DeleteDtro(key, It.IsAny<DateTime?>()), Times.Once);
    }

    [Fact]
    public async Task TryUpdateDtro_WritesToAllServices()
    {
        var dtro = new DfT.DTRO.Models.DTRO();
        var key = Guid.NewGuid();

        var service1 = new Mock<IStorageService>();
        var service2 = new Mock<IStorageService>();

        service1.Setup(it => it.TryUpdateDtroAsJson(It.IsAny<Guid>(), It.IsAny<DfT.DTRO.Models.DTRO>())).ReturnsAsync(true);
        service2.Setup(it => it.TryUpdateDtroAsJson(It.IsAny<Guid>(), It.IsAny<DfT.DTRO.Models.DTRO>())).ReturnsAsync(true);
        _mockFileStorageService.Setup(it => it.TryUpdateDtroAsJson(It.IsAny<Guid>(), It.IsAny<DfT.DTRO.Models.DTRO>())).ReturnsAsync(true);

        _mockStorageServices = new List<IStorageService> { service1.Object, service2.Object, _mockFileStorageService.Object };

        var sut = new MultiStorageService(_mockStorageServices, _mockLogger.Object, false);

        await sut.TryUpdateDtroAsJson(key, dtro);

        service1.Verify(it => it.TryUpdateDtroAsJson(key, dtro), Times.Once);
        service2.Verify(it => it.TryUpdateDtroAsJson(key, dtro), Times.Once);
        _mockFileStorageService.Verify(it => it.TryUpdateDtroAsJson(key, dtro), Times.Once);
    }

    [Fact]
    public async Task TryUpdateDtro_ReturnsFalse_IfAnyServiceReturnsFalse()
    {
        var dtro = new DfT.DTRO.Models.DTRO();
        var key = Guid.NewGuid();

        var service1 = new Mock<IStorageService>();
        var service2 = new Mock<IStorageService>();

        service1.Setup(it => it.TryUpdateDtroAsJson(It.IsAny<Guid>(), It.IsAny<DfT.DTRO.Models.DTRO>())).ReturnsAsync(true);
        service2.Setup(it => it.TryUpdateDtroAsJson(It.IsAny<Guid>(), It.IsAny<DfT.DTRO.Models.DTRO>())).ReturnsAsync(false);
        _mockFileStorageService.Setup(it => it.TryUpdateDtroAsJson(It.IsAny<Guid>(), It.IsAny<DfT.DTRO.Models.DTRO>())).ReturnsAsync(true);

        _mockStorageServices = new List<IStorageService> { service1.Object, service2.Object, _mockFileStorageService.Object };

        var sut = new MultiStorageService(_mockStorageServices, _mockLogger.Object, false);

        var result = await sut.TryUpdateDtroAsJson(key, dtro);

        Assert.False(result);

        service1.Verify(it => it.TryUpdateDtroAsJson(key, dtro), Times.Once);
        service2.Verify(it => it.TryUpdateDtroAsJson(key, dtro), Times.Once);
        _mockFileStorageService.Verify(it => it.TryUpdateDtroAsJson(key, dtro), Times.Never);
    }

    [Fact(Skip = "Mocking FileStorageService not working yet")]
    public async Task TryUpdateDtro_WritesToBucketOnly_IfParameterTrue()
    {
        var dtro = new DfT.DTRO.Models.DTRO();
        var key = Guid.NewGuid();

        var service1 = new Mock<IStorageService>();
        var service2 = new Mock<IStorageService>();

        service1.Setup(it => it.TryUpdateDtroAsJson(It.IsAny<Guid>(), It.IsAny<DfT.DTRO.Models.DTRO>())).ReturnsAsync(true);
        service2.Setup(it => it.TryUpdateDtroAsJson(It.IsAny<Guid>(), It.IsAny<DfT.DTRO.Models.DTRO>())).ReturnsAsync(true);
        _mockFileStorageService.Setup(it => it.TryUpdateDtroAsJson(It.IsAny<Guid>(), It.IsAny<DfT.DTRO.Models.DTRO>())).ReturnsAsync(true);

        _mockStorageServices = new List<IStorageService> { service1.Object, service2.Object, _mockFileStorageService.Object };

        var sut = new MultiStorageService(_mockStorageServices, _mockLogger.Object, true);

        await sut.TryUpdateDtroAsJson(key, dtro);

        service1.Verify(it => it.TryUpdateDtroAsJson(key, dtro), Times.Never);
        service2.Verify(it => it.TryUpdateDtroAsJson(key, dtro), Times.Never);
        _mockFileStorageService.Verify(it => it.TryUpdateDtroAsJson(key, dtro), Times.Once);
    }
}

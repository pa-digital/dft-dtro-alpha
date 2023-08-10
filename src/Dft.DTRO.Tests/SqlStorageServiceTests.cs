using DfT.DTRO.DAL;
using DfT.DTRO.Extensions.DependencyInjection;
using DfT.DTRO.Models;
using DfT.DTRO.Models.Filtering;
using DfT.DTRO.Services.Conversion;
using DfT.DTRO.Services.Data;
using DfT.DTRO.Services.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dft.DTRO.Tests;

public class SqlStorageServiceTests : IDisposable
{
    private readonly DtroContext _context;
    private readonly ISpatialProjectionService _spatialProjectionService;

    private readonly Guid _deletedDtroKey = Guid.NewGuid();
    private readonly Guid _existingDtroKey = Guid.NewGuid();
    private readonly Guid _dtroWithHa1234Key = Guid.NewGuid();
    private readonly Guid _dtroWithCreationDateKey = Guid.NewGuid();
    private readonly Guid _dtroWithModificationTimeKey = Guid.NewGuid();
    private readonly Guid _dtroWithNameKey = Guid.NewGuid();
    private readonly Guid _dtroWithVehicleTypesKey = Guid.NewGuid();
    private readonly Guid _dtroWithRegulationTypesKey = Guid.NewGuid();
    private readonly Guid _dtroWithOrderReportingPointKey = Guid.NewGuid();

    private readonly DfT.DTRO.Models.DTRO _existingDtro;
    private readonly DfT.DTRO.Models.DTRO _deletedDtro;
    private readonly DfT.DTRO.Models.DTRO _dtroWithTa1234;
    private readonly DfT.DTRO.Models.DTRO _dtroWithCreationDate;
    private readonly DfT.DTRO.Models.DTRO _dtroWithModificationTime;
    private readonly DfT.DTRO.Models.DTRO _dtroWithName;
    private readonly DfT.DTRO.Models.DTRO _dtroWithVehicleTypes;
    private readonly DfT.DTRO.Models.DTRO _dtroWithRegulationTypes;
    private readonly DfT.DTRO.Models.DTRO _dtroWithOrderReportingPoint;

    private readonly Mock<IDtroMappingService> _mappingServiceMock = new();

    public SqlStorageServiceTests()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(opt =>
                opt.AddJsonFile("./Configurations/appsettings.SqlStorageServiceTests.json", true))
            .ConfigureServices(
                (host, services) => services.AddPostgresDtroContext(host.Configuration))
            .Build();

        _context = host.Services.GetRequiredService<DtroContext>();

        _context.Database.Migrate();

        _spatialProjectionService = new Proj4SpatialProjectionService();

        _existingDtro = new() {
            Id = _existingDtroKey,
            SchemaVersion = new("3.1.2"),
            Data = new(),
        };

        _deletedDtro = new()
        {
            Id = _deletedDtroKey,
            SchemaVersion = new("3.1.2"),
            Data = new(),
            Deleted = true,
            DeletionTime = DateTime.SpecifyKind(new DateTime(2023, 07, 23), DateTimeKind.Utc)
        };

        _dtroWithTa1234 = new()
        {
            Id = _dtroWithHa1234Key,
            SchemaVersion = new("3.1.2"),
            Data = new(),
            TrafficAuthorityId = 1234
        };

        _dtroWithCreationDate = new()
        {
            Id = _dtroWithCreationDateKey,
            SchemaVersion = new("3.1.2"),
            Data = new(),
            Created = DateTime.SpecifyKind(new DateTime(2023, 07, 22), DateTimeKind.Utc)
        };

        _dtroWithModificationTime = new()
        {
            Id = _dtroWithModificationTimeKey,
            SchemaVersion = new("3.1.2"),
            Data = new(),
            LastUpdated = DateTime.SpecifyKind(new DateTime(2023, 07, 22), DateTimeKind.Utc)
        };

        _dtroWithName = new()
        {
            Id = _dtroWithNameKey,
            SchemaVersion = new("3.1.2"),
            Data = new(),
            TroName = "this is a test name"
        };

        _dtroWithVehicleTypes = new()
        {
            Id = _dtroWithVehicleTypesKey,
            SchemaVersion = new("3.1.2"),
            Data = new(),
            VehicleTypes = new List<string>() { "taxi" }
        };

        _dtroWithRegulationTypes = new()
        {
            Id = _dtroWithRegulationTypesKey,
            SchemaVersion = new("3.1.2"),
            Data = new(),
            RegulationTypes = new List<string>() { "test-regulation-type" }
        };

        _dtroWithOrderReportingPoint = new()
        {
            Id = _dtroWithOrderReportingPointKey,
            SchemaVersion = new("3.1.2"),
            Data = new(),
            OrderReportingPoints = new List<string>() { "test-orp" }
        };

        _context.Dtros.Add(_existingDtro);
        _context.Dtros.Add(_deletedDtro);
        _context.Dtros.Add(_dtroWithTa1234);
        _context.Dtros.Add(_dtroWithCreationDate);
        _context.Dtros.Add(_dtroWithModificationTime);
        _context.Dtros.Add(_dtroWithName);
        _context.Dtros.Add(_dtroWithVehicleTypes);
        _context.Dtros.Add(_dtroWithRegulationTypes);
        _context.Dtros.Add(_dtroWithOrderReportingPoint);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _context.Dtros.RemoveRange(_context.Dtros);
        _context.SaveChanges();
    }

    [Fact]
    public async Task DtroExists_ReturnsFalse_ForNonexistentDtros()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        bool result = await sut.DtroExists(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task DtroExists_ReturnsFalse_ForDeletedDtros()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        bool result = await sut.DtroExists(_deletedDtroKey);

        Assert.False(result);
    }

    [Fact]
    public async Task DtroExists_ReturnsTrue_ForExistingDtros()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        bool result = await sut.DtroExists(_existingDtroKey);

        Assert.True(result);
    }

    [Fact]
    public async Task GetDtro_ReturnsNull_ForNonexistendDtros()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.GetDtroById(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDtro_ReturnsValue_ForExistingDtros()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.GetDtroById(_existingDtroKey);

        Assert.NotNull(result);
        Assert.Same(_existingDtro, result);
    }

    [Fact]
    public async Task DeleteDtro_ReturnsFalse_ForNonexistentDtros()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.DeleteDtro(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteDtro_ReturnsFalse_ForDeletedDtros()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.DeleteDtro(_deletedDtroKey);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteDtro_ReturnsTrue_OnSuccessfulDelete()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.DeleteDtro(_existingDtroKey);

        Assert.True(result);
    }

    [Fact]
    public async Task TryUpdateDtro_ReturnsFalse_ForNonexistentDtros()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var newValueKey = Guid.NewGuid();

        var result = await sut.TryUpdateDtroAsJson(newValueKey, new DfT.DTRO.Models.DTRO()
        {
            Id = newValueKey,
            SchemaVersion = new("3.1.2"),
            Data = new(),
        });

        Assert.False(result);
    }

    [Fact]
    public async Task TryUpdateDtro_ReturnsFalse_ForDeletedDtros()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.TryUpdateDtroAsJson(_deletedDtroKey, new DfT.DTRO.Models.DTRO()
        {
            Id = _deletedDtroKey,
            SchemaVersion = new("3.1.2"),
            Data = new(),
        });

        Assert.False(result);
    }

    [Fact]
    public async Task TryUpdateDtro_ReturnsTrue_ForExistingDtros()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.TryUpdateDtroAsJson(_existingDtroKey, new DfT.DTRO.Models.DTRO()
        {
            Id = _existingDtroKey,
            SchemaVersion = new("3.1.2"),
            Data = new(),
        });

        Assert.True(result);
    }

    [Fact]
    public async Task UpdateDtro_Throws_ForNonexistentDtros()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var newValueKey = Guid.NewGuid();

        var newValue = new DfT.DTRO.Models.DTRO()
        {
            Id = _existingDtroKey,
            SchemaVersion = new("3.1.2"),
            Data = new(),
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateDtroAsJson(newValueKey, newValue));
    }

    [Fact]
    public async Task UpdateDtro_Throws_ForDeletedDtros()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var newValue = new DfT.DTRO.Models.DTRO()
        {
            Id = _existingDtroKey,
            SchemaVersion = new("3.1.2"),
            Data = new(),
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateDtroAsJson(_deletedDtroKey, newValue));
    }

    [Fact]
    public async Task TryUpdateDtro_UpdatesSuccessfully_ForExistingDtros()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var newValue = new DfT.DTRO.Models.DTRO()
        {
            Id = _existingDtroKey,
            SchemaVersion = new("3.1.2"),
            Data = new(),
            TrafficAuthorityId = 1234
        };

        await sut.UpdateDtroAsJson(_existingDtroKey, newValue);

        var found = await _context.Dtros.FindAsync(_existingDtroKey);

        Assert.NotNull(found);
        Assert.Equal(1234, found!.TrafficAuthorityId);
    }

    [Fact]
    public async Task SaveDtro_SuccessfullySavesDtros()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var newValueKey = Guid.NewGuid();
        var dtro = new DfT.DTRO.Models.DTRO()
        {
            Id = newValueKey,
            SchemaVersion = new("3.1.2"),
            Data = new(),
        };

        await sut.SaveDtroAsJson(newValueKey, dtro);

        var saved = _context.Dtros.Find(newValueKey);

        Assert.Same(saved, dtro);
    }

    [Fact]
    public async Task FindDtros_ReturnsOnlyNotDeleted_ByDefault()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.FindDtros(new DtroSearch { Page = 1, PageSize = 10, Queries = new List<SearchQuery> { new() } });

        Assert.DoesNotContain(_deletedDtro, result.Results);
    }

    [Fact]
    public async Task FindDtros_ReturnsDeleted_WhenDeletionTimeInQuery()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.FindDtros(new DtroSearch { Page = 1, PageSize = 10, Queries = new List<SearchQuery> { new() { DeletionTime = new(2023, 07, 22) } } });

        Assert.Contains(_deletedDtro, result.Results);
    }

    [Fact]
    public async Task FindDtros_ReturnsDeletedAfterDeletionTime_WhenDeletionTimeInQuery()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.FindDtros(new DtroSearch { Page = 1, PageSize = 10, Queries = new List<SearchQuery> { new() { DeletionTime = new(2023, 07, 24) } } });

        Assert.DoesNotContain(_deletedDtro, result.Results);
    }

    [Fact]
    public async Task FindDtros_ReturnsOnlyDtrosWithSpecifiedTrafficAuthorityId()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.FindDtros(new DtroSearch { Page = 1, PageSize = 10, Queries = new List<SearchQuery> { new() { Ta = 1234 } } });

        Assert.Equal(1, result.Results.Count);
        Assert.Contains(_dtroWithTa1234, result.Results);
    }

    [Fact]
    public async Task FindDtros_ReturnsOnlyDtrosAfterSpecifiedPublicationTime()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.FindDtros(new DtroSearch { Page = 1, PageSize = 10, Queries = new List<SearchQuery> { new() { PublicationTime = new DateTime(2023, 07, 21) } } });

        Assert.Equal(1, result.Results.Count);
        Assert.Contains(_dtroWithCreationDate, result.Results);
    }

    [Fact]
    public async Task FindDtros_ReturnsOnlyDtrosAfterSpecifiedModificationTime()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.FindDtros(new DtroSearch { Page = 1, PageSize = 10, Queries = new List<SearchQuery> { new() { ModificationTime = new DateTime(2023, 07, 21) } } });

        Assert.Equal(1, result.Results.Count);
        Assert.Contains(_dtroWithModificationTime, result.Results);
    }

    [Fact]
    public async Task FindDtros_ReturnsOnlyDtrosContainingSpecifiedStringInName()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.FindDtros(new DtroSearch { Page = 1, PageSize = 10, Queries = new List<SearchQuery> { new() { TroName = "test" } } });

        Assert.Equal(1, result.Results.Count);
        Assert.Contains(_dtroWithName, result.Results);
    }

    [Fact]
    public async Task FindDtros_ReturnsOnlyDtrosContainingSpecifiedVehicleType()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.FindDtros(new DtroSearch { Page = 1, PageSize = 10, Queries = new List<SearchQuery> { new() { VehicleType = "taxi" } } });

        Assert.Equal(1, result.Results.Count);
        Assert.Contains(_dtroWithVehicleTypes, result.Results);
    }

    [Fact]
    public async Task FindDtros_ReturnsOnlyDtrosContainingSpecifiedRegulationTypes()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.FindDtros(new DtroSearch { Page = 1, PageSize = 10, Queries = new List<SearchQuery> { new() { RegulationType = "test-regulation-type" } } });

        Assert.Equal(1, result.Results.Count);
        Assert.Contains(_dtroWithRegulationTypes, result.Results);
    }

    [Fact]
    public async Task FindDtros_ReturnsOnlyDtrosContainingSpecifiedOrderReportingPoint()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.FindDtros(new DtroSearch { Page = 1, PageSize = 10, Queries = new List<SearchQuery> { new() { OrderReportingPoint = "test-orp" } } });

        Assert.Equal(1, result.Results.Count);
        Assert.Contains(_dtroWithOrderReportingPoint, result.Results);
    }

    [Fact]
    public async Task FindDtrosForEvents_ReturnsDeleted_ByDefault()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.FindDtros(new DtroEventSearch { Page = 1, PageSize = 10 });

        Assert.Contains(_deletedDtro, result);
    }

    [Fact]
    public async Task FindDtrosForEvents_ReturnsDeleted_WhenDeletionTimeInQuery()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.FindDtros(new DtroEventSearch { Page = 1, PageSize = 10,  DeletionTime = new(2023, 07, 22) });

        Assert.Single(result);
        Assert.Contains(_deletedDtro, result);
    }

    [Fact]
    public async Task FindDtrosForEvents_ReturnsDeletedAfterDeletionTime_WhenDeletionTimeInQuery()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.FindDtros(new DtroEventSearch { Page = 1, PageSize = 10, DeletionTime = new(2023, 07, 24) });

        Assert.DoesNotContain(_deletedDtro, result);
    }

    [Fact]
    public async Task FindDtrosForEvents_ReturnsOnlyDtrosWithSpecifiedTrafficAuthorityId()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.FindDtros(new DtroEventSearch { Page = 1, PageSize = 10, Ta = 1234 });

        Assert.Single(result);
        Assert.Contains(_dtroWithTa1234, result);
    }

    [Fact]
    public async Task FindDtrosForEvents_ReturnsOnlyDtrosAfterSpecifiedPublicationTime()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.FindDtros(new DtroEventSearch { Page = 1, PageSize = 10, Since = new DateTime(2023, 07, 21) });

        Assert.Single(result);
        Assert.Contains(_dtroWithCreationDate, result);
    }

    [Fact]
    public async Task FindDtrosForEvents_ReturnsOnlyDtrosAfterSpecifiedModificationTime()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.FindDtros(new DtroEventSearch { Page = 1, PageSize = 10, ModificationTime = new DateTime(2023, 07, 21) });

        Assert.Single(result);
        Assert.Contains(_dtroWithModificationTime, result);
    }

    [Fact]
    public async Task FindDtrosForEvents_ReturnsOnlyDtrosContainingSpecifiedStringInName()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.FindDtros(new DtroEventSearch { Page = 1, PageSize = 10, TroName = "test" });

        Assert.Single(result);
        Assert.Contains(_dtroWithName, result);
    }

    [Fact]
    public async Task FindDtrosForEvents_ReturnsOnlyDtrosContainingSpecifiedVehicleType()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.FindDtros(new DtroEventSearch { Page = 1, PageSize = 10, VehicleType = "taxi" });

        Assert.Single(result);
        Assert.Contains(_dtroWithVehicleTypes, result);
    }

    [Fact]
    public async Task FindDtrosForEvents_ReturnsOnlyDtrosContainingSpecifiedRegulationTypes()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.FindDtros(new DtroEventSearch { Page = 1, PageSize = 10, RegulationType = "test-regulation-type" });

        Assert.Single(result);
        Assert.Contains(_dtroWithRegulationTypes, result);
    }

    [Fact]
    public async Task FindDtrosForEvents_ReturnsOnlyDtrosContainingSpecifiedOrderReportingPoint()
    {
        SqlStorageService sut = new(_context, _spatialProjectionService, _mappingServiceMock.Object);

        var result = await sut.FindDtros(new DtroEventSearch { Page = 1, PageSize = 10, OrderReportingPoint = "test-orp" });

        Assert.Single(result);
        Assert.Contains(_dtroWithOrderReportingPoint, result);
    }
}
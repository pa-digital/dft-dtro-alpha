using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DfT.DTRO.Attributes;
using DfT.DTRO.Models;
using DfT.DTRO.Models.Filtering;
using DfT.DTRO.Models.Pagination;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DfT.DTRO.Services.Storage;

/// <summary>
/// An implementation of <see cref="IStorageService"/>
/// that uses Google Filestore as its data store.
/// </summary>
public class FileStorageService : IStorageService
{
    private readonly IConfiguration _configuration;
    private string _bucket;

    /// <summary>
    /// Informs whether this <see cref="IStorageService"/>
    /// implementation is capable of searching dtros.
    /// <br/><br/>
    /// Always <see langword="false"/> in case of <see cref="FileStorageService"/>.
    /// </summary>
    public bool CanSearch => false;

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="configuration">App setting configurations.</param>
    public FileStorageService(IConfiguration configuration)
    {
        _configuration = configuration;
        _bucket = _configuration.GetSection("BucketName").Value;
    }

    /// <inheritdoc />
    public async Task SaveDtroAsJson(Guid id, JObject jsonContent)
    {
        var storageClient = StorageClient.Create();
        await storageClient.UploadObjectAsync(
            _bucket,
            $"{id}.json",
            "application/json",
            GenerateStreamFromString(jsonContent.ToString())
        );
    }

    /// <inheritdoc />
    public async Task UpdateDtroAsJson(Guid id, JObject jsonContent)
    {
        var storageClient = StorageClient.Create();

        var metadata = await storageClient.GetObjectAsync(_bucket, $"{id}.json");
        await storageClient.UploadObjectAsync(
            metadata,
            GenerateStreamFromString(jsonContent.ToString())
        );
    }

    /// <inheritdoc />    
    public async Task<Models.DTRO> GetDtroById(Guid id)
    {
        var dtroAsString = await GetDtroAsString(id);

        var result = JsonConvert.DeserializeObject<Models.DTRO>(dtroAsString);

        return result;
    }

    private async Task<string> GetDtroAsString(Guid id)
    {
        var storageClient = StorageClient.Create();

        using Stream stream = new MemoryStream();
        await storageClient.DownloadObjectAsync(_bucket, $"{id}.json", stream);
        stream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Private helper for creating a stream from a string.
    /// </summary>
    /// <param name="s">The string to be rendered to a Stream.</param>
    /// <returns><see cref="Stream"/></returns>
    private static Stream GenerateStreamFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    /// <inheritdoc /> 
    public async Task SaveDtroAsJson(Guid id, object data)
    {
        var storageClient = StorageClient.Create();
        await storageClient
            .UploadObjectAsync(
                _bucket,
                $"{id}.json",
                "application/json",
                GenerateStreamFromString(JsonConvert.SerializeObject(data))
            );
    }

    /// <inheritdoc />
    public async Task UpdateDtroAsJson(Guid id, object data)
    {
        var props = data.GetType().GetProperties();

        var ignoredProps = props.Where(prop => prop.HasAttribute<SaveOnceAttribute>());

        if (!ignoredProps.Any())
        {
            await SaveDtroAsJson(id, data);
            return;
        }

        var existing = await GetDtroById(id);

        foreach (var prop in props.Except(ignoredProps))
        {
            prop.SetValue(existing, prop.GetValue(data));
        }

        var storageClient = StorageClient.Create();

        await storageClient
            .UploadObjectAsync(
                _bucket,
                $"{id}.json",
                "application/json",
                GenerateStreamFromString(JsonConvert.SerializeObject(existing))
            );
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteDtro(Guid id, DateTime? deletionTime)
    {
        deletionTime ??= DateTime.UtcNow;

        var existing = await GetDtroById(id);

        if (existing is null)
        {
            return false;
        }

        existing.Deleted = true;
        existing.DeletionTime = deletionTime;

        var storageClient = StorageClient.Create();

        await storageClient
            .UploadObjectAsync(
                _bucket,
                $"{id}.json",
                "application/json",
                GenerateStreamFromString(JsonConvert.SerializeObject(existing))
            );

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> DtroExists(Guid id)
    {
        Models.DTRO dtro;

        try
        {
            dtro = await GetDtroById(id);
        }
        catch
        {
            return false;
        }

        return !(dtro?.Deleted ?? true);
    }

    /// <inheritdoc/>
    public async Task<bool> TryUpdateDtroAsJson(Guid id, object data)
    {
        var existing = await GetDtroById(id);

        if (existing.Deleted) { return false; }

        var props = data.GetType().GetProperties();

        var ignoredProps = props.Where(prop => prop.HasAttribute<SaveOnceAttribute>());

        if (!ignoredProps.Any())
        {
            await SaveDtroAsJson(id, data);
            return true;
        }

        foreach (var prop in props.Except(ignoredProps))
        {
            prop.SetValue(existing, prop.GetValue(data));
        }

        var storageClient = StorageClient.Create();

        await storageClient
            .UploadObjectAsync(
                _bucket,
                $"{id}.json",
                "application/json",
                GenerateStreamFromString(JsonConvert.SerializeObject(existing))
            );

        return true;
    }

    /// <inheritdoc />
    public Task<PaginatedResult<Models.DTRO>> FindDtros(DtroSearch search)
    {
        throw new NotImplementedException("Operation not available for FileStorageService.");
    }

    /// <inheritdoc />
    public Task<List<Models.DTRO>> FindDtros(DtroEventSearch search)
    {
        throw new NotImplementedException("Operation not available for FileStorageService.");
    }
}

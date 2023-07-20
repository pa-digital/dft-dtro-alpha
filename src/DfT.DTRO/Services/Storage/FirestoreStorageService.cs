using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DfT.DTRO.Attributes;
using Google.Cloud.Firestore;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DfT.DTRO.Services.Storage;

/// <summary>
/// An implementation of <see cref="IStorageService"/>
/// that uses Google Firestore as its data store.
/// </summary>
public class FirestoreStorageService : IStorageService
{
    private readonly IConfiguration _configuration;

    private readonly FirestoreDb _firestoreDb;

    private readonly string collectionId = "dtros";

    /// <summary>
    /// Informs whether this <see cref="IStorageService"/>
    /// implementation is capable of searching dtros.
    /// <br/><br/>
    /// Always <see langword="true"/> in case of <see cref="FirestoreStorageService"/>.
    /// </summary>
    public bool CanSearch => true;

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="configuration"></param>
    public FirestoreStorageService(IConfiguration configuration)
    {
        _configuration = configuration;
        _firestoreDb = FirestoreDb.Create(_configuration.GetSection("ProjectId").Value);
    }

    /// <inheritdoc />   
    public async Task<Models.DTRO> GetDtroById(Guid id)
    {
        var docRef = _firestoreDb.Collection(collectionId).Document(id.ToString());
        var snapshot = await docRef.GetSnapshotAsync();

        if (snapshot.Exists)
        {
            Console.WriteLine("Document data for {0} document:", snapshot.Id);
            return snapshot.ConvertTo<Models.DTRO>();
        }
        else
        {
            throw new KeyNotFoundException();
        }
    }

    /// <inheritdoc />   
    public async Task SaveDtroAsJson(Guid id, JObject jsonContent)
    {
        var dtro = JsonConvert.DeserializeObject<ExpandoObject>(jsonContent.ToString());

        var docRef = _firestoreDb.Collection(collectionId).Document(id.ToString());

        await docRef.SetAsync(dtro);
    }

    /// <inheritdoc /> 
    public async Task SaveDtroAsJson(Guid id, object data)
    {
        var docRef = _firestoreDb.Collection(collectionId).Document(id.ToString());
        await docRef.SetAsync(data);
    }

    /// <inheritdoc/>
    public Task UpdateDtroAsJson(Guid id, JObject jsonContent)
        => SaveDtroAsJson(id, jsonContent);
    
    /// <inheritdoc/>
    public async Task UpdateDtroAsJson(Guid id, object data)
    {
        var props = data.GetType().GetProperties().Where(it => !it.HasAttribute<SaveOnceAttribute>());

        Dictionary<string, object> changes = new();

        foreach (var prop in props)
        {
            if (prop.GetCustomAttribute<FirestorePropertyAttribute>() is FirestorePropertyAttribute firestoreProp)
            {
                object value = prop.GetValue(data);

                if (firestoreProp.ConverterType is Type converterType)
                {
                    var converter = Activator.CreateInstance(converterType);
                    value = converterType.GetMethod("ToFirestore").Invoke(converter, new object[] { value });
                }

                changes[firestoreProp.Name] = value;
            }
            else
            {
                changes[prop.Name] = prop.GetValue(data);
            }
        }

        var docRef = _firestoreDb.Collection(collectionId).Document(id.ToString());

        await docRef.UpdateAsync(changes);
    }


    /// <inheritdoc/>
    public async Task<bool> DeleteDtro(Guid id, System.DateTime? deletionTime = null)
    {
        deletionTime ??= System.DateTime.UtcNow;

        var docRef = _firestoreDb.Collection(collectionId).Document(id.ToString());

        try
        {
            var writeResult = await docRef.UpdateAsync(new Dictionary<string, object>
            {
                { "deleted", true },
                { "deletionTime", deletionTime }
            });
        }
        catch (RpcException ex)
        {
            if (ex.StatusCode == StatusCode.NotFound)
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> DtroExists(Guid id)
    {
        var snap = await _firestoreDb.Collection(collectionId).Document(id.ToString()).GetSnapshotAsync();

        return snap.Exists && !(snap.TryGetValue("deleted", out bool deleted) && deleted);
    }

    /// <inheritdoc/>
    public async Task<bool> TryUpdateDtroAsJson(Guid id, object data)
    {
        if (!await DtroExists(id))
        {
            return false;
        }

        await UpdateDtroAsJson(id, data);

        return true;
    }

    /// <inheritdoc />
    public async Task<List<Models.DTRO>> FindDtros(DateTime? minimumPublicationTime)
    {
        Query filteredQuery = _firestoreDb.Collection(collectionId);
            //.Select("data.source.troName", "data.source.ha", "data.source.provision", "created");

        if (minimumPublicationTime is not null)
        {
            filteredQuery = filteredQuery.WhereGreaterThanOrEqualTo("created", minimumPublicationTime);
        }

        AggregateQuerySnapshot countQueryResult = await filteredQuery.Count().GetSnapshotAsync();
        if (countQueryResult.Count is 0 or null)
        {
            return new List<Models.DTRO>();
        }

        QuerySnapshot queryResult = await filteredQuery
            .GetSnapshotAsync();

        return queryResult.Select(document => document.ConvertTo<Models.DTRO>()).ToList();
    }
}

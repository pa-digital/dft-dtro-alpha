using System.Collections.Generic;
using System.Dynamic;
using DfT.DTRO.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Dft.DTRO.Tests;

public class FirestoreDtroConverterTests
{
    [Fact]
    public void ToFirestore_WrapsNestedArrays()
    {
        const string json = @"{""array"": [[]]}";

        var sut = new FirestoreDtroConverter();

        var obj = JsonConvert.DeserializeObject<ExpandoObject>(json, new ExpandoObjectConverter());

        dynamic result = sut.ToFirestore(obj);

        Assert.NotNull(result);
        Assert.NotNull(result.array);
        Assert.NotNull(result.array[0]);
        Assert.IsType<ExpandoObject>(result.array[0]);

        var nestedArray = (ExpandoObject)result.array[0];

        Assert.True(nestedArray.Single().Key == "$array");

        var nestedArrayValue = nestedArray.Single().Value as List<object>;

        Assert.NotNull(nestedArrayValue);
    }

    [Fact]
    public void ToFirestore_ConvertsObjects()
    {
        const string json = @"{""nestedObj"": {""someValue"": 1}}";

        var sut = new FirestoreDtroConverter();

        var obj = JsonConvert.DeserializeObject<ExpandoObject>(json, new ExpandoObjectConverter());

        dynamic result = sut.ToFirestore(obj);

        Assert.NotNull(result.nestedObj);
        Assert.NotNull(result.nestedObj.someValue);
        Assert.IsType<long>(result.nestedObj.someValue);
        Assert.Equal(1, (long)result.nestedObj.someValue);
    }

    [Fact]
    public void ToFirestore_PrimitivesAreCopiedByValue()
    {
        const string json = @"{""value"": 1}";

        var sut = new FirestoreDtroConverter();

        dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(json, new ExpandoObjectConverter())!;

        dynamic result = sut.ToFirestore(obj);

        Assert.NotNull(result.value);
        Assert.IsType<long>(result.value);
        Assert.Equal(1, (long)result.value);

        result.value = 2;

        Assert.Equal(2, result.value);
        Assert.Equal(1, obj.value);
    }

    [Fact]
    public void FromFirestore_UnwrapsNestedArrays()
    {
        dynamic data = new ExpandoObject();
        data.array = new List<object>();
        var nested = new ExpandoObject();
        nested.TryAdd("$array", new List<object>() { 1 });
        data.array.Add(nested);

        var sut = new FirestoreDtroConverter();

        dynamic result = sut.FromFirestore(data);

        Assert.NotNull(result);
        Assert.NotNull(result.array);
        Assert.IsType<List<object>>(result.array);

        var array = result.array as List<object>;

        Assert.NotEmpty(array);

        Assert.IsType<List<object>>(array!.SingleOrDefault());

        var nestedArray = array!.Single() as List<object>;

        Assert.NotEmpty(nestedArray);
        Assert.Equal(1, nestedArray!.Single());
    }
}

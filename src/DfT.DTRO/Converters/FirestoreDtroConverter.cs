using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using Google.Cloud.Firestore;

namespace DfT.DTRO.Converters;

/// <summary>
/// A converter to convert the data field in the DTRO to and from Firestore
/// (Resolves the lack of support for nested arrays in Firestore).
/// </summary>
public class FirestoreDtroConverter : IFirestoreConverter<ExpandoObject>
{
    /// <inheritdoc/>
    public ExpandoObject FromFirestore(object obj)
    {
        var valueAsDict = obj as IDictionary<string, object>;

        var exp = new ExpandoObject();

        foreach (var (key, value) in valueAsDict)
        {
            AddFieldFromFirestore(exp, key, value);
        }

        return exp;
    }

    private static void AddFieldFromFirestore(ExpandoObject target, string targetKey, object obj)
    {
        if (obj is IList list)
        {
            var newList = new List<object>();
            foreach (var item in list)
            {
                AddFieldFromFirestore(newList, item);
            }
            target.TryAdd(targetKey, newList);
        }
        else if (obj is IDictionary<string, object> exp)
        {
            if (exp.TryGetValue("$array", out object array))
            {
                var nestedList = array as IList;
                var newList = new List<object>();
                foreach (var el in nestedList)
                {
                    AddFieldFromFirestore(newList, el);
                }
                target.TryAdd(targetKey, newList);

                return;
            }
            var newObj = new ExpandoObject();
            foreach (var (key, value) in exp)
            {
                AddFieldFromFirestore(newObj, key, value);
            }
            target.TryAdd(targetKey, newObj);
        }
        else
        {
            var _ = obj switch
            {
                Timestamp ts => target.TryAdd(targetKey, ts.ToDateTime()),
                _ => target.TryAdd(targetKey, obj)
            };
        }
    }

    private static void AddFieldFromFirestore(IList target, object obj)
    {
        if (obj is IList list)
        {
            var newList = new List<object>();
            foreach (var item in list)
            {
                AddFieldToFirestore(newList, item);
            }
            target.Add(newList);
        }
        else if (obj is IDictionary<string, object> exp)
        {
            if (exp.TryGetValue("$array", out object array))
            {
                var nestedList = array as IList;
                var newList = new List<object>();
                foreach (var el in nestedList)
                {
                    AddFieldFromFirestore(newList, el);
                }
                target.Add(newList);

                return;
            }
            var newObj = new ExpandoObject();
            foreach (var (key, value) in exp)
            {
                AddFieldFromFirestore(newObj, key, value);
            }
            target.Add(newObj);
        }
        else
        {
            target.Add(obj);
        }
    }

    /// <inheritdoc/>
    public object ToFirestore(ExpandoObject obj)
    {
        var newObject = new ExpandoObject();

        foreach (var (key, value) in obj)
        {
            AddFieldToFirestore(newObject, key, value);
        }

        return newObject;
    }

    private static void AddFieldToFirestore(ExpandoObject target, string targetKey, object obj)
    {
        if (obj is IList list)
        {
            var newList = new List<object>();
            foreach (var item in list)
            {
                AddFieldToFirestore(newList, item);
            }
            target.TryAdd(targetKey, newList);
        }
        else if (obj is ExpandoObject exp)
        {
            var newObj = new ExpandoObject();
            foreach (var (key, value) in exp)
            {
                AddFieldToFirestore(newObj, key, value);
            }
            target.TryAdd(targetKey, newObj);
        }
        else
        {
            target.TryAdd(targetKey, obj);
        }
    }

    private static void AddFieldToFirestore(IList target, object obj)
    {
        if (obj is IList list)
        {
            var newObj = new ExpandoObject();
            var newList = new List<object>();
            foreach (var item in list)
            {
                AddFieldToFirestore(newList, item);
            }
            newObj.TryAdd("$array", newList);
            target.Add(newObj);
        }
        else if (obj is ExpandoObject exp)
        {
            var newObj = new ExpandoObject();
            foreach (var (key, value) in exp)
            {
                AddFieldToFirestore(newObj, key, value);
            }
            target.Add(newObj);
        }
        else
        {
            target.Add(obj);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

public static class DictionaryExtensions
{
    public static string DicToString<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dData)
    {
        string strData = "";
        if (dData != null)
        {
            List<string> str_tmp = new List<string>();
            foreach (var i in dData)
            {
                str_tmp.Add(Convert.ToInt64(i.Key).ToString() + ',' + i.Value);
            }
            strData = string.Join(";", str_tmp);

        }
        return strData;
    }

    public static Dictionary<TKey, TValue> AddRange<TKey, TValue>(this Dictionary<TKey, TValue> aDict, Dictionary<TKey, TValue> bDict)
    {
        foreach (var item in bDict)
        {
            if (!aDict.ContainsKey(item.Key))
                aDict[item.Key] = item.Value;
        }
        return aDict;
    }

    public static Dictionary<TKey, TValue> AddRange<TKey, TValue>(this Dictionary<TKey, TValue> aDict, IReadOnlyDictionary<TKey, TValue> bDict)
    {
        foreach (var item in bDict)
        {
            if (!aDict.ContainsKey(item.Key))
                aDict.Add(item.Key, item.Value);
            else
                aDict[item.Key] = item.Value;
        }
        return aDict;
    }

    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> iEnumrable)
    {
        Dictionary<TKey, TValue> ret = new Dictionary<TKey, TValue>();
        if (iEnumrable == null) return ret;

        foreach (var item in iEnumrable)
        {
            ret[item.Key] = item.Value;
        }

        return ret;
    }

    /// <summary>
    /// 该方法仅适用于字典长度为一的情况
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="pair"></param>
    /// <returns></returns>
    public static KeyValuePair<TKey, TValue> ToDicPair<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> pair)
    {
        if (pair.Count > 1)
            return default;
        return pair.First();
    }

    public static Dictionary<TKey, KeyValuePair<TValue, TValue>> ToDicValue2<TKey, TValue>(this IReadOnlyDictionary<TKey, List<TValue>> dic)
    {
        var rst = new Dictionary<TKey, KeyValuePair<TValue, TValue>>();
        foreach (var item in dic)
        {
            if (item.Value.Count != 2)
                return null;
            else
            {
                var pair = new KeyValuePair<TValue, TValue>(item.Value[0], item.Value[1]);
                rst.Add(item.Key, pair);
            }
        }
        return rst;
    }

    public static Dictionary<TKey, (TValue, TValue, TValue)> ToDicValue3<TKey, TValue>(this IReadOnlyDictionary<TKey, List<TValue>> dic)
    {
        var rst = new Dictionary<TKey, (TValue, TValue, TValue)>();
        foreach (var item in dic)
        {
            if (item.Value.Count != 3)
                return null;
            else
            {
                var tupple = (item.Value[0], item.Value[1], item.Value[2]);
                rst.Add(item.Key, tupple);
            }
        }
        return rst;
    }
}
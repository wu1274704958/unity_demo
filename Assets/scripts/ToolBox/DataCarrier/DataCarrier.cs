using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataCarrier : MonoBehaviour
{
    [SerializeField]
    protected List<string> keys = new List<string>();
    [SerializeField]
    protected List<string> values = new List<string>();
    protected Dictionary<string, (System.Type, object)> cache = new Dictionary<string, (System.Type, object)>();

    public bool Add(string k,string v)
    {
        if (k.Length == 0 || v.Length == 0 || Contain(k)) return false;
        keys.Add(k);
        values.Add(v);
        return true;
    }

    public void Remove(string k)
    {
        int index = -1;
        if (k.Length == 0 || (index = GetKeyIndex(k)) < 0) return;
        cache.Remove(keys[index]);
        keys.RemoveAt(index);
        values.RemoveAt(index);
    }

    public void Remove(int i)
    {
        if (!good_idx(i)) return;
        keys.RemoveAt(i);
        values.RemoveAt(i);
    }
    public void ClearCache()
    {
        cache.Clear();
    }
    public bool Contain(string k)
    {
        return keys.FindIndex((a) => a == k) >= 0;
    }

    public bool SetVal(string k, string v)
    {
        int idx = GetKeyIndex(k);
        if (idx >= 0 && idx < values.Count)
        {
            values[idx] = v;
            return true;
        }
        return false;
    }

    public int GetKeyIndex(string k)
    {
        return keys.FindIndex((a) => a == k);
    }

    public T GetVal<T>(string k)
    {
        if(cache.TryGetValue(k,out var v))
        {
            if(v.Item1 == typeof(T))
            {
                return (T)v.Item2;
            }
            else
            {
                cache.Remove(k);
            }
        }
        int index = -1;
        if (k.Length == 0 || (index = GetKeyIndex(k)) < 0) return default(T);

        var d = Deserialize<T>(values[index]);
        cache.Add(k, (typeof(T), d));
        return d;
    }

    public bool good_idx(int i)
    {
        return i >= 0 && i < keys.Count;
    }

    public (string,string) Get(int i)
    {
        if(i >= 0 && i < keys.Count)
        {
            return (keys[i], values[i]);
        }
        return (null, null);
    }

    public int Count => keys.Count;

    protected T Deserialize<T>(string v)
    {
        return Serialize.Deserialize<T>(v);
    }
    
}

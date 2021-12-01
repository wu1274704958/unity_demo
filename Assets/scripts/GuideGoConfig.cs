using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[System.Serializable]
public class Customer
{
    public Customer(int id, string v) { Key = id;Value = v; }
    [SerializeField]
    public int Key { get; set; }
    [SerializeField]
    public string Value { get; set; }
}
public class GuideGoConfig : MonoBehaviour
{
    public List<string> Childlren = new List<string>();
    public List<int> Id = new List<int>();
    private bool TryGet(uint id, out string v)
    {
        v = null;
        for (int i = 0; i < Id.Count; ++i)
        {
            if (Id[i] == id)
            {
                v = Childlren[i];
                return true;
            }
        }
        return false;
    }

    public bool has(uint id)
    {
        return TryGet(id,out var v);
    }

    public Transform getGo(int id)
    {
        if(TryGet((uint)id,out var n))
        {
            var trans = FindEx(n);
            if (trans != null)
                return trans;
        }
        return null;
    }

    public int Count
    {
        get
        {
            return Id.Count;
        }
    }

    public bool TryGet(int i,out (int,string) v)
    {
        v.Item1 = -1;
        v.Item2 = "";
        if (Id == null || Childlren == null) return false;
        if(i >= 0 && i <Id.Count)
        {
            v.Item1 = Id[i];
            v.Item2 = Childlren[i];
            return true;
        }
        return false;
    }

    public GameObject getGoObj(int id)
    {
        var t = getGo(id);
        return t?.gameObject;
    }

    public Transform getGo(int id,string[] args)
    {
        if (TryGet((uint)id, out var n))
        {
            var trans = FindEx(string.Format(n,args));
            if (trans != null)
                return trans;
        }
        return null;
    }

    public GameObject getGoObj(int id, string[] args)
    {
        var t = getGo(id,args);
        return t?.gameObject;
    }

    public bool Remove(uint id)
    {
        for (int i = 0; i < Id.Count; ++i)
        {
            if (Id[i] == id)
            {
                Id.RemoveAt(i);
                Childlren.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    public bool Add(int id,string v)
    {
        if (!has((uint)id))
        {
            Id.Add(id);
            Childlren.Add(v);
            return true;
        }
        return false;
    }

    public void forEach(System.Action<int,GameObject> on)
    {
        for (int i = 0; i < Id.Count; ++i)
        {
            var id = Id[i];
            var ch = Childlren?[i];
            Transform trans = null;
            if(ch != null)
                trans = FindEx(ch);
            if (trans != null)
                on?.Invoke(id, trans.gameObject);
        }
    }

    private bool ParseSuffix(string path,out string pre,out string suffix,out char tag,out List<string> cnt)
    {
        tag = '\0';
        cnt = null;
        pre = suffix = null;
        if (path == null || path.Length == 0) return false;
        int stage = 0;
        int left = -1,right = -1;
        for(int i = 0;i < path.Length;++i)
        {
            if(stage == 0 && (path[i] == '@' || path[i] == '#' || path[i] == '$' || path[i] == '&' || path[i] == '%'))
            {
                ++stage;
                pre = path.Substring(0, i);
                tag = path[i];
                continue;
            }
            if(stage == 1)
            {
                if (path[i] != ' ' && path[i] != '[') return false;
                if (path[i] == '[')
                {
                    ++stage;
                    left = i;
                    continue;
                }
            }
            if(stage == 2 && path[i] == ']')
            {
                ++stage;
                right = i;
                suffix = i + 1 < path.Length ? path.Substring(i + 1) : "";
                var mid = path.Substring(left + 1, right - (left + 1));
                cnt = mid.Split(';').ToList<string>();
                continue;
            }
        }
        return stage == 3;
    }

    public Transform GetByNameEx(Transform p,string path)
    {
        if (p == null) return null;
        if(ParseSuffix(path,out var pre,out var suffix,out var tag,out var cnts))
        {
            if(tag == '@' && cnts.Count >= 1 && int.TryParse(cnts[0],out var idx))
            {
                if(pre == "" && p.childCount > idx)
                {
                    return p.GetChild(idx);
                }
                int k = 0;Transform last = null;
                for(int i = 0;i < p.childCount;++i)
                {
                    var it = p.GetChild(i);
                    if(it.name == pre)
                    {
                        if (k == idx)
                            return it;
                        last = it;
                        ++k;
                    }
                }
                return last;
            }
            if(tag == '%' && pre != null && pre.Length > 0 && cnts.Count >= 2)
            {
                return FindByUnit(p,pre,cnts);
            }
            else
            {
                return null;
            }
        }
        else
        {
            return p.Find(path);
        }
        return null;
    }

    private Transform FindByUnit(Transform p, string pre, List<string> cnts)
    {
        if(cnts[0] == "ID")
        {
            for (int i = 0; i < p.childCount; ++i)
            {
                var it = p.GetChild(i);
                var comp = it.GetComponentInChildren<GuideGoConfigID>();
                if (it.name == pre && comp != null && uint.TryParse(cnts[1],out var id))
                {
                    if (id == comp.ID) return it;
                }
            }
        }
        return null;
    }

    public Transform FindEx(string path,Transform trans = null)
    {
        if (trans == null) trans = transform;
        int b = path.IndexOf('/');
        if (b < 0) return GetByNameEx(trans,path);
        var curr = path.Substring(0, b);
        if (b + 1 >= path.Length) return GetByNameEx(transform, curr);
        var next = path.Substring(b + 1);
        var n = GetByNameEx(trans, curr);
        if (n == null) return null;
        return FindEx(next, n);
    }
}

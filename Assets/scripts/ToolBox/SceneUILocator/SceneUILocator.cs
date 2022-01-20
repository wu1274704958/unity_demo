using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class SceneUILocator : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> prefabs = new List<GameObject>();
    private Dictionary<string, (Transform, RectTransform,Vector2)> bind = new Dictionary<string, (Transform, RectTransform, Vector2)>();
    [SerializeField]
    private Camera cam;
    [SerializeField]
    private Camera uiCam;
    protected Camera mainCam
    {
        get
        {
            if (cam == null) cam = CameraMgr.Instance.MainCamera;
            return cam;
        }
    }
    protected Camera UiCam
    {
        get
        {
            if (uiCam == null) uiCam = CameraMgr.Instance.UICamera;
            return uiCam;
        }
    }
    // Start is called before the first frame update
    private void Awake()
    {
        var rt = transform as RectTransform;
        if (rt != null)
        {
            rt.pivot = new Vector2(.5f, .5f);
            rt.anchorMin = new Vector2(0.0f, 0.0f);
            rt.anchorMax = new Vector2(1.0f, 1.0f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }
        foreach(var prefab in prefabs)
            prefab?.SetActive(false);
    }
    public void ClearAll()
    {
        for(int i = 0;i < transform.childCount;++i)
        {
            var c = transform.GetChild(i);
            if(c.name.StartsWith("SceneUIL_"))
            {
                Destroy(c.gameObject);
            }
        }
        bind.Clear();
    }

    public void Clear(int idx,string name)
    {
        var key = GetName(idx, name);
        var c = transform.Find(key);
        if (c != null)
        {
            Destroy(c.gameObject);
            bind.Remove(key);
        }
    }

    public string GetName(int idx,string name)
    {
        return string.Format("SceneUIL_{0}_{1}", idx, name);
    }
    public bool ParseName(string n,out int idx,out string name)
    {
        idx = -1;name = null;
        var arr = n.Split('_');
        if (arr.Length < 3) return false;
        if (arr[0] != "SceneUIL") return false;
        if (!int.TryParse(arr[1], out idx)) return false;
        StringBuilder sb = new StringBuilder();
        for (int i = 2; i < arr.Length; ++i)
        {
            if (i > 2) sb.Append('_');
            sb.Append(arr[i]);
        }
        name = sb.ToString();
        return true;
    }

    public void Add(int idx,string name,Transform obj,float x = 0.0f,float y = 0.0f,Action<GameObject> onCreate = null)
    {
        if (cam == null) cam = CameraMgr.Instance.MainCamera;
        if (cam == null || obj == null || name == null || name.Length == 0) return;
        GameObject ui = InstanceUI(idx, name);
        if (ui == null) return;
        var off = new Vector2(x, y);
        UpdateUIpos(obj, ui.transform as RectTransform, off);
        onCreate?.Invoke(ui);
        bind.Add(GetName(idx, name), (obj,ui.transform as RectTransform,off));
    }

    protected GameObject InstanceUI(int idx,string name)
    {
        if(idx >= 0 && idx < prefabs.Count && prefabs[idx] != null)
        {
            var obj = Instantiate(prefabs[idx],transform);
            obj.name = GetName(idx, name);
            obj.transform.localScale = Vector3.one;
            obj.transform.localPosition = Vector3.zero;
            obj.transform.rotation = Quaternion.identity;
            obj.SetActive(true);
            return obj;
        }
        return null;
    }

    protected void UpdateUIpos(Transform obj,RectTransform ui,Vector2 offset)
    {
        var pos = mainCam?.WorldToScreenPoint(obj.position);
        if(RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, pos.GetValueOrDefault(), UiCam, out var uiPos))
            ui.localPosition = uiPos + offset;
    }

    void Update()
    {
        foreach(var it in bind)
        {
            UpdateUIpos(it.Value.Item1, it.Value.Item2, it.Value.Item3);
        }
    }

    public GameObject GetObject(int idx,string name)
    {
        var k = GetName(idx, name);
        if(bind.TryGetValue(k,out var v))
        {
            return v.Item2.gameObject;
        }
        return null;
    }

    public GameObject GetObject(string name)
    {
        foreach(var it in bind)
        {
            if(ParseName(it.Key,out var idx,out var n) && n == name)
            {
                return it.Value.Item2.gameObject;
            }
        }
        return null;
    }
}

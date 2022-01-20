using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StarLevel : MonoBehaviour
{
    [SerializeField]
    private GameObject prefab;
    [SerializeField]
    private LayoutGroup layout;
    [SerializeField]
    private int level;
    private void Awake()
    {
        prefab?.SetActive(false);
        layout?.transform?.DestroyAllChildren();
    }

    public void SetLevel(int lv)
    {
        if (lv < 0) lv = 0;
        if (layout == null || prefab == null) return;
        int i = 0;
        for(i = 0;i < lv;++i)
        {
            if (i >= layout.transform.childCount)
                layout.transform.cloneAdd(prefab);
            layout.transform.GetChild(i).gameObject.SetActive(true);
        }
        for(;i < layout.transform.childCount;++i)
            layout.transform.GetChild(i).gameObject.SetActive(false);
    }

    public void Reset()
    {
        SetLevel(level);
    }

}

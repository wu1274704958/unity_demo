using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TabUtil : MonoBehaviour
{
    [SerializeField]
    private List<Toggle> toggles = new List<Toggle>();
    [SerializeField]
    private List<Transform> tabs = new List<Transform>();
    [SerializeField]
    private int m_Select = -1;

    private List<UnityAction<bool>> actions = new List<UnityAction<bool>>();
    public int Select { get => m_Select; }
    public Action<int> OnChange;
    public Action<Transform> OnTableHide;
    public Action<Transform> OnTableShow;
    public int Count { get => toggles.Count; }


    private bool is_init = false;
    public void init(int sel)
    {
        m_Select = sel;
        init();
    }
    public void init()
    {
        RemoveListeners();
        refresh();
        for (int i = 0; i < toggles.Count; ++i)
        {
            AddListener(toggles[i], i);
        }
        is_init = true;
    }

    private void RemoveListeners()
    {
        foreach (var it in actions)
        {
            foreach (var t in toggles)
            {
                t.onValueChanged.RemoveListener(it);
            }
        }
    }

    private void AddListener(Toggle toggle, int i)
    {
        if (i >= actions.Count)
        { 
            initAllListeners();
        }
        toggles[i].onValueChanged.RemoveListener(actions[i]);
        toggles[i].onValueChanged.AddListener(actions[i]);
    }

    private void initAllListeners()
    {
        foreach(var it in actions)
        {
            foreach(var t in toggles)
            {
                t.onValueChanged.RemoveListener(it);
            }
        }
        actions.Clear();
        for (int i = 0; i < toggles.Count; ++i)
        {
            int idx = actions.Count;
            actions.Add((v) =>
            {
                if (v)
                    OnToggle(idx, v);
            });
        }
    }

    private void OnDisable()
    {
        foreach (var it in actions)
        {
            foreach (var t in toggles)
            {
                t.onValueChanged.RemoveListener(it);
            }
        }
        actions.Clear();
        is_init = false;
    }
    private void OnToggle(int i,bool v)
    {
        if (!good_idx(i)) return;
        if (i == m_Select) return;
        Change(m_Select, i);
    }
    private void Change(int old,int n)
    {
        m_Select = n;
        toggles[old].isOn = false;
        if (tabs[old] != null)
        {
            OnTableHide?.Invoke(tabs?[old]);
            tabs?[old]?.gameObject.SetActive(false);
        }
        if (tabs[n] != null)
        {
            tabs?[n]?.gameObject.SetActive(true);
            OnTableShow?.Invoke(tabs?[n]);
        }
        OnChange?.Invoke(n);
    }

    public void Remove(int did)
    {
        if(good_idx(did))
        {
            toggles[did].onValueChanged.RemoveListener(actions[did]);
            toggles.RemoveAt(did);
            tabs.RemoveAt(did);
            actions.RemoveAt(did);
        }
    }

    private void refresh()
    {
        for (int i = 0; i < toggles.Count; ++i)
        {
            toggles[i].isOn = false;
            if (tabs[i] != null)
            {
                OnTableHide?.Invoke(tabs?[i]);
                tabs?[i]?.gameObject.SetActive(false);
            }
        }
        if (good_idx(m_Select))
        {
            toggles[m_Select].isOn = true;
            if (tabs[m_Select] != null)
            {
                tabs?[m_Select]?.gameObject.SetActive(true);
                OnTableShow?.Invoke(tabs?[m_Select]);
            }
        }
    }

    public bool good_idx(int sel)
    {
        return sel >= 0 && sel < toggles.Count; 
    }

    public void addTab(Toggle toggle,Transform tab)
    {
        if (toggle == null ) return;
        toggles.Add(toggle);
        tabs.Add(tab);
        int idx = actions.Count;
        actions.Add((v) =>
        {
            if (v)
                OnToggle(idx, v);
        });
        if(is_init)
        {
            int i = toggles.Count - 1;
            toggle.onValueChanged.AddListener(actions[idx]);
        }
    }

    public (Toggle,Transform) GetToogle(int i)
    {
        if(good_idx(i))
        {
            return (toggles[i], tabs[i]);
        }
        return (null, null);
    }
}

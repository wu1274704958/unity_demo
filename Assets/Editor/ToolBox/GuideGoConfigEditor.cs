using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GuideGoConfig))]
public class GuideGoConfigEditor : Editor
{
    Transform add = null;
    string name_ = null;
    int id = 0;
    int did = 0;
    List<Transform> FoundTs = new List<Transform>();

    private void OnEnable()
    {

    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var tar = (GuideGoConfig)target;

        
        for(int i = 0;i < tar.Id.Count;++i)
        {
            GUILayout.Label(tar.Id[i].ToString());
            GUILayout.Label(tar.Childlren[i]);
            Transform t = i < FoundTs.Count ? FoundTs[i] : null;
            if (t == null && (t = tar.FindExLoop(tar.Childlren[i])) != null)
            {
                if (i < FoundTs.Count)
                    FoundTs[i] = t;
                else
                    FoundTs.Add(t);
            }
            if (i >= FoundTs.Count) continue;
            EditorGUILayout.ObjectField(FoundTs[i], typeof(Transform), true);
            if (t != null && GUILayout.Button("Clear Found"))
            {
                FoundTs[i] = null;
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (name_ == null && (add = ((Transform)EditorGUILayout.ObjectField(add, typeof(Transform), true))) != null)
        {
            var (res,name) = GetTransPath(tar.transform, add);
            if (!res)
            {
                add = null;
            }
            else
            {
                name_ = name;
            }
        }
        EditorGUI.BeginChangeCheck();
        if (add != null)
        {
            id = EditorGUILayout.IntField(id);
            name_ = GUILayout.TextField(name_);
            if (id >= 0 && name_.Length > 0 && GUILayout.Button("添加"))
            {
                tar.Add(id, name_);
                name_ = null;
                add = null;
            }
            
        }
        did = EditorGUILayout.IntField(did);
        if (did >= 0 && GUILayout.Button("删除"))
        {
            tar.Remove((uint)did);
        }
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }
    }

    public static (bool,string) GetTransPath(Transform p,Transform trans,string chname = "")
    {
        if (trans.parent == null) return (false, join(trans.name,chname));
        if (trans.parent == p) return (true, join(trans.name, chname));
        return GetTransPath(p, trans.parent, join(trans.name, chname));
    }

    private static string join(string pn,string name)
    {
        if (pn == "") return name;
        if (name == "") return pn;
        return pn + "/" + name;
    }
}

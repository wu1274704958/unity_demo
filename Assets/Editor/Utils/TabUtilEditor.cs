using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(TabUtil))]
public class TabUtilEditor : Editor
{
    private Toggle CurrTo;
    private Transform CurrTrans;
    int did = 0;
    private void OnEnable()
    {

    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var tar = (TabUtil)target;

        EditorGUI.BeginChangeCheck();
        for (int i = 0; i < tar.Count; ++i)
        {
            GUILayout.Label(i.ToString());
            var (tog, trans) = tar.GetToogle(i);
            EditorGUILayout.ObjectField(tog.transform, typeof(Transform), true);
            EditorGUILayout.ObjectField(trans, typeof(Transform), true);
        }

        EditorGUILayout.Space();

        CurrTo = ((Toggle)EditorGUILayout.ObjectField(CurrTo, typeof(Toggle), true));
        CurrTrans = ((Transform)EditorGUILayout.ObjectField(CurrTrans, typeof(Transform), true));

        if(CurrTo != null /*&& CurrTrans != null*/)
        {
            if (GUILayout.Button("添加"))
            {
                tar.addTab(CurrTo, CurrTrans);
                CurrTrans = null;
                CurrTo = null;
            }
        }
        did = EditorGUILayout.IntField(did);
        if (tar.good_idx(did) && GUILayout.Button("删除"))
        {
            tar.Remove(did);
        }
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }
    }

    public static (bool, string) GetTransPath(Transform p, Transform trans, string chname = "")
    {
        if (trans.parent == null) return (false, join(trans.name, chname));
        if (trans.parent == p) return (true, join(trans.name, chname));
        return GetTransPath(p, trans.parent, join(trans.name, chname));
    }

    private static string join(string pn, string name)
    {
        if (pn == "") return name;
        if (name == "") return pn;
        return pn + "/" + name;
    }
}

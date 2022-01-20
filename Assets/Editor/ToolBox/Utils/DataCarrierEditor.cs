using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(DataCarrier))]
public class DataCarrierEditor : Editor
{
    private string CurrK = "";
    private string CurrV = "";
    int did = 0;
    private List<string> temp = new List<string>();
    private void OnEnable()
    {

    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var tar = (DataCarrier)target;

        EditorGUI.BeginChangeCheck();
        for (int i = 0; i < tar.Count; ++i)
        {
            var (k, v) = tar.Get(i);
            GUILayout.Label(string.Format( "{2} : {0} => {1}",k,v,i));

            if (i >= temp.Count)
                temp.Add(v);

            temp[i] = GUILayout.TextField(temp[i]);
            if (temp[i].Length > 0 && temp[i] != v)
            {
                if(GUILayout.Button("修改"))
                {
                    if(!tar.SetVal(k, temp[i]))
                        temp[i] = v;
                }
            }
                
        }

        EditorGUILayout.Space();

        CurrK = GUILayout.TextField(CurrK);
        CurrV = GUILayout.TextField(CurrV);

        if(CurrK.Length > 0 && CurrV.Length > 0)
        {
            if (GUILayout.Button("添加"))
            {
                tar.Add(CurrK, CurrV);
                CurrK = CurrV = "";
            }
        }
        did = EditorGUILayout.IntField(did);
        if (tar.good_idx(did) && GUILayout.Button("删除"))
        {
            tar.Remove(did);
        }
        if(GUILayout.Button("清除缓存"))
        {
            tar.ClearCache();
        }
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }
    }
}

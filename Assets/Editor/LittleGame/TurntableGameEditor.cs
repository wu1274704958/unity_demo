using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TurntableGame))]
public class TurntableGameEditor : Editor
{
    public SerializedProperty LastPos;
    public SerializedProperty _State;
    public SerializedProperty SelectFragment;
    public SerializedProperty InMotion;
    public TurntableGame Target;
    private void OnEnable()
    {
        Target = target as TurntableGame;
        LastPos = serializedObject.FindProperty("LastPos");
        _State = serializedObject.FindProperty("_State");
        SelectFragment = serializedObject.FindProperty("SelectFragment");
        InMotion = serializedObject.FindProperty("InMotion");
    }

    

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();
        
        // EditorGUILayout.Space();
        // GUILayout.Label("运行时数据");
        // EditorGUILayout.PropertyField(LastPos, new GUIContent("LastPos"));
        // EditorGUILayout.PropertyField(_State, new GUIContent("_State"));
        // EditorGUILayout.PropertyField(SelectFragment, new GUIContent("SelectFragment"));
        // EditorGUILayout.PropertyField(InMotion, new GUIContent("InMotion"));
        // EditorGUILayout.Space();
        
        serializedObject.ApplyModifiedProperties();
        
        EditorGUI.BeginChangeCheck();
        if (GUILayout.Button("提取排布数据"))
            Target.ExtractArrangeData();
        if (GUILayout.Button("排布"))
            Target.Arrange();
        if(GUILayout.Button("测试平移"))
            Target.Translate(0,1);
        if(GUILayout.Button("测试平移反向"))
            Target.Translate(0,-1);
        if(GUILayout.Button("测试旋转"))
            Target.Rotate(1,1);
        if(GUILayout.Button("测试旋转反向"))
            Target.Rotate(1,-1);
        if(GUILayout.Button("测试旋转多个"))
            Target.Rotate(new int[]{ 1,2,3 }.ToList<int>(),-3);
        if(GUILayout.Button("测试平移多个"))
            Target.Translate(new int[]{ 0,1 }.ToList<int>(),2);
            
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(target);
        }
    }
}

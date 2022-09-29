using System;
using System.Collections;
using System.Collections.Generic;
using lt;
using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(KnobFragment))]
public class KnobFragmentEditor : Editor
{
    private KnobFragment Target;

    private void OnEnable()
    {
        Target = target as KnobFragment;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if(GUILayout.Button("SetNativeSize"))
            Target.SetNativeSize();
        if(GUILayout.Button("SetVerticesDirty"))
            Target.SetVerticesDirty();
            
    }
}

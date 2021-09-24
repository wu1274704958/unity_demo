using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(WheelUI))]
public class WheelUIEditor : Editor
{
    private string TF_Deg = "";
    private int idx = 0;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        WheelUI tar = (WheelUI)target;

        if (GUILayout.Button("Init"))
        {
            tar.InitPos(true);
        }
        if (GUILayout.Button("InitNoAdd"))
        {
            tar.InitPos();
        }
        if (GUILayout.Button("UpdatePos"))
        {
            tar.UpdateTransform();
        }
        float Degress = 0.0f;
        if (float.TryParse(TF_Deg = GUILayout.TextField(TF_Deg),out var degress))
        {
            Degress = degress;
        }
        if(GUILayout.Button("RotateTo"))
        {
            tar.RotateToFroce(Degress, 0.2f);
        }
        if (GUILayout.Button("RotateUnit"))
        {
            int len = tar.Num;
            if (tar.IsAnimation())
            {
                tar.forceStopAni();
            }
            tar.Rotate(-1, 0.5f,true,(t)=> {
                var next = idx + 1;
                if (next >= len) next = 0;
                var t1 = tar.Get(idx);
                var t2 = tar.Get(next);
                var v1 = 1.2f - 0.2f * t;
                var v2 = 1f + 0.2f * t;
                t1.localScale = new Vector3(v1,v1,v1);
                t2.localScale = new Vector3(v2,v2,v2);
            },()=> {
                idx += 1;
                if (idx >= len) idx = 0;
            });
        }
        if (GUILayout.Button("AutoBind"))
        {
            tar.AutoBind();
        }
        
    }
}
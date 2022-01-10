using UnityEngine;
using UnityEditor;
using Assets.Editor;
using System.Text;
using System.Collections.Generic;
using UnityEngine.UI;

public class GenCodeEditor
{
    public const int C_MAXVALUE = 0;
    static readonly TextEditor CopyTool = new TextEditor();

    public static string GenerateSubViewBind(bool part = true)
    {
        UnityEngine.Object[] o = Selection.GetFiltered(typeof(UnityEngine.GameObject), SelectionMode.ExcludePrefab);
        if (o != null && o.Length == 1 && o[0] is GameObject obj)
        {
            StringBuilder res = new StringBuilder();
            GenerateSubViewBindReal(res, obj);
            if (!part)
                GenerateSubViewMgrBindReal(res, obj);
            return res.ToString();
        }
        return "";
    }

    public static void GenerateSubViewBindReal(StringBuilder res,GameObject o)
    {
        Dictionary<string, (string, System.Type)> dic = new Dictionary<string, (string, System.Type)>();
        Dictionary<string, (string, System.Type, int)> arrDic = new Dictionary<string, (string, System.Type, int)>();
        FindAutoItemChildlern(o as GameObject, ref dic, ref arrDic, "", true, "SV_", "SVA_");

        string name = o.name;

        res.Append(string.Format(@"
public class {0}SubViewUI : SubViewUI
{{
", name));
        AppendMembers(res, dic, arrDic);
        res.Append(string.Format(@"
public {0}SubViewUI()
{{

}}
public override void init(Transform root)
{{
    base.init(root);
", name));
        AppendFindMembers(res, dic, arrDic);
        res.Append(@"}
}");
    }

    public static void GenerateSubViewMgrBindReal(StringBuilder res, GameObject o)
    {
        string name = o.name;

        res.Append(string.Format(@"
public class {0}SubViewMgr : SubViewMgr<{0}SubViewUI>
{{
    protected override void OnInit(){{}}
    protected override void OnConstruct() {{}}
    public override void OnOpen(object param) {{}}
    public override void Update() {{}}
    public override void LateUpdate() {{}}
    public override void Close() {{}}
    public override void Destroy() {{}}
    public override void ClearData() {{}}
    public override void EvtListener(bool isAdd) {{}}
    public override void MsgListener(bool isAdd) {{}}
    public override void RefreshData() {{}}
    public override void FixedUpdate() {{}}
}}
", name));
    }

    [MenuItem("GenCode/SubView/OnlyUI", priority = C_MAXVALUE + 1)]
    public static void GenerateItemUIBindPart()
    {
        var uiCode = GenerateSubViewBind();
        SaveToClipBorad(uiCode);
    }

    [MenuItem("GenCode/SubView/All", priority = C_MAXVALUE + 2)]
    public static void GenerateItemUIBind()
    {
        var uiCode = GenerateSubViewBind(false);
        SaveToClipBorad(uiCode);
    }

    public static void SaveToClipBorad(string str)
    {
        CopyTool.text = str;
        CopyTool.OnFocus();
        CopyTool.Copy();
    }

    public static void AppendMembers(StringBuilder res, Dictionary<string, (string, System.Type)> dic,Dictionary<string, (string, System.Type, int)> arrDic)
    {
        foreach (var it in dic)
        {
            res.AppendLine(string.Format("public {0} {1};", it.Value.Item2.FullName, it.Key));
        }
        foreach (var it in arrDic)
        {
            res.AppendLine(string.Format("public {0}[] {1};", it.Value.Item2.FullName, it.Key));
        }
    }

    public static void AppendFindMembers(StringBuilder res, Dictionary<string, (string, System.Type)> dic, Dictionary<string, (string, System.Type, int)> arrDic,
        string self = "this")
    {
        foreach (var it in dic)
        {
            res.AppendLine(string.Format("{3}.{0} = root.Find(\"{1}\").GetComponent<{2}>();", it.Key, it.Value.Item1, it.Value.Item2.FullName,self));
        }
        foreach (var it in arrDic)
        {
            res.AppendLine(string.Format("{3}.{0} = new {1}[{2}];", it.Key, it.Value.Item2.FullName, it.Value.Item3,self));
            for (int i = 0; i < it.Value.Item3; ++i)
            {
                res.AppendLine(string.Format("{4}.{0}[{2}] = root.Find(\"{3}\").GetChild({2}).GetComponent<{1}>();",
                    it.Key, it.Value.Item2.FullName, i, it.Value.Item1,self));
            }
        }
    }

    private static void FindAutoItemChildlern(GameObject gameObj, ref Dictionary<string, (string, System.Type)> dic,
        ref Dictionary<string, (string, System.Type, int)> arrDic, string parentName = "", bool isRoot = true, string HEAD = "IT_", string HEAD_ARR = "ITA_",
        System.Func<GameObject, System.Type> findGameObjectTy = null)
    {
        if (findGameObjectTy == null) findGameObjectTy = FindGameObjTypeDef;
        string name;
        if (parentName.Length == 0)
            name = gameObj.name +"/";
        else
            name = parentName  + gameObj.name +"/";
        if (isRoot) name = "";

        for (int i = 0; i < gameObj.transform.childCount; ++i)
        {
            var it = gameObj.transform.GetChild(i);
            if (it.name.StartsWith(HEAD))
            {
                var n = it.name.Substring(HEAD.Length);
                dic.Add(n, (name + it.name, findGameObjectTy(it.gameObject)));
            }
            else if(it.name.StartsWith(HEAD_ARR) && it.childCount > 0)
            {
                var n = it.name.Substring(HEAD_ARR.Length);
                arrDic.Add(n, (name + it.name, findGameObjectTy(it.GetChild(0).gameObject),it.childCount));
                continue;
            }
            FindAutoItemChildlern(it.gameObject, ref dic,ref arrDic,name,false,HEAD,HEAD_ARR);
        }
    }

    private static System.Type FindGameObjTypeDef(GameObject gameObject)
    {
        System.Type[] arr = new System.Type[] { typeof(Text),typeof(Button), typeof(Image),typeof(RawImage), typeof(Slider) };
        foreach(var t in arr)
        {
            if(gameObject.GetComponent(t) != null)
            {
                return t;
            }
        }
        return typeof(Transform);
    }

    private static System.Type FindGameObjTypeSceneView(GameObject gameObject)
    {
        System.Type[] arr = new System.Type[] { typeof(BoxCollider), typeof(Camera)};
        foreach (var t in arr)
        {
            if (gameObject.GetComponent(t) != null)
            {
                return t;
            }
        }
        return typeof(Transform);
    }

    public static void GenerateSceneViewBindReal(StringBuilder res, GameObject o)
    {
        Dictionary<string, (string, System.Type)> dic = new Dictionary<string, (string, System.Type)>();
        Dictionary<string, (string, System.Type, int)> arrDic = new Dictionary<string, (string, System.Type, int)>();
        FindAutoItemChildlern(o as GameObject, ref dic, ref arrDic, "", true, "SCV_", "SCVA_",FindGameObjTypeSceneView);

        string name = o.name;

        res.Append(string.Format(@"
public class {0}SceneViewUI : SubViewUI
{{
", name));
        AppendMembers(res, dic, arrDic);
        res.Append(string.Format(@"
public {0}SceneViewUI()
{{

}}
public override void init(Transform root)
{{
    base.init(root);
", name));
        AppendFindMembers(res, dic, arrDic);
        res.Append(@"}
}");
    }

    public static void GenerateSceneViewMgrBindReal(StringBuilder res, GameObject o)
    {
        string name = o.name;

        res.Append(string.Format(@"
public class {0}SceneViewMgr : SceneViewMgr<{0}SceneViewUI>
{{
    protected override void OnInit(){{}}
    protected override void OnConstruct() {{}}
    public override void OnEnterStage() {{}}
    public override void OnLoadScene(Dictionary<string, int> preload) {{}}
    public override void Update() {{}}
    public override void LateUpdate() {{}}
    protected override void OnDestroy() {{}}
    public override void EvtListener(bool isAdd) {{}}
    public override void MsgListener(bool isAdd) {{}}
    public override void RefreshData() {{}}
    public override void FixedUpdate() {{}}
}}
", name));
    }


    public static string GenerateSceneViewBind(bool part = true)
    {
        UnityEngine.Object[] o = Selection.GetFiltered(typeof(UnityEngine.GameObject), SelectionMode.ExcludePrefab);
        if (o != null && o.Length == 1 && o[0] is GameObject obj)
        {
            StringBuilder res = new StringBuilder();
            GenerateSceneViewBindReal(res, obj);
            if (!part)
                GenerateSceneViewMgrBindReal(res, obj);
            return res.ToString();
        }
        return "";
    }

    [MenuItem("GenCode/SceneView/OnlyUI", priority = C_MAXVALUE + 1)]
    public static void GenerateSceneUIBindPart()
    {
        var uiCode = GenerateSceneViewBind();
        SaveToClipBorad(uiCode);
    }

    [MenuItem("GenCode/SceneView/All", priority = C_MAXVALUE + 2)]
    public static void GenerateSceneUIBind()
    {
        var uiCode = GenerateSceneViewBind(false);
        SaveToClipBorad(uiCode);
    }
}

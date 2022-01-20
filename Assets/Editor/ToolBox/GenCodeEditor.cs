using UnityEngine;
using UnityEditor;
using Assets.Editor;
using libcore;
using System.Text;
using System.Collections.Generic;
using UnityEngine.UI;

public class GenCodeCxt
{
    public string tag = "IT";
    public string HEAD;
    public string HEAD_ARR;
    public string HEAD_ARREX;
    public Dictionary<string, (string, System.Type)> dic = new Dictionary<string, (string, System.Type)>();
    public Dictionary<string, (string, System.Type, int)> arrDic = new Dictionary<string, (string, System.Type, int)>();
    public Dictionary<string, (List<string>, System.Type)> arrExDic = new Dictionary<string, (List<string>, System.Type)>();

    public GenCodeCxt(string tag)
    {
        this.tag = tag;
        HEAD = string.Format("{0}_",tag);
        HEAD_ARR = string.Format("{0}A_", tag);
        HEAD_ARREX = string.Format("{0}@",tag);
    }
    public bool ParseArrEx(string n,out string name,out int idx)
    {
        name = null;
        idx = -1;
        if(n.StartsWith(HEAD_ARREX))
        {
            var tail = n.Substring(HEAD_ARREX.Length);
            string[] arr = tail.Split('_');
            if (arr.Length != 2) return false;
            if (!int.TryParse(arr[0], out idx)) return false;
            name = arr[1];
            return true;
        }
        return false;
    }
}

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
        GenCodeCxt cxt = new GenCodeCxt("SV");
        FindAutoItemChildlern(o as GameObject, ref cxt, "", true);

        string name = o.name;

        res.Append(string.Format(@"
public class {0}SubViewUI : SubViewUI
{{
", name));
        AppendMembers(res, cxt);
        res.Append(string.Format(@"
public {0}SubViewUI()
{{

}}
public override void init(Transform root)
{{
    base.init(root);
", name));
        AppendFindMembers(res, cxt);
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

    public static void AppendMembers(StringBuilder res,GenCodeCxt cxt)
    {
        foreach (var it in cxt.dic)
        {
            res.AppendLine(string.Format("public {0} {1};", it.Value.Item2.FullName, it.Key));
        }
        foreach (var it in cxt.arrDic)
        {
            res.AppendLine(string.Format("public {0}[] {1};", it.Value.Item2.FullName, it.Key));
        }
        foreach (var it in cxt.arrExDic)
        {
            res.AppendLine(string.Format("public {0}[] {1};", it.Value.Item2.FullName, it.Key));
        }
    }

    public static void AppendFindMembers(StringBuilder res, GenCodeCxt cxt,
        string self = "this")
    {
        foreach (var it in cxt.dic)
        {
            res.AppendLine(string.Format("{3}.{0} = root.Find(\"{1}\").GetComponent<{2}>();", it.Key, it.Value.Item1, it.Value.Item2.FullName,self));
        }
        foreach (var it in cxt.arrDic)
        {
            res.AppendLine(string.Format("{3}.{0} = new {1}[{2}];", it.Key, it.Value.Item2.FullName, it.Value.Item3,self));
            for (int i = 0; i < it.Value.Item3; ++i)
            {
                res.AppendLine(string.Format("{4}.{0}[{2}] = root.Find(\"{3}\").GetChild({2}).GetComponent<{1}>();",
                    it.Key, it.Value.Item2.FullName, i, it.Value.Item1,self));
            }
        }
        foreach (var it in cxt.arrExDic)
        {
            res.AppendLine(string.Format("{3}.{0} = new {1}[{2}];", it.Key, it.Value.Item2.FullName, it.Value.Item1.Count, self));
            for (int i = 0; i < it.Value.Item1.Count; ++i)
            {
                res.AppendLine(string.Format("{4}.{0}[{2}] = root.Find(\"{3}\").GetComponent<{1}>();",
                    it.Key, it.Value.Item2.FullName, i, it.Value.Item1[i], self));
            }
        }
    }


    private static void FindAutoItemChildlern(GameObject gameObj, ref GenCodeCxt cxt, string parentName = "", bool isRoot = true,
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
            if (it.name.StartsWith(cxt.HEAD))
            {
                var n = it.name.Substring(cxt.HEAD.Length);
                cxt.dic.Add(n, (name + it.name, findGameObjectTy(it.gameObject)));
            }
            else if(it.name.StartsWith(cxt.HEAD_ARR) && it.childCount > 0)
            {
                var n = it.name.Substring(cxt.HEAD_ARR.Length);
                cxt.arrDic.Add(n, (name + it.name, findGameObjectTy(it.GetChild(0).gameObject),it.childCount));
                continue;
            }else if(cxt.ParseArrEx(it.name,out string arrExName,out int arrExIdx))
            {
                if (!cxt.arrExDic.ContainsKey(arrExName))
                    cxt.arrExDic.Add(arrExName, (new List<string>(), findGameObjectTy(it.gameObject)));
                while (cxt.arrExDic[arrExName].Item1.Count <= arrExIdx)
                    cxt.arrExDic[arrExName].Item1.Add(null);
                cxt.arrExDic[arrExName].Item1[arrExIdx] = name + it.name;
                continue;
            }
            FindAutoItemChildlern(it.gameObject, ref cxt,name,false,findGameObjectTy);
        }
    }

    private static System.Type FindGameObjTypeDef(GameObject gameObject)
    {
        System.Type[] arr = new System.Type[] { typeof(Text),typeof(Button), typeof(Image),typeof(RawImage), 
            typeof(Slider),typeof(DynamicLoopScroll.LoopScrollRect),typeof(DataCarrier),typeof(Animator) };
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
        System.Type[] arr = new System.Type[] { typeof(BoxCollider),typeof(Collider), typeof(CatAniUtil), typeof(BehaviorDesigner.Runtime.BehaviorTree), typeof(Camera),typeof(SceneConfig) };
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
        GenCodeCxt cxt = new GenCodeCxt("SCV");
        FindAutoItemChildlern(o as GameObject, ref cxt, "", true,FindGameObjTypeSceneView);

        string name = o.name;

        res.Append(string.Format(@"
public class {0}SceneViewUI : SubViewUI
{{
", name));
        AppendMembers(res, cxt);
        res.Append(string.Format(@"
public {0}SceneViewUI()
{{

}}
public override void init(Transform root)
{{
    base.init(root);
", name));
        AppendFindMembers(res, cxt);
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

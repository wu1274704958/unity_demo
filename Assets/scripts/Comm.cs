using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class Wrap<T>
    where T : struct
{
    public T val;

    public Wrap(T val)
    {
        this.val = val;
    }
}
public class Pair<T, T2>
{
    public T first;
    public T2 second;

    public Pair(T first, T2 second)
    {
        this.first = first;
        this.second = second;
    }
}

public class VarCache<T> where T : class
{
    private T t = null;
    private Func<T> construct;

    public VarCache(Func<T> construct)
    {
        this.construct = construct;
    }

    public T val
    {
        get
        {
            if (t == null && construct != null)
                t = construct.Invoke();
            return t;
        }
    }

    public void clear()
    {
        t = null;
    }
}

public static class TransformExt
{
    public static void SetActiveByScale(this Transform transform, bool v)
    {
        transform.localScale = v ? Vector3.one : Vector3.zero;
    }

    public static T FindCompDeep<T>(this Transform transform,string name, bool includeInactive = false)
        where T : MonoBehaviour
    {
        T[] cs = transform.GetComponentsInChildren<T>(includeInactive);
        for(int i = 0;i < cs.Length;++i)
        {
            if(cs[i].name == name)
            {
                return cs[i];
            }
        }
        return null;
    }
}

public interface GetVal<T>
{
    T getVal();
}

public abstract class TickTick
{
    public abstract void update(float ms);
    public void update()
    {
        update(Time.deltaTime);
    }
}

public class DelayAssignment<T> : TickTick,GetVal<T>
{
    protected float time = float.NaN,curr = float.NaN;
    protected T t,delayVal;

    public T getVal()
    {
        return t;
    }

    public DelayAssignment(T t)
    {
        this.t = t;
    }

    public DelayAssignment()
    {
        this.t = default(T);
    }

    public void delaySet(T v,float ms)
    {
        delayVal = v;
        curr = 0;
        time = ms;
    }

    public void clearDelaySet()
    {
        delayVal = default(T);
        curr = time = float.NaN;
    }

    public void immediatelySet(T t)
    {
        this.t = t;
        clearDelaySet();
    }
    public void immediatelySet()
    {
        this.t = delayVal;
        clearDelaySet();
    }

    public override void update(float ms)
    {
        if (time == float.NaN || curr == float.NaN) return;
        curr += ms;
        if(curr >= time)
        {
            t = delayVal;
            clearDelaySet();
        }
    }
}

public class TickGroup : TickTick
{
    protected List<TickTick> tk;
    public TickGroup(IEnumerable<TickTick> it)
    {
        tk = new List<TickTick>(it);
    }

    public override void update(float ms)
    {
        for(int i = 0;i < tk.Count;++i)
        {
            tk[i].update();
        }
    }
}

public interface SubViewMgrInterface
{
    void OnOpen(object param);
    void Update();
    void LateUpdate();
    void Close();
    void Destroy();
    void ClearData();
    void EvtListener(bool isAdd);
    void MsgListener(bool isAdd);
    void RefreshData();
    void FixedUpdate();
}

public abstract class SubViewUI
{
    public Transform root { get; private set; } 
    public SubViewUI()
    {
    }
    public virtual void init(Transform root)
    {
        this.root = root;
    }
}

public abstract class SubViewMgr<T> : SubViewMgrInterface
    where T : SubViewUI, new()
{
    protected T ui;

    public SubViewMgr()
    {
        ui = new T();
        OnConstruct();
    }

    protected virtual void OnConstruct() { }
    protected abstract void OnInit();
    public virtual void OnOpen(object param) { }
    public virtual void Update() { }
    public virtual void LateUpdate() { }
    public virtual void Close() { }
    public virtual void Destroy() { }
    public virtual void ClearData() { }
    public virtual void EvtListener(bool isAdd) { }
    public virtual void MsgListener(bool isAdd) { }
    public virtual void RefreshData() { }
    public virtual void FixedUpdate() { }
    public void init(Transform root)
    {
        ui.init(root);
        OnInit();
    }
}

public class SubViewMgrGroup : SubViewMgrInterface
{
    protected List<SubViewMgrInterface> tk;
    public SubViewMgrGroup(IEnumerable<SubViewMgrInterface> it)
    {
        tk = new List<SubViewMgrInterface>(it);
    }

    public SubViewMgrGroup()
    {
        tk = new List<SubViewMgrInterface>();
    }

    public void Add(SubViewMgrInterface v)
    {
        tk.Add(v);
    }

    public void Remove(SubViewMgrInterface v)
    {
        tk.Remove(v);
    }

    public void Clear()
    {
        tk.Clear();
    }

    public void ClearData()
    {
        for (int i = 0; i < tk.Count; ++i)
        {
            tk[i].ClearData();
        }
    }

    public void Close()
    {
        for (int i = 0; i < tk.Count; ++i)
        {
            tk[i].Close();
        }
    }

    public void Destroy()
    {
        for (int i = 0; i < tk.Count; ++i)
        {
            tk[i].Destroy();
        }
    }

    public void EvtListener(bool isAdd)
    {
        for (int i = 0; i < tk.Count; ++i)
        {
            tk[i].EvtListener(isAdd);
        }
    }

    public void LateUpdate()
    {
        for (int i = 0; i < tk.Count; ++i)
        {
            tk[i].LateUpdate();
        }
    }

    public void MsgListener(bool isAdd)
    {
        for (int i = 0; i < tk.Count; ++i)
        {
            tk[i].MsgListener(isAdd);
        }
    }

    public void OnOpen(object param)
    {
        for (int i = 0; i < tk.Count; ++i)
        {
            tk[i].OnOpen(param);
        }
    }

    public void Update()
    {
        for (int i = 0; i < tk.Count; ++i)
        {
            tk[i].Update();
        }
    }

    public void RefreshData()
    {
        for (int i = 0; i < tk.Count; ++i)
        {
            tk[i].RefreshData();
        }
    }

    public void FixedUpdate()
    {
        for (int i = 0; i < tk.Count; ++i)
        {
            tk[i].FixedUpdate();
        }
    }
}


public static class AnimatorExt
{
    public static bool animationIsEnd(this Animator self, string name,int layer = 0)
    {
        if (self != null)
        {
            var animatorInfo = self.GetCurrentAnimatorStateInfo(layer);
            if (animatorInfo.IsName(name))
            {
                return animatorInfo.normalizedTime >= 1.0f;
            }
        }
        return false;
    }

    public static bool animationIsPlaying(this Animator self, string name, int layer = 0)
    {
        if (self != null)
        {
            var animatorInfo = self.GetCurrentAnimatorStateInfo(layer);
            if (animatorInfo.IsName(name))
            {
                return animatorInfo.normalizedTime < 1.0f;
            }
        }
        return false;
    }

    public static float GetAnimationProgress(this Animator self, int layer = 0)
    {
        if (self != null)
        {
            var animatorInfo = self.GetCurrentAnimatorStateInfo(layer);
            return animatorInfo.normalizedTime;
        }
        return 0.0f;
    }

    public static float GetAnimationProgress(this Animator self,string[] names, int layer = 0)
    {
        if (self != null)
        {
            var animatorInfo = self.GetCurrentAnimatorStateInfo(layer);
            bool k = false;
            foreach(var n in names)
            {
                if(animatorInfo.IsName(n))
                {
                    k = true;
                    break;
                }
            }
            return k ? animatorInfo.normalizedTime : 0.0f;
        }
        return 0.0f;
    }

    public static float GetAnimationProgress(this Animator self, string name, int layer = 0)
    {
        if (self != null)
        {
            var animatorInfo = self.GetCurrentAnimatorStateInfo(layer);
            if (animatorInfo.IsName(name))
            {
                return animatorInfo.normalizedTime;
            }
        }
        return 0.0f;
    }
}

public interface SceneViewMgrInterface
{
    void OnEnterStage();
    void OnLoadScene(Dictionary<string, int> preload);
    void OnStageInit(Transform root);
    void Update();
    void LateUpdate();
    void FixedUpdate();
    void Destroy();
    void EvtListener(bool isAdd);
    void MsgListener(bool isAdd);
    void RefreshData();
}

public abstract class SceneViewMgr<T> : SceneViewMgrInterface
    where T : SubViewUI, new()
{
    protected T ui;
    protected bool isInit = false;
    public SceneViewMgr()
    {
        ui = new T();
        OnConstruct();
    }

    protected virtual void OnConstruct() { }
    protected abstract void OnInit();
    
    public void init(Transform root)
    {
        if (ui == null) ui = new T();
        ui.init(root);
        EvtListener(true);
        MsgListener(true);
        OnInit();
        isInit = true;
    }
    public virtual void OnEnterStage() { }
    public virtual void OnLoadScene(Dictionary<string, int> preload) { }
    public virtual void OnStageInit(Transform root)
    {
        init(root);
    }
    public virtual void Update() { }
    public virtual void LateUpdate() { }
    public virtual void FixedUpdate() { }
    public virtual void Destroy() {
        EvtListener(false);
        MsgListener(false);
        OnDestroy();
        isInit = false;
        ui = null;
    }

    protected abstract void OnDestroy();
    public virtual void EvtListener(bool isAdd) { }
    public virtual void MsgListener(bool isAdd) { }
    public virtual void RefreshData() { }
}

public class SceneViewMgrGroup : SceneViewMgrInterface
{
    protected List<SceneViewMgrInterface> tk;
    public SceneViewMgrGroup(IEnumerable<SceneViewMgrInterface> it)
    {
        tk = new List<SceneViewMgrInterface>(it);
    }

    public SceneViewMgrGroup()
    {
        tk = new List<SceneViewMgrInterface>();
    }

    public void Add(SceneViewMgrInterface v)
    {
        tk.Add(v);
    }

    public void Remove(SceneViewMgrInterface v)
    {
        tk.Remove(v);
    }

    public void Clear()
    {
        tk.Clear();
    }

    public void OnEnterStage()
    {
        foreach (var it in tk)
            it.OnEnterStage();
    }

    public void OnLoadScene(Dictionary<string, int> preload)
    {
        foreach (var it in tk)
            it.OnLoadScene(preload);
    }

    public void OnStageInit(Transform root)
    {
        foreach (var it in tk)
            it.OnStageInit(root);
    }

    public void Update()
    {
        foreach (var it in tk)
            it.Update();
    }

    public void LateUpdate()
    {
        foreach (var it in tk)
            it.LateUpdate();
    }

    public void FixedUpdate()
    {
        foreach (var it in tk)
            it.FixedUpdate();
    }

    public void Destroy()
    {
        foreach (var it in tk)
            it.Destroy();
    }

    public void EvtListener(bool isAdd)
    {
        foreach (var it in tk)
            it.EvtListener(isAdd);
    }

    public void MsgListener(bool isAdd)
    {
        foreach (var it in tk)
            it.MsgListener(isAdd);
    }

    public void RefreshData()
    {
        foreach (var it in tk)
            it.RefreshData();
    }
}

public class ColliderClick
{
    protected int layerMask;
    protected float VaildDist = 100.0f;
    protected float sqrMouseMoveVaildDist = 2.0f;
    protected Vector2 downPos;
    protected HashSet<Collider> colliders = new HashSet<Collider>();
    public Action<Collider> OnClick;
    protected Camera cam;
    public Camera camera { set
        {
            cam = value;
        } 
    }

    public ColliderClick(int layerMask, float VaildDist = 100.0f,Camera cam = null)
    {
        this.layerMask = layerMask;
        this.VaildDist = VaildDist;
        this.cam = cam;
    }

    public bool Add(Collider c)
    {
        if (colliders.Contains(c)) return false;
        colliders.Add(c);
        return true;
    }

    public void Remove(Collider c)
    {
        colliders.Remove(c);
        return;
    }
    protected void Clicked(Vector3 pos)
    {
        //if (cam == null) cam = CameraMgr.Instance.GetCamera(ECamType.MainCam);
        if (cam == null) return;
        var ray = cam.ScreenPointToRay(pos);
        RaycastHit[] res = Physics.RaycastAll(ray, VaildDist, layerMask);
        float dist = float.MaxValue;
        Collider coll = null;
        foreach (var it in res)
        {
            if (colliders.Contains(it.collider) && it.distance < dist)
            {
                coll = it.collider;
                dist = it.distance;
            }
        }
        if (coll != null)
            OnClick?.Invoke(coll);
    }

    public void update()
    {
#if  !UNITY_EDITOR
        if (Input.touchCount > 1 || Input.touchCount <= 0) return;
        
        var touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Stationary)
        {
            Clicked(touch.position);
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            downPos = Input.mousePosition;
            //Debug.LogWarning(" " + downPos);
        }

        if (Input.GetMouseButtonUp(0))
        {
            Vector2 upPos = Input.mousePosition;
            //Debug.LogWarning(upPos + " " + downPos +"  " + (upPos - downPos).sqrMagnitude);
            if ((upPos - downPos).sqrMagnitude > sqrMouseMoveVaildDist) return;
            Clicked(upPos);
        }
#endif
    }

    internal void Clear()
    {
        colliders.Clear();
    }
}

public static class Serialize
{
    public static T Deserialize<T>(string s)
    {
        var o = Deserialize(s, typeof(T));
        if (o == null) return default(T);
        return (T)o;
    }
    public static object Deserialize(string s, System.Type t)
    {
        if (t == typeof(string))
        {
            return s;
        }
        if (t == typeof(int))
        {
            if (int.TryParse(s, out var v))
                return v;
        }
        if (t == typeof(long))
        {
            if (long.TryParse(s, out var v))
                return v;
        }
        if (t == typeof(float))
        {
            if (float.TryParse(s, out var v))
                return v;
        }
        if (t == typeof(double))
        {
            if (double.TryParse(s, out var v))
                return v;
        }
        if (t == typeof(Vector2))
        {
            string[] arr = s.Split(',');
            if (arr.Length >= 2)
            {
                Vector2 v = new Vector2(Deserialize<float>(arr[0]), Deserialize<float>(arr[1]));
                return v;
            }
        }
        if (t == typeof(Vector3))
        {
            string[] arr = s.Split(',');
            if (arr.Length >= 3)
            {
                Vector3 v = new Vector3(Deserialize<float>(arr[0]), Deserialize<float>(arr[1]), Deserialize<float>(arr[2]));
                return v;
            }
        }
        if (t == typeof(Vector4))
        {
            string[] arr = s.Split(',');
            if (arr.Length >= 4)
            {
                Vector4 v = new Vector4(Deserialize<float>(arr[0]), Deserialize<float>(arr[1]), Deserialize<float>(arr[2]), Deserialize<float>(arr[3]));
                return v;
            }
        }
        if (t == typeof(AnyArray))
        {
            try
            {
                AnyArray array = new AnyArray(s);
                return array;
            }
            catch (Exception e) { }
        }
        return null;
    }

}

public static class AnyArrayTyMap
{
    public static readonly Dictionary<string, System.Type> TypeMap = new KeyValuePair<string, System.Type>[]{
        new KeyValuePair<string, System.Type>("i",typeof(int)),
        new KeyValuePair<string, System.Type>("l",typeof(long)),
        new KeyValuePair<string, System.Type>("f",typeof(float)),
        new KeyValuePair<string, System.Type>("lf",typeof(double)),
        new KeyValuePair<string, System.Type>("s",typeof(string)),
        new KeyValuePair<string, System.Type>("vec2",typeof(Vector2)),
        new KeyValuePair<string, System.Type>("vec3",typeof(Vector3)),
        new KeyValuePair<string, System.Type>("vec4",typeof(Vector4))
    }.ToDictionary();
}

public class AnyArray
{
    protected List<object> objs;
    protected List<System.Type> tyArr;
    
    public AnyArray(string str)
    {
        if(parse(str,out var objs,out var tys))
        {
            this.objs = objs;
            this.tyArr = tys;
        }
        else
        {
            throw new Exception("Parse AnyArray Failed!!!");
        }
    }

    public bool good_idx(int idx)
    {
        return idx >= 0 && idx < objs.Count;
    }
    public bool TypeEq<T>(int idx)
    {
        if(good_idx(idx))
        {
            return typeof(T) == tyArr[idx];
        }
        return false;
    }

    public T Get<T>(int idx)
    {
        if (TypeEq<T>(idx))
        {
            return (T)objs[idx];
        }
        return default(T);
    }

    public bool TryGet<T>(int idx,out T v)
    {
        v = default(T);
        if (TypeEq<T>(idx))
        {
            v = (T)objs[idx];
            return true;
        }
        return false;
    }

    public object[] GetValues()
    {
        return objs.ToArray();
    }

    public bool parse(string str,out List<object> objs,out List<System.Type> tys)
    {
        var cs = str.Trim().ToCharArray();
        int step = 0;
        System.Type currTy = null;
        objs = new List<object>();
        tys = new List<Type>();
        for(int i = 0;i < cs.Length;++i)
        {
            var it = cs[i];
            if (step == 0 && it == '[')
            {
                ++step;continue;
            }
            if(step == 1 && it == ']')
            {
                if (i + 1 == cs.Length)
                {
                    step = 100;break;
                }
            }
            if(step == 1)
            {
                if (GetType(cs, i, out var ty, out var new_it))
                {
                    i = new_it; currTy = ty; step += 1;
                    continue;
                }
                else return false;
            }
            if (step == 2)
            {
                if (GetVal(cs, i, currTy, out var obj, out var nit))
                {
                    i = nit; step = 1;
                    objs.Add(obj);
                    tys.Add(currTy);
                    if (cs[i] == ']')
                    {
                        step = 100;
                        break;
                    }
                    else continue;
                }
                else return false;
            }
        }
        return step == 100;
    }

    private bool GetType(char[] arr,int it,out System.Type ty,out int new_it)
    {
        ty = null;new_it = it;
        var step = 0;
        StringBuilder sb = new StringBuilder();
        for (int i = it; i < arr.Length; ++i)
        {
            var c = arr[i];
            if (step == 0)
            {
                if (c != ' ')
                {
                    sb.Append(c);
                    step += 1;
                }
                continue;
            }
            if(step == 1)
            {
                if (c != ':')
                    sb.Append(c);
                else
                {
                    step += 1;
                    new_it = i;
                    ty = GetTypeByTag(sb.ToString().TrimEnd());
                    break;
                }
            }
        }
        return step == 2 && ty != null;
    }

    private bool GetVal(char[] arr, int it,System.Type ty, out object val, out int new_it)
    {
        val = null; new_it = it;
        var step = 0;
        StringBuilder sb = new StringBuilder();
        for (int i = it; i < arr.Length; ++i)
        {
            var c = arr[i];
            if (step == 0)
            {
                if (c != ' ')
                {
                    sb.Append(c);
                    step += 1;
                }
                continue;
            }
            if (step == 1)
            {
                if (c != ';' && c != ']')
                    sb.Append(c);
                else
                {
                    step += 1;
                    new_it = i;
                    val = Serialize.Deserialize(sb.ToString().TrimEnd(),ty );
                    break;
                }
            }
        }
        return step == 2 && val != null;
    }

    private System.Type GetTypeByTag(string res)
    {
        if(AnyArrayTyMap.TypeMap.TryGetValue(res,out var s))
        {
            return s;
        }
        return null;
    }

}

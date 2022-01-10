using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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




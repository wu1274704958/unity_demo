
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
[Serializable]
public class Wrap<T>
    where T : struct
{
    public T val;

    public Wrap(T val)
    {
        this.val = val;
    }

    public Wrap()
    {
        this.val = default(T);
    }

    public override bool Equals(object obj)
    {
        if (obj is Wrap<T> oth)
            return val.Equals(oth.val);
        return false;
    }

    public override int GetHashCode()
    {
        return val.GetHashCode();
    }
}

[Serializable]
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

    public static T FindCompDeep<T>(this Transform transform, string name, bool includeInactive = false)
        where T : MonoBehaviour
    {
        T[] cs = transform.GetComponentsInChildren<T>(includeInactive);
        for (int i = 0; i < cs.Length; ++i)
        {
            if (cs[i].name == name)
            {
                return cs[i];
            }
        }
        return null;
    }

    public static void DestroyAllChildren(this Transform root)
    {
        while (root.childCount > 0)
        {
            UnityEngine.Object.DestroyImmediate(root.GetChild(0).gameObject);
        }
    }

    public static GameObject cloneAdd(this Transform root, GameObject prefab, bool active = true, bool defScale = true, bool defRotate = true, bool defPos = true)
    {
        var obj = GameObject.Instantiate(prefab, root);
        obj.SetActive(active);
        if (defPos) obj.transform.localPosition = Vector3.zero;
        if (defScale) obj.transform.localScale = Vector3.one;
        if (defRotate) obj.transform.localRotation = Quaternion.identity;
        return obj;
    }
}


public interface GetVal<T>
{
    T getVal();
}

public interface TickTag { }
public class NoTag : TickTag { }

public class TickOp
{
    public static TickOp operator +(TickOp a, TickOp b)
    {
        if (a == null) return b;
        if (b == null) return a;
        if (a is MlutTickOp arr && b is MlutTickOp arr2)
        {
            var old = arr.ops;
            arr.ops = new TickOp[arr.ops.Length + arr2.ops.Length];
            old.CopyTo(arr.ops, 0);
            arr2.ops.CopyTo(arr.ops, old.Length);
        }
        else if (a is MlutTickOp arr3)
        {
            var old = arr3.ops;
            arr3.ops = new TickOp[old.Length + 1];
            old.CopyTo(arr3.ops, 0);
            arr3.ops[old.Length] = b;
        }
        else if (b is MlutTickOp arr4)
        {
            var old = arr4.ops;
            arr4.ops = new TickOp[old.Length + 1];
            old.CopyTo(arr4.ops, 0);
            arr4.ops[old.Length] = a;
            return b;
        }
        else
        {
            var arr5 = new TickOp[2];
            arr5[0] = a;
            arr5[1] = b;
            return new MlutTickOp() { ops = arr5 };
        }
        return a;
    }

    public static void Test()
    {
        TickOp a = null;
        var b = a + new TickRmOp();
        var c = b + new TickClearOp();
        var d = c + new TickRmSelfOp();
        var arr = new MlutTickOp() { ops = new TickOp[] { new DelayTaskTickOp(), new TickAddNextOp() } };
        var e = arr + d;
        var f = new TickReplaceOp() + e;
    }
}
public class TickRmSelfOp : TickOp { }
public class TickRmOp : TickOp
{
    public int key;
}
public class TickClearOp : TickOp { }
public class TickAddNextOp : TickOp
{
    public TickTick next;
}
public class TickReplaceOp : TickOp
{
    public TickTick next;
}
public class MlutTickOp : TickOp
{
    public TickOp[] ops;
}
public class DelayTaskTickOp : TickOp
{
    public Func<TickOp> action;
    public TickTag tickTag;
}


public abstract class TickTick
{
    protected DateTime lastTime;
    protected TickTag tag = null;
    public TickTick()
    {
        lastTime = DateTime.Now;
    }
    public abstract TickOp update(TimeSpan t);
    public void update()
    {
        var now = DateTime.Now;
        update(now - lastTime);
        lastTime = now;
    }
    public virtual void OnAdd(TickGroup tg)
    {

    }

    public virtual void OnRemove()
    {

    }
    public virtual void Reset()
    {
        lastTime = DateTime.Now;
    }
    public TickTick SetTag<T>()
        where T : TickTag, new()
    {
        tag = new T();
        return this;
    }
    public TickTick SetTag(TickTag tag)
    {
        this.tag = tag;
        return this;
    }
    public bool IsTag<T>()
        where T : TickTag
    {
        return tag != null && tag is T;
    }
    public bool IsTag(TickTag tag)
    {
        return this.tag != null && this.tag.GetType() == tag.GetType();
    }
    public TickTag GetTag()
    {
        return tag;
    }
}

public class ExecOpTick : TickTick
{
    public TickOp op;
    public override TickOp update(TimeSpan t)
    {
        var res = new List<TickOp>();
        if (op != null)
            res.Add(op);
        res.Add(new TickRmSelfOp());
        return new MlutTickOp() { ops = res.ToArray() };
    }
}

public class DelayLoopTask : TickTick
{
    protected Action action;
    public Action<TimeSpan> UpdateAction; 
    protected TimeSpan time = TimeSpan.Zero, curr = TimeSpan.Zero;

    public DelayLoopTask(Action action, TimeSpan time = default)
    {
        this.action = action;
        this.time = time;
    }

    public override TickOp update(TimeSpan ms)
    {
        UpdateAction?.Invoke(ms);
        curr += ms;
        if (curr >= time)
        {
            return OnTrigger();
        }
        return null;
    }

    protected virtual TickOp OnTrigger()
    {
        action?.Invoke();
        curr -= time;
        return null;
    }

    public TimeSpan Time
    {
        set
        {
            time = value;
            curr = TimeSpan.Zero;
        }
        get => time;
    }
}
public class DelayTaskTag : TickTag { }
public class DelayTask : TickTick
{
    protected Func<TickOp> actionWithOp;
    private Action m_Action;
    private Action<string> m_ActionWithStr;
    private TimeSpan m_DelayTime = TimeSpan.Zero;
    private TimeSpan m_Curr = TimeSpan.Zero;
    protected int TriggerCount = 0;
    public bool DelayRunAction = false;
    private string m_Para;
    public DelayTask(Action action, TimeSpan delay, int TriggerCount)
    {
        tag = new DelayTaskTag();
        m_Action = action;
        m_DelayTime = delay;
        this.TriggerCount = TriggerCount;
    }
    public DelayTask(Action<string> action,string para, TimeSpan delay, int TriggerCount)
    {
        m_Para = para;
        tag = new DelayTaskTag();
        m_ActionWithStr = action;
        m_DelayTime = delay;
        this.TriggerCount = TriggerCount;
    }
    public DelayTask(Func<TickOp> action, TimeSpan time, int triggerCount)
    {
        tag = new DelayTaskTag();
        actionWithOp = action;
        m_DelayTime = time;
        TriggerCount = triggerCount;
    }

    public override TickOp update(TimeSpan ms)
    {
        m_Curr += ms;
        if (m_Curr >= m_DelayTime)
        {
            return OnTrigger();
        }
        return null;
    }

    public DelayTask SetDelayRunAction(bool v)
    {
        DelayRunAction = v;
        return this;
    }

    protected virtual TickOp OnTrigger()
    {
        List<TickOp> ops = new List<TickOp>();
        if (DelayRunAction)
            ops.Add(new DelayTaskTickOp() { action = RunAction, tickTag = tag });
        else
        {
            var op = RunAction();
            if (op != null) ops.Add(op);
        }
        m_Curr = TimeSpan.Zero;
        --TriggerCount;
        if (TriggerCount <= 0) ops.Add(new TickRmSelfOp());
        return new MlutTickOp() { ops = ops.ToArray() };
    }

    private TickOp RunAction()
    {
        if (actionWithOp != null)
            return actionWithOp.Invoke();
        if (m_ActionWithStr != null)
        {
            m_ActionWithStr.Invoke(m_Para);
            return null;
        }
        m_Action?.Invoke();
        return null;
    }

    public void ForceTrigger()
    {
        m_Curr = m_DelayTime;
    }
}

public class DelayFrameTaskTag : TickTag { }
public class DelayFrameTask : TickTick
{
    protected Func<TickOp> actionWithOp;
    protected Action action;
    protected int time = 0, curr = 0;
    protected int triggerCount = 0;
    public bool DelayRunAction = false;
    public DelayFrameTask(Action action, int frame, int TriggerCount)
    {
        this.tag = new DelayFrameTaskTag();
        this.action = action;
        this.time = frame;
        this.triggerCount = TriggerCount;
    }
    public DelayFrameTask(Func<TickOp> action, int frame, int TriggerCount)
    {
        this.tag = new DelayFrameTaskTag();
        this.actionWithOp = action;
        this.time = frame;
        this.triggerCount = TriggerCount;
    }

    public override TickOp update(TimeSpan ms)
    {
        curr += 1;
        if (curr >= time)
        {
            return OnTrigger();
        }
        return null;
    }

    public DelayFrameTask SetDelayRunAction(bool v)
    {
        DelayRunAction = v;
        return this;
    }

    protected virtual TickOp OnTrigger()
    {
        List<TickOp> ops = new List<TickOp>();
        if (DelayRunAction)
            ops.Add(new DelayTaskTickOp() { action = RunAction, tickTag = tag });
        else
        {
            var op = RunAction();
            if (op != null) ops.Add(op);
        }
        curr = 0;
        --triggerCount;
        if (triggerCount <= 0) ops.Add(new TickRmSelfOp());
        return new MlutTickOp() { ops = ops.ToArray() };
    }

    private TickOp RunAction()
    {
        if (actionWithOp != null)
            return actionWithOp.Invoke();
        action?.Invoke();
        return null;
    }

    public void ForceTrigger()
    {
        curr = time;
    }
}

public class UselessTask : DelayTask
{
    public UselessTask(TimeSpan time) : base(null, time, 1)
    {

    }
    public UselessTask() : base(null, TimeSpan.FromSeconds(2), 1)
    {

    }
}

public class DelayLoopTaskdouble : DelayLoopTask
{
    protected bool FirstLevelTrigger = false;
    protected int SecondLevelTriggerTimes = 1;
    protected TimeSpan SecondLevelTriggerDur = TimeSpan.FromSeconds(1);
    public DelayLoopTaskdouble(Action action, TimeSpan time,
        int SecondLevelTriggerTimes,
        TimeSpan SecondLevelTriggerDur) : base(action, time)
    {
        this.SecondLevelTriggerDur = SecondLevelTriggerDur;
        this.SecondLevelTriggerTimes = SecondLevelTriggerTimes;
    }

    public override TickOp update(TimeSpan ms)
    {
        return base.update(ms);
    }

    protected override TickOp OnTrigger()
    {
        base.OnTrigger();
        FirstLevelTrigger = true;
        return new TickAddNextOp()
        {
            next = new DelayTask(() =>
            {
                action?.Invoke();
                FirstLevelTrigger = false;
                return null;
            }, SecondLevelTriggerDur, SecondLevelTriggerTimes)
        };
    }
}

public class DelayAssignment<T> : TickTick, GetVal<T>
{
    protected TimeSpan time = TimeSpan.Zero;
    protected bool isDelaySet = false;
    protected T t, delayVal;

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

    public void delaySet(T v, TimeSpan time)
    {
        delayVal = v;
        this.time = time;
        isDelaySet = true;
    }

    public void clearDelaySet()
    {
        delayVal = default(T);
        this.time = TimeSpan.Zero;
        isDelaySet = false;
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

    public override TickOp update(TimeSpan ms)
    {
        if (!isDelaySet) return null;
        time -= ms;
        if (time < TimeSpan.Zero)
        {
            t = delayVal;
            clearDelaySet();
        }
        return null;
    }
}
public class AddTickTag<T> : TickTag where T : TickTag { }
public class RemoveTickTag<T> : TickTag where T : TickTag { }
public class ClearAllTickTag : TickTag { }
public class TickGroup : TickTick
{
    protected Dictionary<int, TickTick> tk;
    public bool Updating { get; private set; } = false;
    protected List<(Func<TickOp>, TickTag)> DelayActions = new List<(Func<TickOp>, TickTag)>();
    private List<int> m_TmpDel = new List<int>();
    private List<TickTick> m_TmpAdd = new List<TickTick>();
    public int Count => tk.Count;
    public int TaskCount => DelayActions.Count;
    public float Speed = 1.0f;
    public string GetTaskStr()
    {
        var res = new StringBuilder();
        foreach (var t in tk)
        {
            res.Append($"ID:{t.Key} Type:{t.Value.GetType().Name} Tag:{t.Value.GetTag()?.GetType()?.Name}\n");
        }
        return res.ToString();
    }
    public TickGroup(IEnumerable<TickTick> it)
    {
        tk = new Dictionary<int, TickTick>();
        foreach (var i in it)
        {
            tk.Add(i.GetHashCode(), i);
        }
    }

    public TickGroup()
    {
        tk = new Dictionary<int, TickTick>();
    }

    public override TickOp update(TimeSpan ms)
    {
        Updating = true;
        var doClear = false;
        m_TmpAdd.Clear();
        m_TmpDel.Clear();
        var dur = TimeSpan.FromMilliseconds(ms.TotalMilliseconds * Speed);
        foreach (var it in tk)
        {
            var op = it.Value.update(dur);
            if (op == null) continue;
            ExecOp(it, op, ref m_TmpDel, ref m_TmpAdd, ref doClear);
        }
        foreach (var it in m_TmpDel)
            Rmtt(it);
        foreach (var it in m_TmpAdd)
            Addtt(it);
        RunDelayAction(ref m_TmpDel, ref m_TmpAdd, ref doClear);
        if (doClear)
            ClearEx();
        Updating = false;
        return null;
    }
    protected void ExecOp(KeyValuePair<int, TickTick> it, TickOp op, ref List<int> tmpDel, ref List<TickTick> tmpAdd, ref bool doClear)
    {
        if (op == null) return;
        switch (op)
        {
            case TickRmSelfOp rmSelf:
                {
                    if (it.Value != null)
                        tmpDel.Add(it.Key);
                }
                break;
            case TickRmOp rm:
                {
                    tmpDel.Add(rm.key);
                }
                break;
            case TickClearOp clear:
                {
                    doClear = true;
                }
                break;
            case TickAddNextOp add:
                {
                    tmpAdd.Add(add.next);
                }
                break;
            case TickReplaceOp replace:
                {
                    if (it.Value != null)
                        tmpDel.Add(it.Key);
                    tmpAdd.Add(replace.next);
                }
                break;
            case MlutTickOp mlut:
                {
                    foreach (var o in mlut.ops)
                        ExecOp(it, o, ref tmpDel, ref tmpAdd, ref doClear);
                }
                break;
            case DelayTaskTickOp delayTask:
                {
                    addDelayAction(delayTask.action, delayTask.tickTag);
                }
                break;
        }
    }
    protected void Addtt(TickTick t)
    {
        if (t == null) return;
        tk.Add(t.GetHashCode(), t);
        t.OnAdd(this);
    }
    protected void Rmtt(int k)
    {
        if (tk.TryGetValue(k, out var tt))
        {
            tk.Remove(k);
            tt.OnRemove();
        }
    }
    private void RunDelayAction(ref List<int> tmpDel, ref List<TickTick> tmpAdd, ref bool doClear)
    {
        tmpDel.Clear(); tmpAdd.Clear();
        for (var i = 0; i < DelayActions.Count; ++i)
        {
            try
            {
                var op = DelayActions[i].Item1?.Invoke();
                if (op != null) ExecOp(new KeyValuePair<int, TickTick>(-1, null), op, ref tmpDel, ref tmpAdd, ref doClear);
            }
            catch (Exception e)
            {
                Debug.LogError($"TickGroup DelayActions Err {e}");
            }
        }
        DelayActions.Clear();
        foreach (var it in tmpDel)
            Rmtt(it);
        foreach (var it in tmpAdd)
            Addtt(it);
    }
    public bool Contain(TickTick t)
    {
        return tk.ContainsKey(t.GetHashCode());
    }
    public void Add(TickTick t)
    {
        var tag = t.GetTag();
        if (tag == null)
        {
            tag = new AddTickTag<NoTag>();
        }
        else
        {
            var T = tag.GetType();
            var ty = typeof(AddTickTag<>);
            ty = ty.MakeGenericType(T);
            tag = (TickTag)System.Activator.CreateInstance(ty);
        }
        DelayActions.Add((() =>
        {
            return new TickAddNextOp() { next = t };
        }
        , tag));
    }

    public void AddImmediate(TickTick t)
    {
        if (!Contain(t))
        {
            tk.Add(t.GetHashCode(), t);
            t.OnAdd(this);
        }
    }
    public void RemoveImmediate(TickTick t)
    {
        if (Contain(t))
        {
            t.OnRemove();
            tk.Remove(t.GetHashCode());
        }
    }

    public void Remove(TickTick t)
    {
        var tag = t.GetTag();
        if (tag == null)
        {
            tag = new RemoveTickTag<NoTag>();
        }
        else
        {
            var T = tag.GetType();
            var ty = typeof(RemoveTickTag<>);
            ty = ty.MakeGenericType(T);
            tag = (TickTag)System.Activator.CreateInstance(ty);
        }
        DelayActions.Add((() =>
        {
            return new TickRmOp() { key = t.GetHashCode() };
        }
        , tag));
    }

    public void Clear()
    {
        DelayActions.Add((() =>
        {
            return new TickClearOp();
        }
        , new ClearAllTickTag()));
    }

    public void ClearEx()
    {
        foreach (var it in tk)
        {
            it.Value.OnRemove();
        }
        tk.Clear();
        DelayActions.Clear();
    }

    public void ClearTick()
    {
        foreach (var it in tk)
        {
            it.Value.OnRemove();
        }
        tk.Clear();
    }

    public bool IsClean()
    {
        return tk.Count == 0 && DelayActions.Count == 0;
    }

    public bool IsEmpty()
    {
        return tk.Count == 0;
    }

    public void Foreach(Action<TickTick, int> on)
    {
        int i = 0;
        foreach (var a in tk)
        {
            on?.Invoke(a.Value, i++);
        }
    }

    public void addDelayAction(Func<TickOp> action, TickTag tag = null)
    {
        if (action != null)
            DelayActions.Add((action, tag));
    }

    public void ClearImmediateByFunc(Func<TickTag, bool> f, bool incluedeDelayAction = true)
    {
        if (f == null) return;
        var rmLs = new List<int>();
        foreach (var it in tk)
        {
            if (f(it.Value.GetTag()))
            {
                rmLs.Add(it.Key);
                it.Value.OnRemove();
            }
        }
        foreach (var i in rmLs)
        {
            tk.Remove(i);
        }
        if (incluedeDelayAction)
        {
            for (int i = DelayActions.Count - 1; i >= 0; --i)
            {
                var it = DelayActions[i];
                if (f(it.Item2))
                {
                    DelayActions.RemoveAt(i);
                }
            }
        }
    }

    public void ClearImmediateByTag<T>(bool incluedeDelayAction = true)
        where T : TickTag
    {
        ClearImmediateByFunc((t) =>
        {
            return t is T;
        }, incluedeDelayAction);
    }

    public void ClearImmediateByTag(TickTag T, bool incluedeDelayAction = true)
    {
        ClearImmediateByFunc((t) =>
        {
            return t != null && t.GetType() == T.GetType();
        }, incluedeDelayAction);
    }

    public void ClearImmediateByTags(HashSet<Type> types, bool incluedeDelayAction = true)
    {
        ClearImmediateByFunc((t) =>
        {
            return types.Contains(t.GetType());
        }, incluedeDelayAction);
    }

    public void ClearByTag<T>(bool incluedeDelayAction = true)
        where T : TickTag
    {
        addDelayAction(() =>
        {
            ClearImmediateByTag<T>(incluedeDelayAction);
            return null;
        });
    }

    public void ClearByTag(TickTag tag, bool incluedeDelayAction = true)
    {
        addDelayAction(() =>
        {
            ClearImmediateByTag(tag, incluedeDelayAction);
            return null;
        });
    }

    public bool HasTag<T>()
        where T : TickTag
    {
        foreach (var it in tk)
        {
            if (it.Value.IsTag<T>())
            {
                return true;
            }
        }
        return false;
    }

    public bool HasTag(TickTag tag)
    {
        foreach (var it in tk)
        {
            if (it.Value.IsTag(tag))
            {
                return true;
            }
        }
        return false;
    }
    public bool HasTags(params Type[] tags)
    {
        foreach (var it in tk)
        {
            if (it.Value.GetTag() != null && tags.Contains(it.Value.GetTag().GetType()))
            {
                return true;
            }
        }
        return false;
    }
    public void CloseUIAndClearAll()
    {
        if (!Updating)
        {
            addDelayAction(() => new TickClearOp());
            update();
        }
    }
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

public static class AnimatorExt
{
    public static bool animationIsEnd(this Animator self, string name, int layer = 0, bool notCurrIsok = false, bool reverse = false)
    {
        if (self != null)
        {
            var animatorInfo = self.GetCurrentAnimatorStateInfo(layer);
            if (animatorInfo.IsName(name))
            {
                return !reverse ? animatorInfo.normalizedTime >= 0.99f : animatorInfo.normalizedTime <= 0.01f;
            }
            else if (notCurrIsok)
            {
                return true;
            }
        }
        return false;
    }

    public static bool animationIsEndEx(this Animator self, string name, int layer = 0, bool notCurrIsok = false, bool reverse = false)
    {
        if (self != null)
        {
            var n = self.EnumAllAni((a) => a.name.Contains(name)).FirstOrDefault();
            if (n == null) return false;
            return self.animationIsEnd(n, layer, notCurrIsok, reverse);
        }
        return false;
    }

    public static bool animationIsEnd(this Animator self, int layer = 0, bool reverse = false)
    {
        if (self != null)
        {
            var animatorInfo = self.GetCurrentAnimatorStateInfo(layer);
            return !reverse ? animatorInfo.normalizedTime >= 0.99f : animatorInfo.normalizedTime <= 0.01f;
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
                return animatorInfo.normalizedTime < 1.0f && self.speed > 0.0f;
            }
        }
        return false;
    }

    public static bool animationIsPlayingEx(this Animator self, string name, int layer = 0)
    {
        if (self != null)
        {
            var n = self.EnumAllAni((a) => a.name.Contains(name)).FirstOrDefault();
            if (n == null) return false;
            return self.animationIsPlaying(n, layer);
        }
        return false;
    }

    public static bool animationIsPlaying(this Animator self, int layer = 0)
    {
        if (self != null)
        {
            var animatorInfo = self.GetCurrentAnimatorStateInfo(layer);
            {
                return animatorInfo.normalizedTime < 1.0f && self.speed > 0.0f;
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

    public static float GetAnimationDurationTime(this Animator self, int layer = 0)
    {
        if (self != null)
        {
            var animatorInfo = self.GetCurrentAnimatorStateInfo(layer);
            return animatorInfo.normalizedTime * animatorInfo.length;
        }
        return 0.0f;
    }

    public static float GetAnimationProgress(this Animator self, string[] names, int layer = 0)
    {
        if (self != null)
        {
            var animatorInfo = self.GetCurrentAnimatorStateInfo(layer);
            bool k = false;
            foreach (var n in names)
            {
                if (animatorInfo.IsName(n))
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
    public static void SetCurrentAnimationProgress(this Animator self, float p, int layer = 0, float speed = 0.0f)
    {
        if (self != null)
        {
            var animatorInfo = self.GetCurrentAnimatorStateInfo(layer);
            self.speed = speed;
            self.Play(animatorInfo.shortNameHash, layer, p);
        }
    }
    public static void SetCurrentAnimationDuration(this Animator self, float p, int layer = 0, float speed = 0.0f)
    {
        if (self != null)
        {
            var animatorInfo = self.GetCurrentAnimatorStateInfo(layer);
            p = p / animatorInfo.length;
            self.speed = speed;
            self.Play(animatorInfo.shortNameHash, layer, p);
        }
    }

    public static bool AppendCurrentAnimationProgress(this Animator self, float off, int layer = 0)
    {
        if (self != null)
        {
            var animatorInfo = self.GetCurrentAnimatorStateInfo(layer);
            self.speed = 0.0f;
            self.Play(animatorInfo.shortNameHash, layer, animatorInfo.normalizedTime + off);
            return true;
        }
        return false;
    }

    public static string[] EnumAllAni(this Animator self, Func<AnimationClip, bool> f)
    {
        List<string> res = new List<string>();
        if (self.runtimeAnimatorController == null || self.runtimeAnimatorController.animationClips == null) return res.ToArray();
        foreach (var c in self.runtimeAnimatorController.animationClips)
        {
            if ((f?.Invoke(c)).GetValueOrDefault())
                res.Add(c.name);
        }
        return res.ToArray();
    }

    public static bool PlayAniFixed(this Animator self, string name, float p, int layer = 0)
    {
        var aniName = self.EnumAllAni((clip) =>
        {
            return clip.name.Contains(name);
        });
        if (aniName.Length > 0)
        {
            self.Play(aniName[0], layer, p);
            self.speed = 0;
        }
        return aniName.Length > 0;
    }
    public static bool PlayAni(this Animator self, string name, float p, int layer = 0)
    {
        var aniName = self.EnumAllAni((clip) =>
        {
            return clip.name.Contains(name);
        });
        if (aniName.Length > 0)
        {
            self.Play(aniName[0], layer, p);
        }
        return aniName.Length > 0;
    }
}

public abstract class SceneViewMgrInterface
{
    public bool IsInit { get; protected set; }
    public bool IsOpen { get; protected set; }
    public virtual void OnEnterStage()
    {
        IsOpen = true;
    }
    public abstract void OnLoadScene(Dictionary<string, int> preload);
    public virtual void OnStageInit(Transform root)
    {
        IsInit = true;
    }
    public abstract void Update();
    public abstract void LateUpdate();
    public abstract void FixedUpdate();
    public virtual void Destroy()
    {
        IsInit = false;
    }
    public virtual void OnExitStage()
    {
        IsOpen = false;
    }
    public abstract void EvtListener(bool isAdd);
    public abstract void MsgListener(bool isAdd);
    public abstract void RefreshData();
}

public class SceneViewMgrGroup : SceneViewMgrInterface
{
    public enum Attribute : uint
    {
        Normal = 0,
        Manual = 1,
    }
    protected List<SceneViewMgrInterface> tk;
    protected Dictionary<int, Attribute> SceneAttrs;
    public SceneViewMgrGroup(IEnumerable<SceneViewMgrInterface> it)
    {
        tk = new List<SceneViewMgrInterface>(it);
        SceneAttrs = new Dictionary<int, Attribute>();
        foreach (var a in it)
            SceneAttrs.Add(a.GetHashCode(), Attribute.Normal);
    }

    public SceneViewMgrGroup()
    {
        tk = new List<SceneViewMgrInterface>();
        SceneAttrs = new Dictionary<int, Attribute>();
    }

    public void Add(SceneViewMgrInterface v, Attribute attr = Attribute.Normal)
    {
        tk.Add(v);
        SceneAttrs.Add(v.GetHashCode(), attr);
    }

    public void Remove(SceneViewMgrInterface v)
    {
        tk.Remove(v);
        SceneAttrs.Remove(v.GetHashCode());
    }

    public void Clear()
    {
        tk.Clear();
        SceneAttrs.Clear();
    }

    public override void OnEnterStage()
    {
        foreach (var it in tk)
        {
            if (!IsManual(it))
                it.OnEnterStage();
        }
        base.OnEnterStage();
    }

    public override void OnLoadScene(Dictionary<string, int> preload)
    {
        foreach (var it in tk)
            it.OnLoadScene(preload);
    }

    public override void OnStageInit(Transform root)
    {
        foreach (var it in tk)
            it.OnStageInit(root);
        base.OnStageInit(root);
    }

    public override void Update()
    {
        foreach (var it in tk)
        {
            if (!IsManual(it))
                it.Update();
        }
    }

    public override void LateUpdate()
    {
        foreach (var it in tk)
        {
            if (!IsManual(it))
                it.LateUpdate();
        }
    }

    public override void FixedUpdate()
    {
        foreach (var it in tk)
        {
            if (!IsManual(it))
                it.FixedUpdate();
        }
    }

    public override void Destroy()
    {
        foreach (var it in tk)
            it.Destroy();
        base.Destroy();
    }

    public override void EvtListener(bool isAdd)
    {
        foreach (var it in tk)
            it.EvtListener(isAdd);
    }

    public override void MsgListener(bool isAdd)
    {
        foreach (var it in tk)
        {
            if (!IsManual(it))
                it.MsgListener(isAdd);
        }
    }

    public override void RefreshData()
    {
        foreach (var it in tk)
            it.RefreshData();
    }

    public override void OnExitStage()
    {
        foreach (var it in tk)
        {
            if (!IsManual(it))
                it.OnExitStage();
        }
        base.OnExitStage();
    }

    public bool IsManual(SceneViewMgrInterface v)
    {
        return SceneAttrs.ContainsKey(v.GetHashCode()) && (SceneAttrs[v.GetHashCode()] & Attribute.Manual) == Attribute.Manual;
    }
}

public class ColliderClick
{
    protected int layerMask;
    protected float VaildDist = 100.0f;
    protected float sqrMouseMoveVaildDist = 4.0f;
    protected bool ignoreUI = false;
    protected Vector2 downPos;
    protected HashSet<Collider> colliders = new HashSet<Collider>();
    public Action<Collider> OnClick;
    protected Camera cam;
    public Camera camera
    {
        set
        {
            cam = value;
        }
    }

    public ColliderClick(int layerMask, float VaildDist = 100.0f, Camera cam = null, bool ignoreUI = false)
    {
        this.layerMask = layerMask;
        this.VaildDist = VaildDist;
        this.cam = cam;
        this.ignoreUI = ignoreUI;
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
    protected bool Clicked(Vector3 pos)
    {
        if (cam == null) return false;
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
        return coll != null;
    }
    private bool UpPosVaild(Vector2 upPos)
    {
        return (upPos - downPos).sqrMagnitude <= sqrMouseMoveVaildDist;
    }
    public void update()
    {
        if (Input.touchSupported)
        {
            if (Input.touchCount > 1 || Input.touchCount <= 0) return;

            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                if (!ignoreUI && CheckHitUI())
                    return;
                downPos = touch.position;
            }
            else
            if (touch.phase == TouchPhase.Ended)
            {
                if (!ignoreUI && CheckHitUI())
                    return;
                if (!UpPosVaild(touch.position)) return;
                var pos = GetTouchPos(touch);
                foreach (var p in pos)
                {
                    if (Clicked(new Vector3(p.x, p.y, 0.0f)))
                        break;
                }
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (!ignoreUI && CheckHitUI())
                    return;
                downPos = Input.mousePosition;
            }
            else
            if (Input.GetMouseButtonUp(0))
            {
                if (!ignoreUI && CheckHitUI())
                    return;
                if (!UpPosVaild(Input.mousePosition)) return;
                Clicked(Input.mousePosition);
            }
        }

    }

    public static bool CheckHitUI()
    {
        var g = UnityEngine.EventSystems.EventSystem.current;
        if (g != null && g.currentSelectedGameObject != null)
            return true;
        return false;
    }

    internal void Clear()
    {
        colliders.Clear();
    }

    public static Vector2[] GetTouchPos(Touch touch)
    {
        var arr = new List<Vector2>();
        var r = touch.radius - touch.radiusVariance;
        arr.Add(touch.position);
        arr.Add(touch.position + new Vector2(r, 0.0f));
        arr.Add(touch.position + new Vector2(-r, 0.0f));
        arr.Add(touch.position + new Vector2(0.0f, r));
        arr.Add(touch.position + new Vector2(0.0f, -r));
        return arr.ToArray();
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
        if (t == typeof(bool))
        {
            if (bool.TryParse(s, out var v))
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
        if (t == typeof(Color))
        {
            var result = ColorUtility.TryParseHtmlString(s, out Color c);
            if (!result)
            {
                Debug.LogError($"color解析失败，源字符串{s}");
            }
            return c;
        }
        if (t == typeof(AnyArray))
        {
            try
            {
                AnyArray array = new AnyArray(s);
                return array;
            }
            catch //(Exception e)
            { }
        }
        return null;
    }

}

public static class AnyArrayTyMap
{
    public static readonly Dictionary<string, System.Type> TypeMap = new KeyValuePair<string, System.Type>[]{
        new KeyValuePair<string, System.Type>("b",typeof(bool)),
        new KeyValuePair<string, System.Type>("i",typeof(int)),
        new KeyValuePair<string, System.Type>("l",typeof(long)),
        new KeyValuePair<string, System.Type>("f",typeof(float)),
        new KeyValuePair<string, System.Type>("lf",typeof(double)),
        new KeyValuePair<string, System.Type>("s",typeof(string)),
        new KeyValuePair<string, System.Type>("vec2",typeof(Vector2)),
        new KeyValuePair<string, System.Type>("vec3",typeof(Vector3)),
        new KeyValuePair<string, System.Type>("vec4",typeof(Vector4)),
        new KeyValuePair<string, System.Type>("c",typeof(Color)),
    }.ToDictionary();
}

public class AnyArray
{
    protected List<object> objs;
    protected List<System.Type> tyArr;
    public int Count => objs.Count;
    public AnyArray(string str)
    {
        if (str.Length == 0)
        {
            this.objs = new List<object>();
            this.tyArr = new List<Type>();
        }
        else
        if (parse(str, out var objs, out var tys))
        {
            this.objs = objs;
            this.tyArr = tys;
        }
        else
        {
            throw new Exception("Parse AnyArray Failed!!!");
        }
    }

    public AnyArray(object[] a)
    {
        objs = new List<object>();
        tyArr = new List<Type>();
        foreach (var t in a)
        {
            if(t == null) continue;
            tyArr.Add(t.GetType());
            objs.Add(t);
        }
    }

    public static bool TryParse(string str,out AnyArray arr)
    {
        arr = null;
        try
        {
            arr = new AnyArray(str);
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    public bool good_idx(int idx)
    {
        return idx >= 0 && idx < objs.Count;
    }
    public bool TypeEq<T>(int idx)
    {
        if (good_idx(idx))
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

    public bool TryGet<T>(int idx, out T v, T def = default)
    {
        v = def;
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

    public bool parse(string str, out List<object> objs, out List<System.Type> tys)
    {
        var cs = str.Trim().ToCharArray();
        int step = 0;
        System.Type currTy = null;
        objs = new List<object>();
        tys = new List<Type>();
        for (int i = 0; i < cs.Length; ++i)
        {
            var it = cs[i];
            if (step == 0 && it == '[')
            {
                ++step; continue;
            }
            if (step == 1 && it == ']')
            {
                if (i + 1 == cs.Length)
                {
                    step = 100; break;
                }
            }
            if (step == 1)
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

    private bool GetType(char[] arr, int it, out System.Type ty, out int new_it)
    {
        ty = null; new_it = it;
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

    private bool GetVal(char[] arr, int it, System.Type ty, out object val, out int new_it)
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
                    val = Serialize.Deserialize(sb.ToString().TrimEnd(), ty);
                    break;
                }
            }
        }
        return step == 2 && val != null;
    }

    private System.Type GetTypeByTag(string res)
    {
        if (AnyArrayTyMap.TypeMap.TryGetValue(res, out var s))
        {
            return s;
        }
        return null;
    }

    public object[] GetRange(int start)
    {
        if (start >= Count) return null;
        return objs.GetRange(start, objs.Count - start).ToArray();
    }
    public object[] GetRange(int start, int count)
    {
        if (start >= Count) return null;
        return objs.GetRange(start, count).ToArray();
    }
}

public interface SendMessage<T>
{
    void Send<M>(M msg, params object[] args)
        where M : T;
}

public interface RecvMessage<E>
    where E : struct
{
    void Recv(E id, object ack, object req);
}

public interface CanSetOnChangeAction
{
    void SetOnChangeAction(Action on);
}

[Serializable]
public abstract class IndexableData<I, T> : CanSetOnChangeAction
    where I : struct
    where T : new()
{
    protected Dictionary<I, T> map = new Dictionary<I, T>();
    [NonSerialized]
    protected Action OnChangeAction;
    public T this[I i]
    {
        get
        {
            if (!map.ContainsKey(i))
                Add(i, new T());
            return map[i];
        }
        set
        {
            if (!Set(i, value))
                Add(i, value);
        }
    }
    //public T Find(I id)
    //{
    //    if (map.TryGetValue(id, out var v))
    //        return v;
    //    return default(T);
    //}
    public bool Add(I id, T t)
    {
        if (map.ContainsKey(id))
            return false;
        map.Add(id, t);
        OnAdd(t);
        OnChanged();
        return true;
    }

    private void OnAdd(T t)
    {
        if (t is CanSetOnChangeAction cs)
        {
            cs.SetOnChangeAction(OnChangeAction);
        }
    }

    public bool Remove(I id)
    {
        var f = map.Remove(id);
        if (f)
            OnChanged();
        return f;
    }
    public bool RemoveOther(List<I> exclude)
    {
        var f = map.RemoveOther(exclude);
        if (f)
            OnChanged();
        return f;
    }
    public bool Contain(I id)
    {
        return map.ContainsKey(id);
    }
    public Pair<I, T> GetFirst()
    {
        foreach (var v in map)
            return new Pair<I, T>(v.Key, v.Value);
        return null;
    }
    public bool Set(I id, T t)
    {
        if (map.ContainsKey(id))
        {
            var old = map[id];
            map[id] = t;
            OnAdd(t);
            if (!old.Equals(t)) OnChanged();
            return true;
        }
        return false;
    }
    public int Count => map.Count;
    public void Clear()
    {
        if (map.Count == 0) return;
        map.Clear();
        OnChanged();
    }

    protected virtual void OnChanged()
    {
        OnChangeAction?.Invoke();
    }
    public virtual void SetOnChangeAction(Action on)
    {
        OnChangeAction = on;
        foreach (var a in map)
        {
            if (a.Value is CanSetOnChangeAction cs)
            {
                cs.SetOnChangeAction(on);
            }
        }
    }
    public override bool Equals(object obj)
    {
        if (obj is IndexableData<I, T> oth)
        {
            var dict3 = oth.map.Where(x => !map.ContainsKey(x.Key) || !map[x.Key].Equals(x.Value))
                         .Union(map.Where(x => !oth.map.ContainsKey(x.Key) || !oth.map[x.Key].Equals(x.Value)))
                         .ToDictionary(x => x.Key, x => x.Value);
            return dict3.Count == 0;
        }
        return false;
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
public interface IPersistentData
{
    void Load(string parentPath);
    void Save();
    void Dispose();
}
public class PersistentDataset : IPersistentData
{
    protected List<IPersistentData> datas = new List<IPersistentData>();

    public void Add(IPersistentData d)
    {
        datas.Add(d);
    }
    public void Dispose()
    {
        foreach (var it in datas)
            it.Dispose();
        datas.Clear();
    }

    public void Load(string parentPath)
    {
        foreach (var it in datas)
            it.Load(parentPath);
    }

    public void Save()
    {
        foreach (var it in datas)
            it.Save();
    }
}
public static class ListExt
{
    public static string toStr<T>(this List<T> self, char delimiter = ',', bool hasBrackets = false, bool rmLastDelimiter = true)
    {
        return toStr(self, (a) => a.ToString(), delimiter, hasBrackets, rmLastDelimiter);
    }
    public static string toStr<T>(this List<T> self, Func<T, string> to, char delimiter = ',', bool hasBrackets = false, bool rmLastDelimiter = true)
    {
        var sb = new StringBuilder();
        if (hasBrackets)
            sb.Append("[");
        foreach (var a in self)
        {
            sb.Append(to(a));
            sb.Append(delimiter);
        }
        if (self.Count > 0 && rmLastDelimiter)
            sb.Remove(sb.Length - 1, 1);
        if (hasBrackets)
            sb.Append("]");
        return sb.ToString();
    }

    public static void MakeUp<T>(this List<T> self,int count)
    where T:new()
    {
         if(self.Count >= count) return;
         var len = count - self.Count;
         for (int i = 0; i < len; ++i)
         {
             self.Add(new T());
         }
    }
    public static void MakeUpDef<T>(this List<T> self,int count)
    {
        if(self.Count >= count) return;
        var len = count - self.Count;
        for (int i = 0; i < len; ++i)
        {
            self.Add(default(T));
        }
    }
}

public static class StringEx
{
    public static bool isVaild(this string self)
    {
        return self != null && self.Length != 0;
    }
}

public static class DictEx
{
    public static bool RemoveOther<K, V>(this Dictionary<K, V> map, List<K> exclude)
    {
        var list = new List<K>();
        foreach (var it in map)
        {
            if (exclude.Contains(it.Key)) continue;
            list.Add(it.Key);
        }
        var f = false;
        foreach (var it in list)
        {
            if (map.Remove(it))
                f = true;
        }
        return f;
    }
}

namespace TagNum
{
    public class _0 : TickTag { }
    public class _1 : TickTag { }
    public class _2 : TickTag { }
    public class _3 : TickTag { }
    public class _4 : TickTag { }
    public class _5 : TickTag { }
    public class _6 : TickTag { }
    public class _7 : TickTag { }
    public class _8 : TickTag { }
    public class _9 : TickTag { }
    public static class NumMap
    {
        public static TickTag Get(int i)
        {
            var ty = GetTy(i);
            if (ty == null) return null;
            return (TickTag)System.Activator.CreateInstance(ty);
        }
        public static Type GetTy(int i)
        {
            return Type.GetType($"TagNum._{i}");
        }
        public static TickTag Make(Type parentTy, int i)
        {
            var ty = parentTy.MakeGenericType(GetTy(i));
            if (ty == null) return null;
            return (TickTag)System.Activator.CreateInstance(ty);
        }
        public static TickTag MakeEx(Type parentTy, params int[] i)
        {
            var Ts = new Type[i.Length];
            for (int a = 0; a < i.Length; ++a)
            {
                Type t = null;
                if ((t = GetTy(i[a])) == null)
                    return null;
                Ts[a] = t;
            }
            var ty = parentTy.MakeGenericType(Ts);
            if (ty == null) return null;
            return (TickTag)System.Activator.CreateInstance(ty);
        }

        public static Type MakeType(Type parentTy, params int[] i)
        {
            var Ts = new Type[i.Length];
            for (int a = 0; a < i.Length; ++a)
            {
                Type t = null;
                if ((t = GetTy(i[a])) == null)
                    return null;
                Ts[a] = t;
            }
            return parentTy.MakeGenericType(Ts);
        }
        public static bool HasNumTags(this TickGroup self, Type t, int b = 0, int e = 2)
        {
            for (int i = b; i < e; ++i)
            {
                if (self.HasTag(Make(t, i)))
                    return true;
            }
            return false;
        }
    }
}
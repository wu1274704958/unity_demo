using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum GStateEnum
{
    Success,
    Running,
    PrePare,
    Failed,
    Pause,
    Stoped
}

public abstract class GStateHandler
{
    protected GState nextState;
    protected Dictionary<string, (Type, object)> m_param;
    protected Dictionary<string, (float, object)> m_lazy_param;

    public GStateHandler()
    {
        nextState = ExpectNext();
        m_param = OnSetPrarmDict();
    }
    public GStateHandler(GState nextState)
    {
        this.nextState = nextState;
        m_param = OnSetPrarmDict();
    }
    public GStateHandler(GStateHandler next)
    {
        this.nextState = new GState(GStateEnum.Success,next);
        m_param = OnSetPrarmDict();
    }
    protected virtual GState ExpectNext()
    {
        return new GState(GStateEnum.Success);
    }
    protected virtual Dictionary<string, (Type, object)> OnSetPrarmDict()
    {
        return null;
    }

    public void SetNext(GStateEnum e,GStateHandler n)
    {
        nextState = new GState(e,n);
    }
    public virtual void AddPrarm<T>(string str,T v = null)
        where T:class
    {
        if (m_param == null) m_param = new Dictionary<string, (Type, object)>();
        if(!m_param.ContainsKey(str))
        {
            m_param[str] = (typeof(T), v);
        }
    }
    public virtual void AddPrarmX<T>(string str)
        where T : struct
    {
        if (m_param == null) m_param = new Dictionary<string, (Type, object)>();
        if (!m_param.ContainsKey(str))
        {
            m_param[str] = (typeof(Wrap<T>), null);
        }
    }
    public virtual void AddPrarmDef<T>(string str)
        where T : struct
    {
        if (m_param == null) m_param = new Dictionary<string, (Type, object)>();
        if (!m_param.ContainsKey(str))
        {
            m_param[str] = (typeof(Wrap<T>), new Wrap<T>(default(T)));
        }
    }
    public virtual void AddPrarmX<T>(string str,T x)
        where T : struct
    {
        if (m_param == null) m_param = new Dictionary<string, (Type, object)>();
        if (!m_param.ContainsKey(str))
        {
            m_param[str] = (typeof(Wrap<T>), new Wrap<T>(x));
        }
    }
    public virtual bool InputParam<T>(string key,T v)
        where T:class
    {
        if (m_param == null) return false;
        if (m_param.TryGetValue(key, out var val))
        {
            if(typeof(T) == val.Item1)
            {
                m_param[key] = (typeof(T), v);
                return true;
            }
        }
        return false;
    }
    public virtual bool InputParamX<T>(string key, T v)
        where T : struct
    {
        if (m_param == null) return false;
        if (m_param.TryGetValue(key, out var val))
        {
            if (typeof(Wrap<T>) == val.Item1)
            {
                m_param[key] = (typeof(Wrap<T>), new Wrap<T>(v));
                return true;
            }
        }
        return false;
    }
    public virtual bool InputParamNext<T>(string key, T v)
        where T : class
    { 
        if(nextState.hasNextHandler())
        {
            return nextState.nextHandler.InputParam<T>(key, v);
        }
        return false;
    }
    public virtual bool InputParamNextX<T>(string key, T v)
        where T : struct
    {
        if (nextState.hasNextHandler())
        {
            return nextState.nextHandler.InputParamX<T>(key, v);
        }
        return false;
    }
    public virtual bool AllPrarmSet()
    {
        if (m_param == null) return true;
        foreach(var p in m_param)
        {
            if (p.Value.Item2 == null) return false;
        }
        return true;
    }

    public virtual T GetParam<T>(string key)
        where T:class
    {
        if (m_param == null) return null;
        if (m_param.TryGetValue(key, out var val))
        {
            if (typeof(T) == val.Item1)
            {
                return val.Item2 as T;
            }
        }
        return null;
    }
    public virtual bool GetParamX<T>(string key,out T v)
        where T : struct
    {
        v = default(T);
        if (m_param == null) return false;
        if (m_param.TryGetValue(key, out var val))
        {
            if (typeof(Wrap<T>) == val.Item1)
            {
                v = (val.Item2 as Wrap<T>).val;
                return true;
            }
        }
        return false;
    }
    public virtual bool HasParam(string key)
    {
        if (m_param == null) return false;
        return m_param.ContainsKey(key) && m_param[key].Item2 != null;
    }
    public GState Success()
    {
        if(nextState != null)
        {
            nextState.state = GStateEnum.Success;
            return nextState;
        }
        return new GState(GStateEnum.Success);
    }
    public GState Failed()
    {
        if (nextState != null)
        {
            nextState.state = GStateEnum.Failed;
            return nextState;
        }
        return new GState(GStateEnum.Failed);
    }
    public GState OnlyState(GStateEnum e)
    {
        return new GState(e);
    }

    public abstract GState onUpdate();
    public virtual void update()
    {
        onUpdate();
        lateUpdate();
    }
    public virtual void lateUpdate()
    {
        if (m_lazy_param == null) return;
        List<string> temp = new List<string>();
        foreach(var a in m_lazy_param)
        {
            var t = a.Value.Item1;
            t -= Time.deltaTime;
            if(t < 0.0f)
            {
                InputParamForce(a.Key,a.Value.Item2);
                temp.Add(a.Key);
            }
            else
            {
                m_lazy_param[a.Key] = (t, a.Value.Item2);
            }
        }
        foreach (var a in temp)
        {
            m_lazy_param.Remove(a);
        }
    }

    public virtual bool InputParamLazy<T>(string key, T v,float ms)
         where T : class
    {
        if (m_param == null) return false;
        if (m_param.TryGetValue(key, out var val))
        {
            if (typeof(T) == val.Item1)
            {
                addLazy(key,v,ms);
                return true;
            }
        }
        return false;
    }

    private void addLazy<T>(string key, T v, float ms) where T : class
    {
        if (m_lazy_param == null) m_lazy_param = new Dictionary<string, (float, object)>();
        if(m_lazy_param.ContainsKey(key))
        {
            m_lazy_param[key] = (ms, v);
        }
        else
        {
            m_lazy_param.Add(key, (ms, v));
        }
    }

    public virtual bool InputParamXLazy<T>(string key, T v, float ms)
        where T : struct
    {
        if (m_param == null) return false;
        if (m_param.TryGetValue(key, out var val))
        {
            if (typeof(Wrap<T>) == val.Item1)
            {
                addLazy(key, new Wrap<T>(v), ms);
                return true;
            }
        }
        return false;
    }

    private bool InputParamForce(string key, object item2)
    {
        if (m_param == null) return false;
        if (m_param.TryGetValue(key, out var val))
        {
            m_param[key] = (val.Item1,item2);
            return true;
        }
        return false;
    }

    public bool Input(GStateHandler oth,string key)
    {
        if (oth == null || oth.m_param == null || m_param == null) return false;
        if(m_param.ContainsKey(key) && oth.m_param.ContainsKey(key) && m_param[key].Item1 == oth.m_param[key].Item1)
        {
            m_param[key] = oth.m_param[key];
            return true;
        }
        return false;
    }

    public virtual void start() { }
    public virtual void end() { }
}

public class GState
{
    public GStateEnum state { get; set; }
    public GStateHandler nextHandler { get; private set; }

    public GState(GStateEnum e)
    {
        state = e;
    }

    public GState(GStateEnum e, GStateHandler t)
    {
        state = e;
        nextHandler = t;
    }

    public bool hasNextHandler() { return nextHandler != null; }
}

public class GStateMachine
{
    private GStateHandler m_cur_state;
    private bool m_pause { get; set; }

    public void start(GStateHandler c)
    {
        if (c == null) return;
        m_cur_state = c;
        m_pause = false;
        m_cur_state.start();
    }

    public bool InputParam(string key, object v)
    {
        if (m_cur_state != null)
        {
            return m_cur_state.InputParam(key, v);
        }
        return false;
    }

    public GStateEnum update()
    {
        if (m_pause) return GStateEnum.Pause;
        if (m_cur_state == null) return GStateEnum.Stoped;

        GState state = m_cur_state.onUpdate();
        switch(state.state)
        {
            case GStateEnum.Success:
            case GStateEnum.Failed:
                if (state.nextHandler != null)
                {
                    m_cur_state.end();
                    m_cur_state = state.nextHandler;
                    m_cur_state.start();
                    return GStateEnum.Running;
                }
                else
                {
                    Stop();
                    return state.state;
                }
            default:
                if (state.nextHandler != null)
                {
                    m_cur_state.end();
                    m_cur_state = state.nextHandler;
                    m_cur_state.start();
                }
                return state.state;
        }
    }

    public void Stop()
    {
        m_cur_state?.end();
        m_cur_state = null;
        m_pause = true;
    }
}

public class WaitStateHandler : GStateHandler
{
    public float dur = 2.0f;
    private float delay = 0.0f;

    public WaitStateHandler(float dur) : base()
    {
        this.dur = dur;
    }
    public WaitStateHandler(float dur,GStateHandler h) : base(h)
    {
        this.dur = dur;
    }
    public override GState onUpdate()
    {
        delay += Time.deltaTime;
        if (delay >= dur) return nextState;
        return new GState(GStateEnum.Running);
    }
}


public class WaitFrameStateHandler : GStateHandler
{
    public int dur = 2;
    private int delay = 0;

    public WaitFrameStateHandler(int dur) : base()
    {
        this.dur = dur;
    }
    public WaitFrameStateHandler(int dur, GStateHandler h) : base(h)
    {
        this.dur = dur;
    }
    public override GState onUpdate()
    {
        delay += 1;
        if (delay >= dur) return nextState;
        return new GState(GStateEnum.Running);
    }
}


public abstract class SampleCheckGS : GStateHandler
{
    public SampleCheckGS(GStateHandler n) : base(n)
    {
        AddPrarmX<int>(CAM.WaitFrame, -1);
    }
    public override GState onUpdate()
    {
        if (!AllPrarmSet()) return Failed();
        if (Check())
        {
            InputParamNextX<bool>(InputPrarmKey(), true);
            return Success();
        }
        GetParamX<int>(CAM.WaitFrame, out var wait);
        if (wait > 0)
        {
            InputParamX<int>(CAM.WaitFrame, wait - 1);
        }
        else if (wait == 0)
        {
            InputParamNextX<bool>(InputPrarmKey(), false);
            return Failed();
        }
        return OnlyState(GStateEnum.Running);
    }
    protected abstract bool Check();
    protected abstract string InputPrarmKey();
}

public abstract class SampleStateHandler : GStateHandler
{
    public SampleStateHandler(GStateHandler n) : base(n)
    {
        AddPrarmX<int>(CAM.WaitFrame, -1);
    }
    public override GState onUpdate()
    {
        if (Check())
        {
            OnHandlRes(true);
            return Success();
        }
        GetParamX<int>(CAM.WaitFrame, out var wait);
        if (wait > 0)
        {
            InputParamX<int>(CAM.WaitFrame, wait - 1);
        }
        else if (wait == 0)
        {
            OnHandlRes(false);
            return Failed();
        }
        return OnlyState(GStateEnum.Running);
    }
    protected abstract bool Check();
    protected virtual void OnHandlRes(bool v) { }
}


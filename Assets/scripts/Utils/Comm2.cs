using DG.Tweening;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public abstract class ProducableTick : TickTick
{
    public TickTick Next { get; protected set; }
    public ProducableTick SetNext(TickTick n)
    {
        Next = n;
        return this;
    }
    public bool HasNext => Next != null;
    public TickOp NextOnRmSelf => HasNext ? (TickOp)new TickReplaceOp() { next = Next } : new TickRmSelfOp();
}

public class TaskReproducible : DelayTask
{
    public Action _OnAdd, _OnRemove;
    public TickTick next;
    public TaskReproducible(Action action, TimeSpan time, int TriggerCount, TickTick next = null) : base(action, time, TriggerCount)
    {
        this.next = next;
    }
    public TaskReproducible(Func<TickOp> action, TimeSpan time, int TriggerCount, TickTick next = null) : base(action, time, TriggerCount)
    {
        this.next = next;
    }

    protected override TickOp OnTrigger()
    {
        var op = base.OnTrigger() as MlutTickOp;
        if (TriggerCount <= 0)
        {
            if (next != null)
                return new MlutTickOp() { ops = new TickOp[]{ op, new TickReplaceOp() { next = next } } };
            else
                return new MlutTickOp() { ops = new TickOp[] { op, new TickRmSelfOp() } };  
        }
        return op;
    }

    public TaskReproducible SetNext(TickTick n)
    {
        next = n;
        return this;
    }
    public override void OnRemove()
    {
        base.OnRemove();
        _OnRemove?.Invoke();
    }
    public override void OnAdd(TickGroup tg)
    {
        base.OnAdd(tg);
        _OnAdd?.Invoke();
    }
    public TaskReproducible SetOnRemove(Action n)
    {
        _OnRemove = n;
        return this;
    }
    public TaskReproducible SetOnAdd(Action n)
    {
        _OnAdd = n;
        return this;
    }
}

public class PlayLoopAniTick : TaskReproducible
{
    protected Animator ani;
    protected string AniName;
    protected int layer = 0;
    private float BeginSpeed = 0.05f;
    public bool PartOfName = false;
    public PlayLoopAniTick(Animator animator, TimeSpan time, string AniName, int layer = 0, TickTick next = null) : base(null, time, 1, next)
    {
        this.ani = animator;
        this.AniName = AniName;
        this.layer = layer;
    }
    public override void OnAdd(TickGroup tg)
    { 
        base.OnAdd(tg);
        if (PartOfName)
        {
            AniName = ani.EnumAllAni((clip) => clip.name.Contains(AniName)).FirstOrDefault();
        }

        if (AniName.isVaild())
        {
            ani.speed = BeginSpeed;
            ani.Play(AniName, layer);
        }
    }
    protected override TickOp OnTrigger()
    {
        ani.speed = 0.0f;
        return base.OnTrigger();
    }
    public PlayLoopAniTick SetPartOfName(bool v)
    {
        PartOfName = v;
        return this;
    }
    public PlayLoopAniTick SetSpeed(float v)
    {
        BeginSpeed = v;
        return this;
    }
}

public class PlayAniTick : ProducableTick
{
    public class PlayAniTag : TickTag { }
    protected Animator ani;
    protected string AniName;
    protected int layer = 0;
    protected float progress;
    public float BeginSpeed = 1.0f;
    public bool NotCurrentTrigger = false;
    public Action<Animator> OnPlay = null;
    public Action<Animator> OnEnd = null;
    public Func<Animator,TickOp> OnTriggerCb = null;
    public bool PartOfName = false;
    public bool Reverse = false;
    public PlayAniTick(Animator a, string ani = null, int layer = 0, float progress = 0.0f,bool reverse = false) 
    {
        this.ani = a;
        this.AniName = ani;
        this.layer = layer;
        this.progress = progress;
        this.Reverse = reverse;
        SetTag<PlayAniTag>();
    }
    public override TickOp update(TimeSpan t)
    {
        TickOp res = null;
        var v = false;
        if (!AniName.isVaild())
        {
            v = ani.runtimeAnimatorController != null ? ani.animationIsEnd(layer,Reverse) : true;
        }
        else
            v = ani.animationIsEnd(AniName, layer, NotCurrentTrigger,Reverse);
        if (v)
        {
            res += OnTriggerCb?.Invoke(ani);
            res += NextOnRmSelf;
        }
        return res;
    }
    public void SetAniProgress(float p)
    {
        ani.SetCurrentAnimationProgress(p, layer);
        ani.speed = BeginSpeed;
    }

    public override void OnRemove()
    {
        base.OnRemove();
        OnEnd?.Invoke(ani);
    }

    public override void OnAdd(TickGroup tg)
    {
        base.OnAdd(tg);
        OnPlay?.Invoke(ani);
        if (PartOfName)
        {
            AniName = ani.EnumAllAni((clip) => clip.name.Contains(AniName)).FirstOrDefault();
        }
        if (AniName.isVaild())
        {
            if(Reverse)
            {
                ani.StartPlayback();
                ani.speed = -BeginSpeed;
            }
            else
            {
                ani.StopPlayback();
                ani.speed = BeginSpeed;
            }
            ani.Play(AniName, layer, progress);
        }
    }

    public PlayAniTick SetNotCurrentTrigger(bool v)
    {
        NotCurrentTrigger = v;
        return this;
    }
    public PlayAniTick SetOnPlay(Action<Animator> v)
    {
        OnPlay = v;
        return this;
    }
    public PlayAniTick SetOnEnd(Action<Animator> v)
    {
        OnEnd = v;
        return this;
    }
    public PlayAniTick SetOnTriggerCb(Func<Animator,TickOp> v)
    {
        OnTriggerCb = v;
        return this;
    }
    public PlayAniTick SetPartOfName(bool v)
    {
        PartOfName = v;
        return this;
    }
}

public class PlayTweenTick : TickTick
{
    protected Func<Tween> func;
    public TickTick next;
    Tween tween;
    private Ease _ease = Ease.Linear;
    public PlayTweenTick(Func<Tween> func,Ease e = Ease.Linear)
    {
        this.func = func;
        _ease = e;
    }

    public override void OnAdd(TickGroup tg)
    {
        base.OnAdd(tg);
        tween = func.Invoke();
        tween.SetEase(_ease);
        tween.SetAutoKill(false);
        
    }

    public override TickOp update(TimeSpan t)
    {
        if (tween == null)
            return new TickRmSelfOp();
        TickOp res = null;
        if (tween != null && tween.IsComplete())
        {
            tween.Kill();
            if (next != null)
                res = new TickReplaceOp() { next = next };
            else
                res = new TickRmSelfOp();
        }
        return res;
    }

    public void forceComplete()
    {
        tween?.Complete();
    }
    public PlayTweenTick SetNext(TickTick n)
    {
        next = n;
        return this;
    }
}



public class CommPopupTextSubViewUI : SubViewUI
{
    public UnityEngine.UI.Text txt_Text;

    public CommPopupTextSubViewUI()
    {

    }
    public override void init(Transform root)
    {
        base.init(root);
        this.txt_Text = root.Find("SV_Text").GetComponent<UnityEngine.UI.Text>();
    }
}

public static class ScrollRectExt
{
    public static bool Contain(this RectTransform selfRt, RectTransform rt, float offset = 0.1f)
    {
        Vector3[] containerCorners = new Vector3[4];
        selfRt.GetWorldCorners(containerCorners);

        Vector3[] rtCorners = new Vector3[4];
        rt.GetWorldCorners(rtCorners);

        return containerCorners[0].x - offset <= rtCorners[0].x && containerCorners[0].y - offset <= rtCorners[0].y &&
            containerCorners[2].x + offset >= rtCorners[2].x && containerCorners[2].y + offset >= rtCorners[2].y;
    }
    public static bool IsVisible(this ScrollRect self, RectTransform rt, float offset = 0.1f)
    {
        var selfRt = self.GetComponent<RectTransform>();
        return selfRt.Contain(rt, offset);
    }

    public static bool ScrollTo(this ScrollRect self, RectTransform rt, TweenCallback complete = null, float offset = 0.0f, float dur = 1.0f, Ease ease = Ease.Linear)
    {
        var selfRt = self.GetComponent<RectTransform>();
        var bound = RectTransformUtility.CalculateRelativeRectTransformBounds(self.content, rt);

        var v = (self.horizontal ? -bound.min.x : -bound.max.y) + offset;
        var now = self.horizontal ? self.content.anchoredPosition.x : self.content.anchoredPosition.y;
        var max = self.horizontal ? self.content.rect.width - self.viewport.rect.width : self.content.rect.height - self.viewport.rect.height;
        if (max <= 0)
        {
            complete?.Invoke();
            return true;
        }
        if (v < 0)
        {
            if (v < -max) v = -max;
        }
        else
        {
            if (v > max) v = max;
        }
        Debug.Log($"Scroll To v {v} min = {bound.min} max = {bound.max}");
        if (Mathf.Abs(now - v) <= float.Epsilon)
        {
            if (self.horizontal)
                self.content.anchoredPosition = new Vector2(v, self.content.anchoredPosition.y);
            else
                self.content.anchoredPosition = new Vector2(self.content.anchoredPosition.x, v);
            complete?.Invoke();
            return true;
        }
        else
        {
            if (self.horizontal)
            {
                var tween = self.content.DOAnchorPosX(v, dur);
                tween.OnComplete(complete);
            }
            else
            {
                var tween = self.content.DOAnchorPosY(v, dur);
                tween.OnComplete(complete);
            }
            return false;
        }
    }
}

public static class GameObjectEx
{
    public static void SetLayerContainChildlren(this GameObject self, int layer)
    {
        self.layer = layer;
        for (int i = 0; i < self.transform.childCount; ++i)
        {
            var go = self.transform.GetChild(i).gameObject;
            go.SetLayerContainChildlren(layer);
        }
    }

    public static T GetOrAddComponent<T>(this GameObject self) where T : Component
    {
        var v = self.GetComponent<T>();
        if (v == null)
            return self.AddComponent<T>();
        return v;
    }
    public static T GetOrAddComponent<T>(this Transform self) where T : Component
    {
        var v = self.GetComponent<T>();
        if (v == null)
            return self.gameObject.AddComponent<T>();
        return v;
    }
}

public class FaithfulVal<T>
    where T : struct, IComparable
{
    public T Expected;
    public T m_val;


    public FaithfulVal(T Expected)
    {
        this.Expected = Expected;
    }
    public FaithfulVal(T Expected, T v)
    {
        this.Expected = Expected;
        val = v;
    }
    public void Reset(T v)
    {
        m_val = v;
    }
    public void Reset(T v, T expec)
    {
        m_val = v;
        Expected = expec;
    }
    public T val
    {
        get => m_val;
        set
        {
            if (m_val.Equals(Expected))
                return;
            m_val = value;
        }
    }
}
public class KnobTick : ProducableTick
{
    public enum EState
    {
        Manual = 1,
        Adsorption = 2,
        Auto = 3,
        Stop = 4
    }
    public float ManualFactor = 1.0f;
    public float AdsorptionFactor = 1.0f;
    protected float _Velocity = 0.0f;
    protected float _Position = 0.0f;
    /// <summary>
    /// 能够触发吸附的最高速度
    /// </summary>
    public float AdsorptionMinVelocity = 0.0f;
    public float MinAdsorptionRange = 0.1f;
    public bool Loop = false;
    public float AutoVelocity = 0.01f;
    public float AdsorptionVelocity { get; protected set; } = 0.01f;
    public bool Manual { get;protected set;} = true;
    protected bool HasAdsorption = true;
    public EState State { get; protected set; } = EState.Manual;
    public Func<TickOp> OnArrive = null;
    public bool AutoKill = true;
    protected bool KillSelfAfterAdsorption = false;
    protected bool ForceNotOverflow = false;
    protected bool TouchBorderToManual = false;
    public KnobTick(){}
    public KnobTick(bool manual,bool hasAdsorption,float velocity)
    {
        Manual = manual;
        HasAdsorption = manual && hasAdsorption;
        State = manual ? EState.Manual : EState.Auto;
        AdsorptionVelocity = velocity;
    }
    public float Velocity
    {
        get => _Velocity;
        set
        {
            if(State != EState.Manual) return;
            _Velocity = value;
            OnChangeVelocity(true);
        }
    }
    protected float VelocityAuto
    {
        set { _Velocity = value;OnChangeVelocity(false); }
    }

    public float Position
    {
        get => _Position;
        set
        {
            _Position = value;ClampPosition();OnChangePos(_Position, true);
        }
    }
    protected float PositionAuto
    {
        set { _Position = value;ClampPosition();OnChangePos(_Position,false); }
    }
    protected void ClampPosition()
    {
        if (!Loop)
        {
            if (_Position >= 1.0f) _Position = 1.0f;
            if (_Position <= 0.0f) _Position = 0.0f;
        }
        else
        {
            if (_Position >= 1.0f) _Position = _Position % 1.0f;
            if (_Position <= 0.0f) _Position = 1.0f - Mathf.Abs(_Position);
        }
    }
    public override TickOp update(TimeSpan t)
    {
        TickOp op = null;
        switch (State)
        {
            case EState.Manual:
                #if WDBG
                if(HasAdsorption)
                    TmpLog.log($"Adsorption Log Velocity = abs({Velocity}) <= {AdsorptionMinVelocity} &&\n" +
                               $"PositionInEdge = {PositionInEdge(MinAdsorptionRange,out var _edgeDir)} edgeDir = {_edgeDir} ");
                #endif
                if (HasAdsorption && !IsArrive(Position) && Mathf.Abs(Velocity) <= AdsorptionMinVelocity && 
                    PositionInEdge(MinAdsorptionRange,out var edgeDir) &&
                    SameDir(edgeDir,Velocity))
                {
                    State = EState.Adsorption;
                    AdsorptionVelocity = AutoVelocity * edgeDir;
                    TouchBorderToManual = true;
                }
                else
                {
                    ClearVelocity();
                }
                break;
            case EState.Adsorption:
            case EState.Auto:
                VelocityAuto = AdsorptionVelocity;
                if (!Loop)
                {
                    var arrive = IsArrive(Velocity, Position);
                    if (arrive)
                    {
                        // if (State == EState.Adsorption && !KillSelfAfterAdsorption)
                        // {
                        //     State = EState.Manual;
                        //     ForceNotOverflow = false;
                        // }
                        if (State == EState.Auto || KillSelfAfterAdsorption)
                        {
                            State = EState.Stop;
                            if (AutoKill) op += NextOnRmSelf;
                            op += OnArrive?.Invoke();
                        }
                        AdsorptionVelocity = 0.0f;
                    }
                }
                break;
        }
        return op;
    }

    protected bool PositionInEdge(float r,out float dir)
    {
        dir = Position > 0.5f ? 1.0f : -1.0f;
        return Position > 0.5f ? 1.0f - Position <= r : Position <= r;
    }
    public static bool SameDir(float a, float b)
    {
        return (a < 0.0f && b < 0.0f) || (a > 0.0f && b > 0.0f);
    }
    private void ClearVelocity()
    {
        Velocity = 0.0f;
    }
    protected virtual void OnChangeVelocity(bool manual)
    {
        var p = Position + (manual ? ManualFactor : AdsorptionFactor) * _Velocity;
        if (Mathf.Abs(p - Position) >= float.Epsilon)
        {
            if (State == EState.Manual) Position = p;
            else PositionAuto = p;
            if (!Loop)
            {
                var overflow = p - Position;
                if (ForceNotOverflow) overflow = 0;
                IsArrive(Velocity, Position,
                    (v, pos, dir) => OnTouchBorder(v,pos,overflow,manual));
            }
        }
    }
    protected bool IsArrive(float v,float p,Action<float,float,float> on = null)
    {
        if (v > 0.0f && Mathf.Abs(p - 1.0f) <= float.Epsilon)
        {
            on?.Invoke(v,p,1.0f);
            return true;
        }
        else if (v < 0.0f && Mathf.Abs(p - 0.0f) <= float.Epsilon)
        {
            on?.Invoke(v,p,-1.0f);
            return true;
        }
        return false;
    }
    protected bool IsArrive(float p,Action<float,float> on = null)
    {
        if (Mathf.Abs(p - 1.0f) <= float.Epsilon)
        {
            on?.Invoke(p,1.0f);
            return true;
        }
        else if (Mathf.Abs(p - 0.0f) <= float.Epsilon)
        {
            on?.Invoke(p,-1.0f);
            return true;
        }
        return false;
    }
    protected virtual void OnChangePos(float position,bool manual)
    {
        
    }
    protected virtual void OnTouchBorder(float velocity, float position,float over,bool manual)
    {
        if (TouchBorderToManual && State == EState.Adsorption)
        {
            State = EState.Manual;
            AdsorptionVelocity = 0.0f;
            TouchBorderToManual = false;
        }
    }
    public void SetForceAdsorption(bool killSelfAfterAdsorption = true)
    {
        State = EState.Adsorption;
        KillSelfAfterAdsorption = killSelfAfterAdsorption;
        if(PositionInEdge(0.5f,out var edgeDir))
            AdsorptionVelocity = AutoVelocity * edgeDir;
    }

}
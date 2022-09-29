using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace MT
{
    public struct Interpolator<T>
        where T : struct
    {
        public static T interp(T b, T e, float t)
        {
            {
                if (b is float bv && e is float ev)
                    return (T)(object)(bv + ((ev - bv) * t));
            }
            {
                if (b is Vector2 bv && e is Vector2 ev)
                    return (T)(object)(bv + ((ev - bv) * t));
            }
            {
                if (b is Vector3 bv && e is Vector3 ev)
                    return (T)(object)(bv + ((ev - bv) * t));
            }
            return default(T);
        }
    }

    public struct Assignment<O, T>
    {
        public O Obj;
        public Action<O, T> AssignAction;

        public Assignment(O obj, Action<O, T> assignAction)
        {
            Obj = obj;
            AssignAction = assignAction;
        }
        public T assignment(T t)
        {
            AssignAction?.Invoke(Obj,t);
            return t;
        }
    }

    public abstract class MTween : TickTick
    {
        public Ease EaseType = Ease.Linear;
        public float Elapsed = 0.0f;
        public float Duration = 1.0f;
        public float TimeScale = 1.0f;
        public bool AutoKill = true;
        public abstract void Apply();
        public float position
        {
            get => Elapsed;
            set
            {
                Elapsed = value * Duration;
                Apply();
            }
        }
        public override TickOp update(TimeSpan t)
        {
            var d = TimeScale * (float)t.TotalSeconds;
            if (Elapsed + d > Duration)
                Elapsed = Duration;
            else
                Elapsed += d;
            if(Mathf.Abs(d) > float.Epsilon)
                Apply();
            return AutoKill && Elapsed>= Duration ? new TickRmSelfOp() : null;
        }
    }
    public class ManualTween<O,T> : MTween
    where T:struct
    {
        public Assignment<O, T> Assign;
        public T Begin, End;

        public ManualTween(Assignment<O, T> assign, T begin, T end,float duration,Ease ease = Ease.Linear,float elapsed = 0.0f,bool apply = false)
        {
            Assign = assign;
            Begin = begin;
            End = end;
            Duration = duration;
            EaseType = ease;
            Elapsed = elapsed;
            if (apply)
                Apply();
        }

        public override void Apply()
        {
            Assign.assignment(Interpolator<T>.interp(Begin, End,
                EaseManager.Evaluate(EaseType, null, Elapsed, Duration, 0, 0)));
        }
    }

    public static class RectTransformMT
    {
        public static ManualTween<RectTransform, Vector2> MDoAnchorPos(this RectTransform self, Vector2 end, float dur,
            Ease ease = Ease.Linear)
        {
            return new ManualTween<RectTransform, Vector2>(
                new Assignment<RectTransform, Vector2>(self,(rt,pos)=> rt.anchoredPosition = pos),
                self.anchoredPosition,
                end,
                dur,
                ease
            );
        }
        public static ManualTween<Transform, Vector3> MDoScale(this Transform self, Vector3 end, float dur,
            Ease ease = Ease.Linear)
        {
            return new ManualTween<Transform, Vector3>(
                new Assignment<Transform, Vector3>(self,(rt,pos)=> rt.localScale = pos),
                self.localScale,
                end,
                dur,
                ease
            );
        }
        public static ManualTween<RectTransform, Vector2> MDoSize(this RectTransform self, Vector2 end, float dur,
            Ease ease = Ease.Linear)
        {
            return new ManualTween<RectTransform, Vector2>(
                new Assignment<RectTransform, Vector2>(self,(rt,pos)=> self.sizeDelta = pos),
                self.rect.size,
                end,
                dur,
                ease
            );
        }
        
        public static ManualTween<RectTransform, float> MDoRotateZ(this RectTransform self, float end, float dur,
            Ease ease = Ease.Linear)
        {
            return new ManualTween<RectTransform, float>(
                new Assignment<RectTransform, float>(self,(rt,z)=>
                {
                    var euler = self.rotation.eulerAngles;
                    euler.z = z;
                    self.rotation =  Quaternion.Euler(euler);
                }),
                self.rotation.eulerAngles.z,
                end,
                dur,
                ease
            );
        }
    }
}
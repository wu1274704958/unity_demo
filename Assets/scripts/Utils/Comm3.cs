
using System;
using System.Collections.Generic;
using UnityEngine;


public static class AniUtil
{
    public static void PlayAniWithFR(this TickGroup tickGroup, int sel, Animator a, Type F, Type R, bool isReverse = false, string key = "in",
        Action<Animator> onStart = null,TickTick next = null)
    {
        if (a == null) return;
        var progress = isReverse ? 1.0f : 0.0f;
        var reverseTag = TagNum.NumMap.MakeEx(isReverse ? F : R, sel);
        if (tickGroup.HasTag(reverseTag))
        {
            progress = a.GetAnimationProgress();
            tickGroup.ClearImmediateByTag(reverseTag);
        }
        PlayAniTick tk = new PlayAniTick(a, key, progress: progress, reverse: isReverse).SetPartOfName(true);
        if (onStart != null)
            tk.SetOnPlay(onStart);
        tk.SetNext(next);
        tickGroup.AddImmediate(tk.SetTag(TagNum.NumMap.MakeEx(isReverse ? R : F, sel)));

    }
}



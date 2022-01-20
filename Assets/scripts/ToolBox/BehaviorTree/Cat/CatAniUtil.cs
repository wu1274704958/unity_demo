using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CatAniUtil : MonoBehaviour
{
    private SkeletonAnimationUnion m_SkAnim;
    private SkeletonAnimation ani;
    private Dictionary<int, string> moveAni;//0 上 1 下
    private Dictionary<int, List<string>> idleAni;
    public int CurrentDir { get; set; }

    void Start()
    {
        ani = GetComponent<SkeletonAnimation>();
        SkeletonAnimation subAni = FindSub();
        m_SkAnim = new SkeletonAnimationUnion(GetComponent<SkeletonAnimation>(), subAni);
        moveAni = findMoveAni();
        idleAni = findIdleAni();
    }
    private SkeletonAnimation FindSub()
    {
        foreach (Transform child in transform)
        {
            var res = child.GetComponent<SkeletonAnimation>();
            if (res != null)
                return res;
        }

        return null;
    }
    private Dictionary<int, string> findMoveAni()
    {
        var anis = ani.AnimationState.Data.SkeletonData.Animations;
        var res = new Dictionary<int, string>();
        foreach (var a in anis)
        {
            if (a.Name.Contains("move"))
            {
                int i = a.Name.IndexOf("fx") + 2;
                if (!int.TryParse(a.Name.Substring(i, 1), out int id)) continue;
                res.Add(id, a.Name);
            }
        }
        return res;
    }

    private Dictionary<int, List<string>> findIdleAni()
    {
        var res = new Dictionary<int, List<string>>();
        FindAni(ani, "idle", (n, fx, id) =>
        {
            if (!res.ContainsKey(fx))
                res.Add(fx, new List<string>());
            res[fx].Add(n);
        });
        return res;
    }

    private void FindAni(SkeletonAnimation a,string key,Action<string,int,int> on)
    {
        var anis = a.AnimationState.Data.SkeletonData.Animations;
        var res = new Dictionary<int, string>();
        foreach (var it in anis)
        {
            if (it.Name.Contains(key))
            {
                int i = it.Name.IndexOf("fx") + 2;
                if (!int.TryParse(it.Name.Substring(i, 1), out int fx)) continue;
                i = it.Name.IndexOf(key) + key.Length;
                if (!int.TryParse(it.Name.Substring(i), out int id)) continue;
                on?.Invoke(it.Name, fx, id);
            }
        }
    }

    private int CalcDir(Vector3 t)
    {
        var p = GetComponent<Transform>().position;
        var dir = (new Vector2(t.x, t.z) - new Vector2(p.x, p.z)).normalized;
        float rad = 0;
        if (dir.y < 0)
        {
            rad = Vector2.Angle(new Vector2(dir.x, dir.y), new Vector2(-1, 0)) + 180;
        }
        else
        {
            rad = Vector2.Angle(new Vector2(dir.x, dir.y), new Vector2(1, 0));
        }
        rad += 45;
        rad %= 360;
        var d = (int)rad / 90;

        int animValue, flipValue = 0;
        CofUtils.GetAnimUpDownAndFlipByDirEx(d, out animValue, out flipValue);
        //print(string.Format("clac dir{4},{5} rad {0} d {1}  animValue {2} flipValue {3} ",rad,d,animValue,flipValue,dir.x,dir.y));
        var scale = GetComponent<Transform>().localScale;
        scale.x = flipValue * Mathf.Abs(scale.x);
        GetComponent<Transform>().localScale = scale;
        return CurrentDir = animValue;
    }

    public bool PlayMoveByTarget(Vector3 t)
    {
        var idx = CalcDir(t);
        if (!moveAni.ContainsKey(idx)) return false;
        if (ani.AnimationName == moveAni[idx])
        {
            return true;
        }
        ani.AnimationName = moveAni[idx];
        return true;
    }

    public bool PlayIdleByTarget(Vector3 t,int id = -1,bool keep = false)
    {
        var idx = CalcDir(t);
        return PlayIdle(idx,id,keep);
    }

    public bool PlayIdle(int dir, int id = -1, bool keep = false, List<string> exclude = null)
    {
        if (!idleAni.ContainsKey(dir)) return false;
        if (keep && ani.AnimationName.Contains("idle"))
            return true;
        List<string> arr = null;
        if (exclude != null && exclude.Count > 0)
        {
            arr = idleAni[dir].Where((a) => {
                bool f = true;
                for(int i = 0;i < exclude.Count;++i)
                {
                    if (a.Contains(exclude[i])) return false;
                }
                return f;
            }).ToList();
        }
        else
            arr = idleAni[dir];

        if (arr.Count == 0) return false;
        if (id < 0 || id >= arr.Count)
            id = RandomUti.GetRandomEx(0, arr.Count);
        ani.AnimationName = arr[id];
        return true;
    }

    public bool CurrentIsStop(float minDisparity = 0.01f)
    {
        var track = ani.AnimationState.GetCurrent(0);
        if (track == null) return true;
        return 1.0f - track.AnimationTime / track.AnimationEnd <= minDisparity;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using System;


public class WheelUI : MonoBehaviour
{
    public List<RectTransform> bindTrans;
    
    public List<Vector2> pos;
    public List<WheelUIAddedOnAni> Added;
    
    public float radius = 10.0f;
    private int num = 5;
    public float startDegrees = 0f;

    private float degress = 0f;

    public Vector3 offset = Vector3.zero;
    public float Degress { get => degress;}
    public float CurrDegress { get => currDegress; set => currDegress = value; }
    public int Num { get => num; set{
            num = value;
            degress = 360.0f / (float)num;
        }
    }

    private Transform trans;
    [SerializeField]
    private Vector3 startVector = new Vector3(1.0f, 0.0f, 0.0f);

    public RectTransform centerTrans;

    [SerializeField]
    private float currDegress = 0.0f;
    private TweenerCore<Quaternion, Vector3, QuaternionOptions> tween;
    private Action<float> m_OnTweenUpdate;
    private Action m_OnTweenComplete;
    private float m_TweenToDegress;


    // Start is called before the first frame update

    //void Start()
    //{
    //    bindTrans = new List<RectTransform>();
    //    InitPos(true);
    //}

    public void InitPos(bool initNullTrans = false)
    {
        if (initNullTrans) bindTrans = new List<RectTransform>();
        startVector = (Quaternion.Euler(0, 0, startDegrees) * new Vector3(1.0f, 0.0f, 0.0f)).normalized;
        degress = 360.0f / (float)num;
        pos = new List<Vector2>();

        float deg = CurrDegress;

        for(int i = 0; i < num;++i)
        {
            pos.Add(Quaternion.Euler(0, 0, deg) * startVector * radius + offset);
            deg += degress;
            if (initNullTrans) bindTrans.Add(null);
        }
    }

    public void UpdatePos()
    {
        float deg = CurrDegress;
        for (int i = 0; i < pos.Count; ++i)
        {
            pos[i] = Quaternion.Euler(0, 0, deg) * startVector * radius + offset;
            deg += degress;
        }
    }

    public void UpdatePosAndTrans()
    {
        float deg = CurrDegress;
        if (centerTrans != null) centerTrans.localRotation = Quaternion.Euler(0.0f, 0.0f, deg);
        SetVal(deg);
        for (int i = 0; i < pos.Count; ++i)
        {
            pos[i] = Quaternion.Euler(0, 0, deg) * startVector * radius + offset;
            if (i < bindTrans.Count && bindTrans[i] != null)
            {
                var p = bindTrans[i].anchoredPosition;
                p.x = pos[i].x;
                p.y = pos[i].y;
                bindTrans[i].anchoredPosition = p;
            }
            deg += degress;
        }
    }

    public void UpdateTransform()
    {
        if(bindTrans!=null && pos != null)
        {
            for(int i = 0;i < bindTrans.Count;++i)
            {
                var it = bindTrans[i];
                if (it == null) continue;
                var p = it.anchoredPosition;
                p.x = pos[i].x;
                p.y = pos[i].y;
                it.anchoredPosition = p;
            }
        }
    }

    public void AutoBind(bool hideOther = true,int max = 5,List<WheelUIAddedOnAni> added = null)
    {
        if (transform.childCount == 0) return;
        centerTrans = transform.GetChild(0) as RectTransform;
        Added = added;
        int i = 0;
        int l = (added == null || added.Count == 0) ? 1 : 1 + added.Count;
        if (l > 1)
        {
            for (int x = 0; x < added.Count;++x)
            {
                added[x]?.SetTrans(transform.GetChild(x + 1) as RectTransform);
            }
        }
        for(i = l;i < Math.Min(transform.childCount,bindTrans.Count + l);++i)
        {
            var t = transform.GetChild(i) as RectTransform;
            t.gameObject.SetActive(true);
            bindTrans[i - l] = t;
        }
        for(int m = Math.Min(transform.childCount,max + l);i < m;++i)
        {
            var t = transform.GetChild(i) as RectTransform;
            t.gameObject.SetActive(false);
        }
    }

    public bool Rotate(float dir,float t, bool force = false,Action<float> on_update = null, Action on_complete = null)
    {
        if (IsAnimation())
        {
            forceStopAni();return false;
        }
        var n = currDegress + (Degress * dir);
        Debug.LogWarning("currDegress " + currDegress + " Degress" + Degress + " " + num);
        if (n >= 360.0f || n <= -360.0f)
        {
            n = n % 360.0f;
        }
        {
            return RotateTo(n, t, on_update, on_complete);
        }
        
    }

    public bool RotateTo(float deg,float t, Action<float> on_update = null, Action on_complete = null)
    {
        if(centerTrans != null && tween == null)
        {
            RotateToFroce(deg, t, on_update, on_complete);
            return true;
        }
        return false;
    }
    
    public void RotateToFroce(float deg, float t,Action<float> on_update=null,Action on_complete=null)
    {
        m_OnTweenUpdate = on_update;
        m_OnTweenComplete = on_complete;
        m_TweenToDegress = deg;
        if (centerTrans == null)
        {
            Debug.LogWarning("RotateToFroce But Center Transform Is null!!!");
            return;
        }
        if (tween != null) {
            tween.Kill();
        }
        OnAniStart();
        tween = centerTrans.DOLocalRotate(new Vector3(0, 0, deg), t);
        tween.onUpdate = () =>
        {
            OnTweenUpdate();
            var p = tween.position / t;
            on_update?.Invoke(p);
            OnAni(p);
            Debug.Log("Tween position " + p );
        };

        tween.onComplete = () => {
            OnTweenComplete();
            on_complete?.Invoke();
            OnAniEnd();
        }; 
    }

    public bool IsAnimation()
    {
        return tween != null;
    }

    public void forceStopAni()
    {
        if(tween != null)
        {
            tween.Kill();
            var pos = centerTrans.eulerAngles;
            centerTrans.eulerAngles = new Vector3(pos.x,pos.y,m_TweenToDegress);
            OnTweenUpdate();
            m_OnTweenUpdate?.Invoke(1.0f);
            m_OnTweenComplete?.Invoke();
            tween = null;
        }
    }

    private void OnTweenUpdate()
    {
        currDegress = centerTrans.localRotation.eulerAngles.z;
        UpdatePosAndTrans();
    }
    private void OnTweenComplete()
    {
        tween = null;
        Debug.Log("OnTweenComplete ");
    }

    // Update is called once per frame
    void Update()
    {

    }

    public RectTransform Get(int idx)
    {
        if (idx >= 0 && idx < bindTrans.Count)
            return bindTrans[idx];
        else
            return null;
    }

    private void OnAniStart()
    {
        if(Added != null && Added.Count > 0)
        {
            var start = centerTrans.localRotation.eulerAngles.z;
            var dist = m_TweenToDegress - start;

            foreach (var a in Added)
            {
                a?.SetRange(start, m_TweenToDegress);
                a?.SetDist(dist);
                a?.OnStartAni();
            }
        }

    }
    private void OnAniEnd()
    {
        if (Added != null && Added.Count > 0)
        {
            foreach (var a in Added)
            {
                a?.OnEndAni();
            }
        }
    }
    private void OnAni(float p)
    {
        if (Added != null && Added.Count > 0)
        {
            foreach (var a in Added)
            {
                a?.OnAni(p);
            }
        }
    }
    private void SetVal(float v)
    {
        if (Added != null && Added.Count > 0)
        {
            foreach (var a in Added)
            {
                a?.SetVal(v);
            }
        }
    }


}

public abstract class WheelUIAddedOnAni
{
    protected RectTransform trans;
    protected float start, end, dist;
    public abstract void OnAni(float p);
    public abstract void OnStartAni();
    public abstract void OnEndAni();
    public virtual void SetRange(float start, float end)
    {
        this.start = start;
        this.end = end;
    }
    public virtual void SetDist(float dist)
    {
        this.dist = dist;
    }
    public virtual void SetTrans(RectTransform trans)
    {
        this.trans = trans;
    }
    public abstract void SetVal(float v);
}

public class DefWheelUIAddedOnAni : WheelUIAddedOnAni
{
    public float factor = 1;
    public Vector3 StartRot;

    public DefWheelUIAddedOnAni(float factor)
    {
        this.factor = factor;
    }

    public DefWheelUIAddedOnAni()
    {
    }

    public override void OnAni(float p)
    {
        if (trans != null)
        {
            var z = StartRot.z + p * factor * dist;
            trans.rotation = Quaternion.Euler(StartRot.x, StartRot.y, z);
        }
    }

    public override void OnEndAni()
    {
        
    }

    public override void OnStartAni()
    {
        if (trans != null)
            StartRot = trans.rotation.eulerAngles;
    }

    public override void SetVal(float v)
    {
        if (trans != null)
            trans.rotation = Quaternion.Euler(0.0f,0.0f, v * factor);
    }
}

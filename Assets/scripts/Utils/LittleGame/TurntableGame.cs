using System;
using System.Collections.Generic;
using lg;
using lt;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TurntableGame : MonoBehaviour
{
    public ArrangeData _ArrangeData = new ArrangeData();
    public MotionData MotionConf = new MotionData();
    [SerializeField] public List<Row> Rows = new List<Row>();
    private TickGroup _tickGroup = new TickGroup();
    [SerializeField]protected State m_State  = State.Idle;
    //运动时的临时数据
    [SerializeField]protected Vector2 LastPos  = Vector2.zero;
    [SerializeField]protected Vector2Int SelRealId  = new Vector2Int(Int32.MinValue,Int32.MinValue);
    [SerializeField]protected RectTransform SelectFragment = null;
    protected FragmentComp SelectFragComp;
    [SerializeField]protected List<RectTransform> InMotion = null;
    [SerializeField]protected int Direction = 0;
    protected KnobTick MotionTick = null;
    [SerializeField]protected OperatingData OperatingDat;
    //临时的
    protected List<RectTransform> RotatingArr = new List<RectTransform>();
    protected List<FragmentComp> RotatingCompArr = new List<FragmentComp>();
    //事件
    public TimeSpan DetermineDirTimeout { set; get; } = TimeSpan.FromSeconds(0.5f);
    public Action<RectTransform,Vector2Int> OnDetermineDirTimeout = null;
    public Action<Vector2Int,int> OnDetermineDirRes = null;
    public Action<OperatingData, bool> OnManualMotionEnd = null;
    public Action OnBtnUpFunc;
    public State _State
    {
        protected set
        {
            if(m_State == value) return;
            _tickGroup.ClearImmediateByTag(TagNum.NumMap.MakeEx(typeof(StateTag<>),(int)m_State));
            m_State = value;
        }
        get => m_State;
    }
    public bool CanPlay = true;
    private Shader m_Shader;
    private void Awake()
    {
        Input.multiTouchEnabled = false;
        m_Shader = Shader.Find("UI/KnobFragment");
        Init();
    }

    private void Start()
    {
        _tickGroup.Reset();
    }
#region 初始化
    public void Init(bool editorMode = false)
    {
        Rows = new List<Row>();
        for (int i = 0; i < transform.childCount; ++i)
        {
            var it = transform.GetChild(i);
            if (ParseFragment(it.gameObject, out Vector2Int pos))
            {
                 if (pos.x >= Rows.Count)
                    Rows.MakeUp(pos.x + 1);
                 if(pos.y >= Rows[pos.x].Fragments.Count)
                    Rows[pos.x].Fragments.MakeUpDef(pos.y + 1);
                 Rows[pos.x].Fragments[pos.y] = it as RectTransform;
                 if(!editorMode)InitFragment(it,pos);
            }
        }
        if(editorMode) return;
        for (int i = 0;i < Rows.Count;++i)
        {
            InitRow(i,Rows[i]);
        }
    }

    public void SetImage(string n)
    {
        foreach (var row in Rows)
        {
            for (int i = 0; i < row.Fragments.Count; ++i)
            {
                row.FragmentComps[i].img.sprite = AssetDepot.LoadSprite(GetImagePath(row.Id, i, n));
            }
        }
    }

    private void InitRow(int i, Row row)
    {
        var last = row.Fragments.Count - 1;
        ResetPlaceHolderImg(row.Fragments,row.FragmentComps);
        row.Id = i;
        row.Direction = (row.Fragments[0].GetPosFromPolarPos() - row.Fragments[last].GetPosFromPolarPos()).normalized;
        row.Angle = row.Fragments[0].localRotation.eulerAngles.z;
        row.Ends = new (Vector2,Vector2)[2]
        {
            (row.Fragments[0].GetPolarPos(),row.Fragments[0].rect.size),
            (row.Fragments[last].GetPolarPos(),row.Fragments[last].rect.size),
        };
    }

    private void InitFragment(Transform it, Vector2Int pos,bool addEvt = true)
    {
        var img = it?.GetComponent<KnobFragment>();
        if (img != null)
        {
            //img.alphaHitTestMinimumThreshold = 0.01f;
            if(img.material == null || img.material.shader.name != m_Shader.name)
                img.material = new Material(m_Shader);
            img.LineWeight = MotionConf.LineWeight;
            img.LineWeightAngle = MotionConf.LineWeightAngle;
            var data = it.GetOrAddComponent<DataCarrier>();
            data.SetCache("pos", pos);
            var btn = it.GetOrAddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            EventTrigger trigger = btn.transform.GetOrAddComponent<EventTrigger>();
            trigger.triggers.Clear();
            if (addEvt)
            {
                UIUtils.BtnPointerDownEvt(btn, (e) =>
                {
                    if((Input.touchCount == 0 && e is PointerEventData evt && evt.button == 0) || Input.touchCount == 1)
                        OnBtnDown(e, it as RectTransform, data);
                }, true);
                UIUtils.BtnPointerUpEvt(btn, (e) =>
                {
                    if((Input.touchCount == 0 && e is PointerEventData evt && evt.button == 0) || Input.touchCount == 1)
                        OnBtnUp(e, it as RectTransform, data);
                }, true);
            }
            if(pos.y >= Rows[pos.x].FragmentComps.Count) Rows[pos.x].FragmentComps.MakeUpDef(pos.y + 1);
            Rows[pos.x].FragmentComps[pos.y] = new FragmentComp() { img = img,btn = btn,data = data};
        }
    }
    #endregion

    #region 事件处理
    private void OnBtnDown(BaseEventData e, RectTransform t, DataCarrier data)
    {
        if(m_State != State.Idle || !CanPlay) return;
        SelectFragment = t;
        var pos = data.GetVal<Vector2Int>("pos");
        if(pos.y == 0 || pos.y == 9 || pos.y == 4 || pos.y == 5) return;
        SelectFragComp = GetFragmentComp(pos);
        _State = State.DetermineDir;
        SelRealId = pos;
        LastPos = Input.mousePosition;
        _tickGroup.AddImmediate(new DelayTask(()=>OnDetermineDirTimeout?.Invoke(SelectFragment,pos),DetermineDirTimeout,1)
            .SetTag(TagNum.NumMap.MakeEx(typeof(StateTag<>),(int)State.DetermineDir)));
    }
    private void OnBtnUp(BaseEventData e, RectTransform t, DataCarrier data)
    {
        if(!CanPlay ||  m_State == State.AutoMotion || m_State == State.WaitingMotionStop || m_State == State.Idle) return;
        ClearMotionState();
        OnBtnUpFunc?.Invoke();
    }

    public void ForceStopManualMotion()
    {
        if(!CanPlay ||  m_State == State.AutoMotion || m_State == State.WaitingMotionStop || m_State == State.Idle) return;
        ClearMotionState();
    }

    private void ClearMotionState()
    {
        SelectFragment = null;
        SelectFragComp = default;
        SelRealId = new Vector2Int(Int32.MinValue, Int32.MinValue);
        LastPos = Vector2.zero;
        InMotion = null;
        var st = State.Idle;
        if (MotionTick != null && _tickGroup.Contain(MotionTick))
        {
            st = State.WaitingMotionStop;
            MotionTick.SetForceAdsorption();
        }else
            MotionTick = null;
        _State = st;
    }

    private void DoDetermineDir()
    {
        var tdis = GetTranslateDistance((Vector2)Input.mousePosition,SelRealId,out Direction);
        var rdis = GetRotateDistance((Vector2)Input.mousePosition, SelRealId, out Direction);
        if (rdis > tdis && rdis >= MotionConf.DetermineDirMinDistanceForRotate)
        {
            var rot = GetRotIdx(SelRealId);
            InMotion = GetRotateRow(rot.x);
            MotionTick = MakeManualRotate(InMotion, rot.x,Direction);
            MotionTick.Velocity = rdis;
            _tickGroup.AddImmediate(MotionTick);
            _State = State.ManualMotion;
            OnDetermineDirRes?.Invoke(SelRealId,rot.x);
            BeginManualMotion();
        }else
        if (tdis > rdis && tdis >= MotionConf.DetermineDirMinDistanceForTranslate)
        {
            InMotion = Rows[SelRealId.x].Fragments;
            MotionTick = MakeManualTranslate(InMotion, SelRealId.x,Direction);
            MotionTick.Velocity = tdis;
            _tickGroup.AddImmediate(MotionTick);
            _State = State.ManualMotion;
            OnDetermineDirRes?.Invoke(SelRealId,0);
            BeginManualMotion();
        }
    }

    private void BeginManualMotion()
    {
        var isRot = MotionTick is KnobRotateTick;
        var rotId = isRot ? GetRotIdx(SelRealId) : SelRealId;
        OperatingDat.Type = isRot ? (byte)1 : (byte)0;
        OperatingDat.Rid = isRot ? SelRealId.x : rotId.x;
        OperatingDat.DirRes = 0;
        OperatingDat.Comps = isRot ? GetRotateRowComp(rotId.x) : Rows[rotId.x].FragmentComps;
        OperatingDat.Begin = new Vector2Int[OperatingDat.Comps.Count];
        for (int i = 0;i < OperatingDat.Comps.Count;++i)
            OperatingDat.Begin[i] = OperatingDat.Comps[i].Pos;
    }
    private void EndManualMotion(int dir = 0)
    {
        var isRot =  OperatingDat.Type == 1;
        var circle = isRot ? OperatingDat.Begin.Length : OperatingDat.Begin.Length - 4;
        OperatingDat.DirRes = (OperatingDat.DirRes + dir) % circle;
        var isComplete = IsComplete();
        OnManualMotionEnd?.Invoke(OperatingDat,isComplete);
        OperatingDat = default;
    }

    public bool IsComplete()
    {
        for (int i = 0; i < Rows.Count; ++i)
        {
            for (int j = 0; j < Rows[i].Fragments.Count; ++j)
            {
                if (j == 0 || j == 9 || j == 4 || j == 5) continue;
                if (!Rows[i].FragmentComps[j].Good) 
                    return false;
            }
        }
        return true;
    }

    private float GetTranslateDistance(Vector2 pos ,Vector2Int sel,out int dir)
    {
        var row = Rows[sel.x];
        var d = pos - LastPos;
        dir = 0;
        if (Mathf.Abs(Vector2.Dot(d.normalized, row.Direction)) > MotionConf.InTranslateVal)
        {
            var v = row.Direction * Vector2.Dot(d, row.Direction);
            dir = Vector2.Dot(v, row.Direction) > 0 ? 1 : -1;
            return v.magnitude;
        }
        return 0.0f;
    }
    public static float GetRadian(Vector2 v)
    {
        var c = Vector2.Dot(new Vector2(0, -1),new Vector2(v.x, v.y).normalized);
        var t = v.x < 0 ? Mathf.PI - Mathf.Acos(c) : Mathf.Acos(c);
        return (t / Mathf.Deg2Rad) + (v.x < 0 ? 180.0f : 0.0f);
    }
    private float GetRotateDistance(Vector2 pos ,Vector2Int sel,out int dir)
    {
        var mid = new Vector2(Screen.width, Screen.height) * 0.5f;
        var b = (LastPos - mid).normalized;
        var e = (pos - mid).normalized;
        var d = Mathf.Acos( Vector2.Dot(b, e)) / Mathf.Deg2Rad;
        if (float.IsNaN(d)) d = 0.0f;
        var lastR = GetRadian(b);
        var nowR = GetRadian(e);
        dir = lastR > 300.0f && nowR < 60 ? 1 : nowR > lastR ? 1 : -1;
#if WDBG
        TmpLog.log($"GetRotateDistance {b} {e} last = {lastR} now = {nowR} {d} dir = {dir}");
#endif
        return d;
    }

    #endregion
    
    #region 移动相关
    protected void ResetPlaceHolderImg(List<RectTransform> frags,List<FragmentComp> comps,int op = 3)
    {
        if ((op & 1) == 1)
        {
            var size = frags.Count;
            comps[0].img.sprite = comps[size - 2].img.sprite;
            comps[size - 1].img.sprite = comps[1].img.sprite;
        }
        if ((op & 2) == 2)
        {
            var mid = frags.Count / 2 - 1;
            comps[mid].img.sprite = comps[mid + 2].img.sprite;
            comps[mid + 1].img.sprite = comps[mid - 1].img.sprite;
        }
    }
    protected void ResetPlaceHolderImgRot(int rid)
    {
        if(rid > 1 && rid < _ArrangeData.SizeArr.Length - 2) return;
        var op = rid <= 1 ? 1 : 2;
        for (int i = 0; i < Rows.Count; ++i)
        {
            ResetPlaceHolderImg(Rows[i].Fragments,Rows[i].FragmentComps,op);
        }
    }

    protected void OnTranslateEnd(int dir, Row row)
    {
        var a = dir > 0 ? 0 : row.Fragments.Count - 1;
        var b = dir > 0 ? row.Fragments.Count - 1 : 0;
        var i = dir > 0 ? 1 : 0;
        row.Fragments[a].sizeDelta = row.Ends[i].Item2;
        row.Fragments[a].SetPolarPos(row.Ends[i].Item1);
        var tmp = row.Fragments[a];
        var tmpComp = row.FragmentComps[a];
        Action<int,int> f = (x,y) =>
        {
            row.Fragments[x] = row.Fragments[y];
            row.FragmentComps[x] = row.FragmentComps[y];
        };
        if (dir > 0)
        {
            for (int j = 1; j < row.Fragments.Count; ++j)
                f(j - 1, j);
        }
        else
        {
            for (int j = row.Fragments.Count - 2; j >= 0; --j)
                f(j + 1, j);
        }
        row.Fragments[b] = tmp;
        row.FragmentComps[b] = tmpComp;
        ResetRowData(row);
    }

    private void ResetRowData(Row row)
    {
        for (int i = 0; i < row.FragmentComps.Count; ++i)
            row.FragmentComps[i].data.SetCache("pos", new Vector2Int(row.Id, i));
    }
    private void ClearAutoMotion()
    {
        Direction = 0;
        _State = State.Idle;
        #if WDBG
        TmpLog.log("ClearAutoMotion()");
        #endif
    }

    protected void OnTranslateEnd(float velocity, float position, float over,bool manual,Row row)
    {
#if WDBG
        TmpLog.log($"OnTranslateEnd pos={position} v={velocity} over={over} manual={manual}");
#endif
        if (position >= 1.0)
        {
            OnTranslateEnd(Direction, row);
            ResetPlaceHolderImg(row.Fragments, row.FragmentComps);
        }
        if(m_State == State.ManualMotion || m_State == State.WaitingMotionStop)
            OnManualMotionStepEnd(position,Direction,over);
        if (m_State == State.ManualMotion)
        {
            if (over > 0 && position >= 1.0f)
                ((KnobTranslateTick)MotionTick).ReInitTweens(Rows[SelRealId.x].Fragments, Direction, over);
            if (over < 0 && position <= 0.0f)
            {
                Direction = -Direction;
                ((KnobTranslateTick)MotionTick).ReInitTweens(Rows[SelRealId.x].Fragments, Direction, Mathf.Abs(over));
            }
        }
    }

    private void OnManualMotionStepEnd(float position, int dir, float over)
    {
        if (position >= 1.0f)
            OperatingDat.DirRes += dir;
    }

    protected void OnRotateEnd(float velocity, float position, float over,bool manual,int rid)
    {
#if WDBG
        TmpLog.log($"OnRotateEnd pos={position} v={velocity} over={over} manual={manual}");
#endif
        if (position >= 1.0)
        {
            OnRotateEnd(Direction, rid);
            ResetPlaceHolderImgRot(rid);
        }
        if(m_State == State.ManualMotion || m_State == State.WaitingMotionStop)
            OnManualMotionStepEnd(position,Direction,over);
        if (m_State == State.ManualMotion)
        {
            if (over >= 0 && position >= 1.0f)
                ((KnobTranslateTick)MotionTick).ReInitTweens(GetRotateRow(rid), Direction, over);
            if (over <= 0 && position <= 0.0f)
            {
                Direction = -Direction;
                ((KnobTranslateTick)MotionTick).ReInitTweens(GetRotateRow(rid), Direction, Mathf.Abs(over));
            }
        }
    }

    private void OnRotateEnd(int dir, int rid)
    {
        var count = Rows.Count * 2;
        var b = dir > 0 ? 0 : count - 1;
        var a = dir > 0 ? count - 1 : 0;
        var tmp = GetRotFragment(rid,a);
        var tmpComp = GetRotFragmentComp(rid,a);
        Action<int,int> f = (x,y) =>
        {
            SetRotFragment(rid,x,GetRotFragment(rid,y));
            SetRotFragmentComp(rid,x,GetRotFragmentComp(rid,y));
        };
        if (dir < 0)
        {
            for (int i = 1; i < Rows.Count * 2; ++i)
                f(i - 1, i);   
        }
        else
        {
            for (int i = Rows.Count * 2 - 2; i >= 0; --i)
                f(i + 1, i);
        }
        SetRotFragment(rid,b,tmp);
        SetRotFragmentComp(rid,b,tmpComp);
        ResetRotRowData(rid);
    }

    private void ResetRotRowData(int rid)
    {
        for (int i = 0; i < Rows.Count * 2; ++i)
        {
            var (x, y) = GetIdxByRotIdx(rid,i);
            Rows[x].FragmentComps[y].data.SetCache("pos",new Vector2Int(x,y));
        }
    }

    #region 旋转辅助函数
        public (int, int) GetIdxByRotIdx(int rid, int i)
        {
            return (i % Rows.Count, i < Rows.Count ? rid : (_ArrangeData.SizeArr.Length * 2 - 1) - rid);
        }
        public Vector2Int GetRotIdx(Vector2Int idx)
        {
            return new Vector2Int(idx.y >= _ArrangeData.SizeArr.Length ? (_ArrangeData.SizeArr.Length - 1) - (idx.y - _ArrangeData.SizeArr.Length) : idx.y,
                 idx.y >= _ArrangeData.SizeArr.Length ? idx.x + _ArrangeData.NumOfRow : idx.x );
        }
        public RectTransform GetRotFragment(int rid, int i)
        {
            var (x,y) = GetIdxByRotIdx(rid, i);
            return Rows[x].Fragments[y];
        }
        public void SetRotFragment(int rid, int i,RectTransform t)
        {
            var (x,y) = GetIdxByRotIdx(rid, i);
            Rows[x].Fragments[y] = t;
        }
        public FragmentComp GetRotFragmentComp(int rid, int i)
        {
            var (x,y) = GetIdxByRotIdx(rid, i);
            return Rows[x].FragmentComps[y];
        }
        public void SetRotFragmentComp(int rid, int i,FragmentComp t)
        {
            var (x,y) = GetIdxByRotIdx(rid, i);
            Rows[x].FragmentComps[y] = t;
        }

    #endregion
    #endregion

    #region 生成相关
    private bool ParseFragment(GameObject obj, out Vector2Int pos)
    {
        string[] arr = null;
        pos = new Vector2Int(0, 0);
        if ((arr = obj.name.Split('_')) != null && arr.Length == 2 && int.TryParse(arr[0],out var x) && 
            int.TryParse(arr[1],out var y))
        {
            pos = new Vector2Int(x, y);
            return true;
        }
        return false;
    }

    public void Arrange()
    {
        float angle = _ArrangeData.StartAngle;
        for (int i = 0; i < _ArrangeData.NumOfRow; ++i)
        {
            if(i >= Rows.Count) Rows.MakeUpDef(i + 1);
            GenRow(Rows[i],angle,i);
            angle += _ArrangeData.Angle;
        }
    }

    private void GenRow(Row row, float angle, int rid)
    {
        if (row == null) row = new Row();
        for (int i = 0; i < _ArrangeData.SpaceArr.Length; ++i)
        {
            var sp = _ArrangeData.SpaceArr[i];
            int j = _ArrangeData.SpaceArr.Length - i - 1;
            int k = _ArrangeData.SpaceArr.Length + i;
            if(j >= row.Fragments.Count) row.Fragments.MakeUpDef(j + 1);
            if(k >= row.Fragments.Count) row.Fragments.MakeUpDef(k + 1);
            GenFragment(rid,j,row.Fragments[j],angle,_ArrangeData.SizeArr[i],sp);
            GenFragment(rid,k,row.Fragments[k],angle + 180,_ArrangeData.SizeArr[i],sp);
        }
    }

    private RectTransform CreateFragment(int rid,int fid)
    {
        var go = new GameObject($"{rid}_{fid}");
        go.AddComponent<RectTransform>().SetParent(transform);
        var frag = go.AddComponent<KnobFragment>();
        frag.Mode = 1;
        frag.Angle = _ArrangeData.Angle;
        frag.PointNum = _ArrangeData.FragmentPointNum;
        frag.material = new Material(m_Shader == null ? Shader.Find("UI/KnobFragment") : m_Shader );
        frag.LineWeight = MotionConf.LineWeight;
        frag.LineWeightAngle = MotionConf.LineWeightAngle;
        go.layer = 5;
        go.transform.localScale = Vector3.one;
        go.transform.localPosition = Vector3.zero;
        return go.transform as RectTransform;
    }

    public static Vector2Int GetImagePos(int rid,int fid)
    {
        bool mir = fid > 4;
        var _1 = !mir ? rid + 1 : rid + 4;
        fid = !mir ? 4 - fid : fid - 5;
        var _2 = fid == 0 ? 1 : fid == 4 ? 3 : fid;
        return new Vector2Int(_1, _2);
    }
    public virtual string GetImagePath(int rid,int fid,string n = "img_turntable")
    {
        var pos = GetImagePos(rid, fid);
        return $"NewRogueLike_turntable/{n}_{pos.x}_{pos.y}.png";
    }
    public virtual string GetCeneterImagePath(string n = "img_turntable")
    {
        return $"NewRogueLike_turntable/{n}_0.png";
    }
    public virtual Sprite GetImageSprite(int rid,int fid)
    {
        return AssetDepot.LoadSprite(GetImagePath(rid, fid,_ArrangeData.ImagePrefix));
    }
    public virtual Sprite GetCenterImageSprite()
    {
        return AssetDepot.LoadSprite(GetCeneterImagePath(_ArrangeData.ImagePrefix));
    }
    #if UNITY_EDITOR
    public virtual Sprite GetImageSpriteEditor(int rid,int fid)
    {
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/"+GetImagePath(rid, fid,_ArrangeData.ImagePrefix));
    }
    #else
    public virtual Sprite GetImageSpriteEditor(int rid,int fid)
    {
        return null;
    }
    #endif
    private void GenFragment(int rid,int  fid,RectTransform trans, float angle, Vector2 size, float space)
    {
        var isNew = trans == null;
        if (isNew) trans = CreateFragment(rid, fid);
        trans.sizeDelta = size;
        trans.SetPolarPos(new Vector2(space, angle));
        var frag = trans.GetComponent<KnobFragment>();
        frag.Mode = 1;
        frag.sprite = Application.isPlaying ? GetImageSprite(rid, fid) : GetImageSpriteEditor(rid,fid);
        if (!isNew) trans.name = $"{rid}_{fid}";
#if WDBG
        var pos = trans.GetPolarPos();
        Debug.Assert(Math.Abs(pos.x - space) < 0.001f && Math.Abs(pos.y - angle) < 0.001f,
            $"GetPolarPos {pos} ({space},{angle}) Assert Failed!!!");
#endif        
    }

    public void ExtractArrangeData()
    {
        Init(true);
        if(Rows.Count == 0) return;
        _ArrangeData.NumOfRow = Rows.Count;
        _ArrangeData.SizeArr = new Vector2[Rows[0].Fragments.Count / 2];
        _ArrangeData.SpaceArr = new float[Rows[0].Fragments.Count / 2];
        var last = Vector2.zero;
        for (int i = Rows[0].Fragments.Count / 2 - 1; i >= 0; --i)
        {
            int j = (_ArrangeData.SizeArr.Length - 1) - i;
            _ArrangeData.SizeArr[j] = Rows[0].Fragments[i].rect.size;
            _ArrangeData.SpaceArr[j] = (Rows[0].Fragments[i].anchoredPosition - last).magnitude;
        }
        _ArrangeData.Angle = 360.0f / _ArrangeData.NumOfRow;
    }
    #endregion

    // Update is called once per frame
    void Update()
    {
        _tickGroup.update();
        switch (m_State)
        {
            case State.Idle:
                break;
            case State.DetermineDir:
                DoDetermineDir();
                LastPos = Input.mousePosition;
                break;
            case State.ManualMotion:
                if (MotionTick is KnobRotateTick rt)
                {
                    var rdis = GetRotateDistance((Vector2)Input.mousePosition, SelRealId, out int dir);
                    rt.Velocity = KnobTick.SameDir(dir, Direction) ? rdis : -rdis;
                }else
                if (MotionTick is KnobTranslateTick kt)
                {
                    var v = GetTranslateDistance((Vector2)Input.mousePosition, SelRealId, out int dir);
                    kt.Velocity = KnobTick.SameDir(dir, Direction) ? v : -v;
                }
                LastPos = Input.mousePosition;
                break;
            case State.AutoMotion:
                break;
            case State.WaitingMotionStop:
                if (!_tickGroup.Contain(MotionTick))
                {
                    MotionTick = null;
                    _State = State.Idle;
                    EndManualMotion();
                }
                break;
        }
    }

    private void OnDisable()
    {
        _tickGroup.ClearEx();
    }

    public bool GoodIdx(Vector2Int pos)
    {
        if (pos.x < 0 || pos.y < 0 || pos.x >= Rows.Count || pos.y >= Rows[pos.x].Fragments.Count)
            return false;
        return true;
    }
    public RectTransform GetFragment(Vector2Int pos)
    {
        if (!GoodIdx(pos))
            return null;
        return Rows[pos.x].Fragments[pos.y];
    }
    public FragmentComp GetFragmentComp(Vector2Int pos)
    {
        if (!GoodIdx(pos))
            return default;
        return Rows[pos.x].FragmentComps[pos.y];
    }

    #region 数据

        [Serializable]
        public class Row
        {
            //相对于 0，1的角度
            [SerializeField]
            public float Angle;
            [SerializeField]
            public Vector2 Direction;
            [SerializeField]
            public List<RectTransform> Fragments;
            public int Id;
            public List<FragmentComp> FragmentComps;
            public (Vector2,Vector2)[] Ends;
            public Row()
            {
                Fragments = new List<RectTransform>();
                FragmentComps = new List<FragmentComp>();
            }
        }
        public struct FragmentComp
        {
            public Image img;
            public DataCarrier data;
            public Button btn;
            public Vector2Int Pos => data.GetVal<Vector2Int>("pos");
            public bool Good
            {
                get
                {
                    if (img == null || img.sprite == null) return false;
                    var arr = img.sprite.name.Split('_');
                    if (arr.Length >= 2 && int.TryParse(arr[arr.Length - 2], out int x) && int.TryParse(arr[arr.Length - 1], out int y))
                    {
                        var pos = Pos;
                        var imgPos = GetImagePos(pos.x,pos.y);
                        return imgPos.x == x && imgPos.y == y;
                    }
                    return false;
                }
            }
        }
        [Serializable]
        public struct ArrangeData
        {
            public Vector2[] SizeArr;
            public float[] SpaceArr;
            public float Angle;
            public int NumOfRow;
            public float StartAngle;
            public int FragmentPointNum;
            public string ImagePrefix;
        }
        public enum State
        {
            Idle,
            DetermineDir,
            ManualMotion,
            AutoMotion,
            WaitingMotionStop
        }
        [Serializable]
        public class MotionData
        {
            public float DetermineDirMinDistanceForRotate = 0.001f;
            public float DetermineDirMinDistanceForTranslate = 0.0001f;
            public float TranslateAdsorptionVelocity = 0.01f;
            public float TranslateVelocity = 0.01f;
            public float TranslateManualFactor = 1.0f;
            public float TranslateAdsorptionFactor = 1.0f;
            public float TranslateAdsorptionMinVelocity = 0.1f;
            public float RotateAdsorptionVelocity = 0.01f;
            public float RotateVelocity = 0.01f;
            public float RotateManualFactor = 1.0f;
            public float RotateAdsorptionFactor = 1.0f;
            public float RotateAdsorptionMinVelocity = 0.1f;
            public float MinAdsorptionRange = 0.1f;
            public int LineWeight = 40;
            public float LineWeightAngle = 1.0f;
            public float TranslateAutoVelocity = 0.15f;
            public float RotateAutoVelocity = 0.15f;
            public float InTranslateVal = 0.7f;
        }
        [Serializable]
        public struct OperatingData
        {
            public int DirRes;
            public byte Type;
            public int Rid;
            public Vector2Int[] Begin;
            public List<FragmentComp> Comps;
        }
        #endregion
    public KnobTick MakeManualTranslate(List<RectTransform> ts,int rid, int dir, TickTick next = null)
    {
        var tween = new KnobTranslateTick(ts,true,true,MotionConf.TranslateAdsorptionVelocity)
        {
            ManualFactor = MotionConf.TranslateManualFactor,
            AdsorptionFactor = MotionConf.TranslateAdsorptionFactor,
            AdsorptionMinVelocity = MotionConf.TranslateAdsorptionMinVelocity,
            MinAdsorptionRange = MotionConf.MinAdsorptionRange
        };
        var t = TagNum.NumMap.MakeEx(typeof(lg.KnobTranslateTag<>), rid);
        tween.onTouchBorder = (vel, pos, over,manual, _) => OnTranslateEnd(vel, pos, over,manual,Rows[rid]);
        tween.SetTag(t);
        tween.InitTweens(dir);
        tween.SetNext(next);
        return tween;
    }
    public KnobTick MakeManualRotate(List<RectTransform> ts,int rid, int dir, TickTick next = null)
    {
        var tween = new KnobRotateTick(_ArrangeData.Angle,ts,true,true,MotionConf.RotateAdsorptionVelocity)
        {
            ManualFactor = MotionConf.RotateManualFactor,
            AdsorptionFactor = MotionConf.RotateAdsorptionFactor,
            AdsorptionMinVelocity = MotionConf.RotateAdsorptionMinVelocity,
            MinAdsorptionRange = MotionConf.MinAdsorptionRange
        };
        var t = TagNum.NumMap.MakeEx(typeof(lg.KnobRotateTag<>), rid);
        tween.onTouchBorder = (vel, pos, over,manual, _) => OnRotateEnd(vel, pos, over,manual,rid);
        tween.SetTag(t);
        tween.InitTweens(dir);
        tween.SetNext(next);
        return tween;
    }
    public ProducableTick MakeTranslate(int rid, int dir, float velocity = -0.01f, TickTick next = null)
    {
        if (velocity < 0) velocity = MotionConf.TranslateVelocity;
        var t = TagNum.NumMap.MakeEx(typeof(lg.KnobTranslateTag<>), rid);
        next?.SetTag(t);
        var tween = new KnobTranslateTick(Rows[rid].Fragments, false, false, velocity);
        tween.onTouchBorder = (vel, pos, over,manual, _) => OnTranslateEnd(vel, pos, over,manual,Rows[rid]);
        tween.SetTag(t);
        tween.InitTweens(dir);
        tween.SetNext(next);
        tween.OnAddFunc = () =>
        {
            Direction = dir;
            _State = State.AutoMotion;
        };
        tween.OnRmFunc = ClearAutoMotion;
        return tween;
    }
    public List<RectTransform> GetRotateRow(int rid)
    {
        RotatingArr.Clear();
        for (int i = 0; i < Rows.Count * 2; ++i)
        {
            var (x, y) = GetIdxByRotIdx(rid,i);
            RotatingArr.Add(Rows[x].Fragments[y]);
        }
        return RotatingArr;
    }
    public List<FragmentComp> GetRotateRowComp(int rid)
    {
        RotatingCompArr.Clear();
        for (int i = 0; i < Rows.Count * 2; ++i)
        {
            var (x, y) = GetIdxByRotIdx(rid,i);
            RotatingCompArr.Add(Rows[x].FragmentComps[y]);
        }
        return RotatingCompArr;
    }
    public ProducableTick MakeRotate(int rid, int dir,float velocity = -0.01f, TickTick next = null)
    {
        if (velocity < 0) velocity = MotionConf.RotateVelocity;
        var t = TagNum.NumMap.MakeEx(typeof(lg.KnobRotateTag<>), rid);
        next?.SetTag(t);
        var tween = new KnobRotateTick(_ArrangeData.Angle,GetRotateRow(rid), false, false, velocity);
        tween.onTouchBorder = (vel, pos, over,manual, _) => OnRotateEnd(vel, pos, over,manual,rid);
        tween.SetTag(t);
        tween.InitTweens(dir);
        tween.SetNext(next);
        tween.OnAddFunc = () =>
        {
            Direction = dir;
            _State = State.AutoMotion;
        };
        tween.OnRmFunc = ClearAutoMotion;
        return tween;
    }
    public bool Translate(int rid, int dir, float velocity = -0.01f, TickTick next = null)
    {
        if (velocity <= 0) velocity = MotionConf.TranslateAutoVelocity;
        if (m_State != State.Idle || rid < 0 || rid >= Rows.Count) return false;
        var t = TagNum.NumMap.MakeEx(typeof(lg.KnobRotateTag<>), rid);
        int d = dir > 0 ? 1 : -1;
        for (int x = 0; x < Math.Abs(dir); ++x)
        {
            if(x == Math.Abs(dir) - 1)
                next = MakeTranslate(rid, d, velocity, next);
            else
            {
                var n = next;
                next = new TaskReproducible(() => _tickGroup.Add(MakeTranslate(rid, d, velocity, n)), TimeSpan.Zero, 1).SetTag(t);
            }
        }
        if (_tickGroup.HasTag(next.GetTag())) return false;
        _tickGroup.Add(next);
        return true;
    }
    public bool Rotate(int rid, int dir, float velocity = -0.01f, TickTick next = null)
    {
        if (velocity <= 0) velocity = MotionConf.RotateAutoVelocity;
        if (m_State != State.Idle || rid < 0 || rid >= _ArrangeData.SizeArr.Length) return false;
        var t = TagNum.NumMap.MakeEx(typeof(lg.KnobRotateTag<>), rid);
        int d = dir > 0 ? 1 : -1;
        for (int x = 0; x < Math.Abs(dir); ++x)
        {
            if(x == Math.Abs(dir) - 1)
                next = MakeRotate(rid, d, velocity, next);
            else
            {
                var n = next;
                next = new TaskReproducible(() => _tickGroup.Add(MakeRotate(rid, d, velocity, n)), TimeSpan.Zero, 1).SetTag(t);
            }
            
        }
        if (_tickGroup.HasTag(next.GetTag())) return false;
        _tickGroup.Add(next);
        return true;
    }
    public bool Rotate(List<int> rid, int dir, float velocity = -0.01f, TickTick next = null)
    {
        #if WDBG
        TmpLog.log($"Rotate {rid.toStr()} {dir}");
        #endif
        for (int i = 0; i < rid.Count; ++i)
        {
            if (!Rotate(rid[i], dir, velocity, i == 0 ? next : null))
                return false;
        }
        if(rid.Count == 0)
            _tickGroup.Add(next);
        return true;
    }
    public bool Translate(List<int> rid, int dir, float velocity = -0.01f, TickTick next = null)
    {
        #if WDBG
        TmpLog.log($"Translate {rid.toStr()} {dir}");
        #endif  
        for (int i = 0;i < rid.Count;++i)
        {
            if (!Translate(rid[i], dir, velocity, i == 0 ? next : null))
                return false;
        }
        if(rid.Count == 0)
            _tickGroup.Add(next);
        return true;
    }
    #region 测试相关
    #endregion
}


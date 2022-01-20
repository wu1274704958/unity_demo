using BehaviorDesigner.Runtime.Tasks;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[TaskCategory("【Cat】")]
public class MoveToTarget : Action
{
    public Vector3 pos { get {
            var tarSlot = GetComponent<TargetSlot>();
            if (tarSlot == null || tarSlot.target == null) return Vector3.zero;
            return tarSlot.target.GetTarget();
        } 
    }
    [UnityEngine.Tooltip("speed")]
    public float speed;
    public CatAniUtil ani;
    public bool ArrivePlayIdle = true;
    public List<string> exclude = new List<string>(new string[] { "idle3" });
    public int idleId = -1;
    public bool keep = true;
    public override void OnStart()
    {
        ani = GetComponent<CatAniUtil>();
    }
    public override TaskStatus OnUpdate()
    {
        if (ani == null ) return TaskStatus.Failure;
        if(IsArrive())
        {
            if(ArrivePlayIdle) ani.PlayIdle(ani.CurrentDir,idleId,keep,exclude);
            return TaskStatus.Success;
        }
        if (!ani.PlayMoveByTarget(pos)) return TaskStatus.Failure;
        MoveStep(speed);
        return TaskStatus.Running;
    }

    private void MoveStep(float speed)
    {
        var dir = (pos - GetComponent<Transform>().position).normalized;
        var np = GetComponent<Transform>().position;
        GetComponent<Transform>().position = np + dir * speed;
    }
    private bool IsArrive()
    {
        var off = pos - GetComponent<Transform>().position;
        return Mathf.Abs(off.magnitude) < 0.01;
    }
}

[TaskCategory("【Cat】")]
public class NextTarget : Action
{
    public override TaskStatus OnUpdate()
    {
        GetComponent<TargetSlot>()?.target?.next();
        return TaskStatus.Success;
    }
}


[TaskCategory("【Cat】")]
public class IsClicked : Conditional
{
    protected Camera cam;
    protected ColliderClick click;
    protected bool isClicked = false;

    public IsClicked()
    {
        click = new ColliderClick(1 << 10);
    }
    public override void OnStart()
    {
        cam = gameObject.transform.parent?.GetComponent<Camera>();
        if (cam == null) cam = CameraMgr.Instance.GetCamera(ECamType.MainCam);
        click.camera = cam;
        click.Clear();
        click.Add(GetComponent<Collider>());
        click.OnClick = OnClick;
    }

    private void OnClick(Collider obj)
    {
        isClicked = true;
    }

    public override TaskStatus OnUpdate()
    {
        isClicked = false;
        click.update();
        return isClicked ? TaskStatus.Success : TaskStatus.Failure;
    }
}

[TaskCategory("【Cat】")]
public class PlayIdle : Action
{
    public CatAniUtil ani;
    public int dir = -1;
    public int idleId = -1;
    public bool keep = false;
    public List<string> exclude = new List<string>( new string[] { "idle3" } );

    public override void OnStart()
    {
        ani = GetComponent<CatAniUtil>();
    }
    public override TaskStatus OnUpdate()
    {
        if (ani == null) return TaskStatus.Failure;
        int dir = this.dir == -1 ? ani.CurrentDir : this.dir;
        if (idleId == -1)
            ani.PlayIdle(dir, idleId, keep,exclude);
        else
            ani.PlayIdle(dir,idleId,keep);
        return TaskStatus.Success;
    }

}

[TaskCategory("【Cat】")]
public class WaitCurrentStop : Action
{
    public CatAniUtil ani;
    public float minDisparity = 0.01f;

    public override void OnStart()
    {
        ani = GetComponent<CatAniUtil>();
    }
    public override TaskStatus OnUpdate()
    {
        if (ani == null) return TaskStatus.Failure;
        
        return ani.CurrentIsStop(minDisparity) ? TaskStatus.Success : TaskStatus.Running; 
    }

}

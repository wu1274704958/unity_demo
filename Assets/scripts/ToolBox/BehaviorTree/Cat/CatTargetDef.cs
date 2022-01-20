using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatTargetDef : CatTarget
{
    [SerializeField]
    public List<Transform> points;
    public int CurrPoint = 0;

    public override Vector3 GetTarget()
    {
        if (points == null || CurrPoint < 0 || CurrPoint >= points.Count)
            return Vector3.zero;
        return points[CurrPoint].position;
    }

    public override void next()
    {
        CurrPoint += 1;
        if (CurrPoint >= points.Count)
            CurrPoint = 0;
            
    }
}
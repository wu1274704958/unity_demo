using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CatTargetRand : CatTarget
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
        var points = this.points.Where((a) => a != this.points[CurrPoint]).ToList();
        CurrPoint = RandomUti.GetRandomEx(0, points.Count);
    }
}
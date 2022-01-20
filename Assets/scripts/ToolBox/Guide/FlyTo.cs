using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;



public class FlyTo : MonoBehaviour
{
    public Transform target;
    public float dur = 0.3f;
    public float dir = -90.0f;
    public float len = 100.0f;
    public void flyTo(Action complete = null)
    {
        if (target != null)
            flyTo(target,complete);
    }
    public void flyTo(Transform t, Action complete = null)
    {
        flyTo(t.position,complete);
    }

    public void flyTo(Vector3 pos, Action complete = null)
    {
        var dir_o = Quaternion.Euler(0, 0, dir) * (pos - transform.position).normalized;

        var tween = transform.DOMove(pos, dur);
        tween.SetEase(Ease.Linear);
        tween.onUpdate = () =>
        {
            var p = tween.position / dur;
            var n = Mathf.Sin(p * Mathf.PI);
            if (n < 0) n = 0.0f;
            var a = transform.position + dir_o * len * n;
            transform.position = a;
        };
        tween.onComplete = ()=> {
            complete?.Invoke();
        };
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatTarget : MonoBehaviour
{
    public virtual Vector3 GetTarget()
    {
        return GetComponent<Transform>().position;
    }

    public virtual void next()
    {
        
    }
}



using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class First : MonoBehaviour
{

    interface GetId
    {
        int getId();
    }

    class Data : GetId
    {
        public Data(int a)
        {
            this.a = a;
        }
        int a = 0;
        public int getId()
        {
            return a;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        TimeUtils.Test();
        var l = new List<Data>();
        l.Add(new Data(1));
        l.Add(new Data(2));
        l.Add(new Data(3));

        IList ls = l;


        foreach (var d in ls)
        {
            Debug.Log("asdas " + (d as GetId).getId().ToString());
        }

        AnyArray a = new AnyArray("[i:123;  l:78;s:  哈哈哈 ;vec2 : 123,67;]");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

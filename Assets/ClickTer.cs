using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickTer : MonoBehaviour
{
    // Start is called before the first frame update
    private Camera camera;
    void Start()
    {
        camera = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Mouse0))
        {
            var ray = camera.ScreenPointToRay(Input.mousePosition);
            var objs = Physics.RaycastAll(ray,50f,1 << 8);
            var minDist = float.MaxValue;
            Vector3 pos = Vector3.zero;
           
            foreach(var hit in objs)
            {
                if(hit.distance < minDist)
                {
                    minDist = hit.distance;
                    pos = hit.point;
                }
            }

            Shader.SetGlobalVector("_HitPos", new Vector4(pos.x, pos.y, pos.z, 1));
            Debug.Log("hit pos " + pos);
        }
    }
}

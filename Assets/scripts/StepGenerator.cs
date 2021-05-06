using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class StepGenerator : MonoBehaviour
{

    public RenderTexture StepRT;
    public RenderTexture mTmpRT;
    public Material StepMat;

    private Vector3 LastPlayerPos;

    void OnEnable()
    {
        mTmpRT = RenderTexture.GetTemporary(StepRT.descriptor);
        LastPlayerPos = transform.position;

    }

    void OnDisable()
    {

    }

    void Start()
    {
        Graphics.Blit(null,StepRT, StepMat);
    }

    void Update()
    {
        
    }
}

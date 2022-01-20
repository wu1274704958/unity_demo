using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuideDevTool : MonoBehaviour
{
    public bool RemoveGuide(int realId)
    {
        return sys.RemoveGuide(realId);
    }

    public GuideSystem sys
    {
        get
        {
            return GuideSystem.Instance;
        }
    }
}

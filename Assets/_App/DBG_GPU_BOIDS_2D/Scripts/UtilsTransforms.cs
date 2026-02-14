using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UtilsTransforms
{
    public static Transform[] GetChildren(this Transform t)
    {
        if (t.childCount == 0) return null;
        Transform[] ts = new Transform[t.childCount];
        for (int i=0;i<t.childCount;i++)
        {
            ts[i] = t.GetChild(i);
        }
        return ts;
    }
}

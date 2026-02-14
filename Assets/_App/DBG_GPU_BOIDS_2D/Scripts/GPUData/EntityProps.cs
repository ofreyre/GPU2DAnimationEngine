using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct EntityProps
{
    public float walkSpeed;
    public float runSpeed;
    public float life;
}

public struct EntityInstanceProps
{
    public Vector3 direction;

    public override string ToString()
    {
        return direction.ToString();
    }
}

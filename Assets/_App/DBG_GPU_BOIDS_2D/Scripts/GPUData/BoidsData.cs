using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public struct Boid
{
    public float maxSpeed;
    public float maxForce;
    public float seekWeight;
    public float r;
    public float collider_x;
    public float collider_r;
    public float collider_h;
    public float walkSrcSpeed;
    public float runSrcSpeed;
    public float damage0;
    public float damage1;

    public int GetSize()
    {
        return sizeof(float) * 11;
    }
}

public struct BoidInstance
{
    public Vector2Int gridPos;
    //public Vector2Int chunkCoords;
    public float stamina;
    public Vector2 v;

    public int GetSize()
    {
        return sizeof(int) * 4 + sizeof(float) * 3;
    }
};

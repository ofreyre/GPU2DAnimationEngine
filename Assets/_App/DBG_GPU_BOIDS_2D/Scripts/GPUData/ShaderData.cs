using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public struct _GPUSprite
{
    public Vector2 uv;
    public Vector2 sizeuv;

    public _GPUSprite(GPUSprite sprite)
    {
        uv = sprite.uv;
        sizeuv = sprite.sizeuv;
    }
};

public struct _Entity
{
    public int nodesC;
    public int clipsStartI;

    public _Entity(Entity entity)
    {
        nodesC = entity.nodesC;
        clipsStartI = entity.clipsStartI;
    }
};

public struct _Clip
{
    public int frameStartI;
    public int framesC;
    public float frameDuration;

    public _Clip(Clip clip)
    {
        frameStartI = clip.frameStartI;
        framesC = clip.framesC;
        frameDuration = clip.frameDuration;
    }
};

public struct _Frame
{
    public float3x3 transformOS;
    public int spriteI;

    public _Frame(Frame frame)
    {
        //transformOS = new _Matrix3x3(frame.transformOS);

        transformOS = new float3x3(
            frame.transformOS._00,
            frame.transformOS._01,
            frame.transformOS._02,

            frame.transformOS._10,
            frame.transformOS._11,
            frame.transformOS._12,

            frame.transformOS._20,
            frame.transformOS._21,
            frame.transformOS._22
            );
        spriteI = frame.spriteI;
    }
};

public struct _EntityInstance
{
    public int entityI;
    public int currentClipI;
    public Vector4 transformWS;
    public float time;

    public _EntityInstance(EntityInstance instance)
    {
        entityI = instance.entityI;
        currentClipI = instance.currentClipI;
        transformWS = instance.transformWS;
        time = instance.time;
    }
}

public struct _NodeInstance
{
    public int entityInstanceI;
    public int order;

    public _NodeInstance(NodeInstance instance)
    {
        entityInstanceI = instance.entityInstanceI;
        order = instance.order;
    }
};

public struct _Matrix3x3
{
    public float m00, m01, m02, m10, m11, m12, m20, m21, m22;

    public _Matrix3x3(Matrix3x3 m)
    {
        m00 = m._00;
        m01 = m._01;
        m02 = m._02;
        m10 = m._10;
        m11 = m._11;
        m12 = m._12;
        m20 = m._20;
        m21 = m._21;
        m22 = m._22;
    }

    public override string ToString()
    {
        return
            m00 + ", " + m01 + ", " + m02 + "\n" +
            m10 + ", " + m11 + ", " + m12 + "\n" +
            m20 + ", " + m21 + ", " + m22;
    }

    public int GetSize()
    {
        return sizeof(float) * 9;
    }
}
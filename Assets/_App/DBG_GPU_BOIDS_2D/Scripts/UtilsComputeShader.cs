using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UtilsComputeShader
{
    public static int[] GetThreadGroups(ComputeShader shader, string kernel, Vector3Int dataLength)
    {
        int kernelHandle = shader.FindKernel(kernel);
        return GetThreadGroups(shader, kernelHandle, dataLength);
    }

    public static int[] GetThreadGroups(ComputeShader shader, int kernelHandle, Vector3Int dataLength)
    {
        shader.GetKernelThreadGroupSizes(kernelHandle, out uint threadGroupSize1, out uint threadGroupSize2, out uint threadGroupSize3);
        return new int[] {
               Mathf.CeilToInt( Mathf.Max(dataLength.x, 1.0f) / threadGroupSize1),
               Mathf.CeilToInt( Mathf.Max(dataLength.y, 1.0f) / threadGroupSize2),
               Mathf.CeilToInt( Mathf.Max(dataLength.z, 1.0f) / threadGroupSize3)
        };
    }

    public static int[] GetThreadGroups(ComputeShader shader, string kernel, Vector3Int dataLength, int[] data)
    {
        if(data == null)
        {
            data = GetThreadGroups(shader, kernel, dataLength);
        }
        else
        {
            int kernelHandle = shader.FindKernel(kernel);
            data = GetThreadGroups(shader, kernelHandle, dataLength, data);
        }
        return data;
    }

    public static int[] GetThreadGroups(ComputeShader shader, int kernelHandle, Vector3Int dataLength, int[] data)
    {
        if (data == null)
        {
            data = GetThreadGroups(shader, kernelHandle, dataLength);
        }
        else
        {
            shader.GetKernelThreadGroupSizes(kernelHandle, out uint threadGroupSize1, out uint threadGroupSize2, out uint threadGroupSize3);
            data[0] = Mathf.CeilToInt(Mathf.Max(dataLength.x, 1.0f) / threadGroupSize1);
            data[1] = Mathf.CeilToInt(Mathf.Max(dataLength.y, 1.0f) / threadGroupSize2);
            data[2] = Mathf.CeilToInt(Mathf.Max(dataLength.z, 1.0f) / threadGroupSize3);
        }
        return data;
    }

    public static ComputeBuffer GetArgsComputeBuffer()
    {
        return new ComputeBuffer(3, sizeof(int) * 3);
    }

    public static ComputeBuffer GetArgsComputeBuffer(int[] args)
    {
        ComputeBuffer argsBuffer = GetArgsComputeBuffer();
        argsBuffer.SetData(args);
        return argsBuffer;
    }

    public static ComputeBuffer GetArgsComputeBuffer(ComputeShader shader, string kernel, Vector3Int dataLength)
    {
        return GetArgsComputeBuffer(GetThreadGroups(shader, kernel, dataLength));
    }

    public static ComputeBuffer GetArgsComputeBuffer(ComputeShader shader, int kernelHandle, Vector3Int dataLength)
    {
        return GetArgsComputeBuffer(GetThreadGroups(shader, kernelHandle, dataLength));
    }

    public static int NextExpOf2(int N)
    {
        if (N <= 0) return 1;
        return (int)Mathf.Ceil(Mathf.Log(N) / Mathf.Log(2));
    }

    public static int PrevExpOf2(int N)
    {
        if (N < 1) return 0;
        return (int)Mathf.Floor(Mathf.Log(N) / Mathf.Log(2));
    }


    public static int NextPowerOf2(int N)
    {
        if (N <= 0) return 1;
        int logBase2 = (int)Mathf.Ceil(Mathf.Log(N) / Mathf.Log(2));
        return (int)Mathf.Pow(2, logBase2);
    }

    public static int PrevPowerOf2(int N)
    {
        if (N < 1) return 0;
        int logBase2 = (int)Mathf.Floor(Mathf.Log(N) / Mathf.Log(2));
        return (int)Mathf.Pow(2, logBase2);
    }

}

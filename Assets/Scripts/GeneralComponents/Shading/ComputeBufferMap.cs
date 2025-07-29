using System;
using System.Collections.Generic;
using UnityEngine;

public struct ComputeBufferGroup
{
    public List<GameObject> subscribers;
    public ComputeBuffer buffer;

    public ComputeBufferGroup(int count, int stride, GameObject subscriber)
    {
        subscribers = new List<GameObject>() { subscriber };

        buffer = new ComputeBuffer(count, stride);
    }

    public void SetBufferData(Array data)
    {
        buffer?.SetData(data);
    }

    public void ReleaseBuffer()
    {
        buffer?.Release();
    }

    public void AddSubscriber(GameObject subscriber)
    {
        subscribers?.Add(subscriber);
    }

    public void RemoveSubscriber(GameObject subscriber)
    {
        subscribers?.Remove(subscriber);
    }
}

public sealed class ComputeBufferMap
{
    private static Dictionary<string, ComputeBufferGroup> bufferMap =
        new Dictionary<string, ComputeBufferGroup>();

    static ComputeBufferMap() { }

    private ComputeBufferMap() { }

    public static ComputeBuffer CreateBuffer(
        string bufferName,
        int count,
        int stride,
        GameObject createdWith
    )
    {
        if (bufferMap.ContainsKey(bufferName))
        {
            return bufferMap[bufferName].buffer;
        }
        else
        {
            bufferMap[bufferName] = new ComputeBufferGroup(count, stride, createdWith);
            return bufferMap[bufferName].buffer;
        }
    }

    public static ComputeBuffer AddBufferDependency(string bufferName, GameObject subscriber)
    {
        if (bufferMap.ContainsKey(bufferName))
        {
            bufferMap[bufferName].AddSubscriber(subscriber);
            return bufferMap[bufferName].buffer;
        }
        return null;
    }

    public static void RemoveBufferDependency(string bufferName, GameObject subscriber)
    {
        if (bufferMap.ContainsKey(bufferName))
        {
            ComputeBufferGroup bufferGroup = bufferMap[bufferName];

            bufferGroup.RemoveSubscriber(subscriber);

            if (bufferGroup.subscribers.Count == 0)
            {
                bufferGroup.ReleaseBuffer();
                bufferMap.Remove(bufferName);
            }
        }
    }

    public static void AssignBufferData(string bufferName, Array data)
    {
        if (bufferMap.ContainsKey(bufferName))
        {
            bufferMap[bufferName].SetBufferData(data);
        }
    }

    public static void AssignBufferToComputerShader(
        string bufferName,
        ComputeShader shader,
        int kernalIndex
    )
    {
        if (bufferMap.ContainsKey(bufferName))
        {
            shader.SetBuffer(kernalIndex, bufferName, bufferMap[bufferName].buffer);
        }
    }

    public static void AssignBufferToMaterial(string bufferName, Material mat)
    {
        if (bufferMap.ContainsKey(bufferName))
        {
            mat.SetBuffer(bufferName, bufferMap[bufferName].buffer);
        }
    }
}

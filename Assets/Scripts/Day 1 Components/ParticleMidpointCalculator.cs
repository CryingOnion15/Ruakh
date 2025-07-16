using System.Collections.Generic;
using UnityEngine;

public class ParticleMidpointCalculator : MonoBehaviour
{
    public Mesh mesh;
    public ComputeShader compShader;

    protected ComputeBuffer midpointBuffer;

    protected List<Vector2> meshUvs = new List<Vector2>();
    protected Vector2[] midpoints;
    protected int[] meshTriIndices;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        meshTriIndices = mesh.triangles;
        mesh.GetUVs(0, meshUvs);

        int triangleCount = meshTriIndices.Length / 3;
        midpoints = new Vector2[triangleCount];

        // Create the Vertex Buffer, Triangle Buffer, and Color Buffers.
        midpointBuffer = ComputeBufferMap.CreateBuffer(
            "particleMidpoints",
            triangleCount,
            sizeof(float) * 3,
            gameObject
        );

        for (int i = 0; i < triangleCount; i++)
        {
            int triStart = i * 3;
            Vector2 uv1 = meshUvs[meshTriIndices[triStart]];
            Vector2 uv2 = meshUvs[meshTriIndices[triStart + 1]];
            Vector3 uv3 = meshUvs[meshTriIndices[triStart + 2]];

            float midX = uv1.x + uv2.x + uv3.x / 3;
            float midY = uv1.y + uv2.y + uv3.y / 3;

            midpoints[i] = new Vector2(midX, midY);
        }

        ComputeBufferMap.AssignBufferData("particleMidpoints", midpoints);
        ComputeBufferMap.AssignBufferToComputerShader("particleMidpoints", compShader, 0);
    }

    void OnDestroy()
    {
        ComputeBufferMap.RemoveBufferDependency("midpoints", gameObject);
    }
}

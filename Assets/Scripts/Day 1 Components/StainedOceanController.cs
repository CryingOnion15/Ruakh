using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class StainedOceanController : MonoBehaviour
{
    [Header("Mesh")]
    public Mesh oceanMesh;

    [Header("Shader")]
    //public ComputeShader compShader;
    public Material material;

    protected int triangleCount = 0;

    protected ComputeBuffer vertexBuffer;
    protected ComputeBuffer indicesBuffer;
    protected ComputeBuffer colorBuffer;
    protected ComputeBuffer uvBuffer;

    protected Dictionary<(Vector3, Vector3), List<int>> edgeMap;
    protected List<int>[] adjacencyList;

    protected Vector3[] meshVertices;
    protected int[] meshTriIndices;
    protected List<Vector2> meshUvs = new List<Vector2>();

    // Update is called once per frame
    void Update()
    {
        material.SetBuffer("colors", colorBuffer);
        material.SetBuffer("indices", indicesBuffer);

        // TODO update the vertices buffer to allign the to the local node position.
        material.SetBuffer("vertices", vertexBuffer);

        Graphics.DrawProcedural(
            material,
            new Bounds(Vector3.zero, Vector3.one * 10000),
            MeshTopology.Triangles,
            oceanMesh.triangles.Length
        );
    }

    void OnEnable()
    {
        edgeMap = new Dictionary<(Vector3, Vector3), List<int>>();

        meshVertices = oceanMesh.vertices;
        meshTriIndices = oceanMesh.triangles;

        int vertexCount = meshVertices.Length;
        triangleCount = meshTriIndices.Length / 3;

        // Create the Vertex Buffer, Triangle Buffer, and Color Buffers.
        vertexBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 3);
        indicesBuffer = new ComputeBuffer(meshTriIndices.Length, sizeof(int));
        colorBuffer = new ComputeBuffer(triangleCount, sizeof(int));
        uvBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 2);

        oceanMesh.GetUVs(0, meshUvs);

        // Set initial Buffer Data
        vertexBuffer.SetData(meshVertices);
        indicesBuffer.SetData(meshTriIndices);
        uvBuffer.SetData(meshUvs);

        int[] colors = new int[triangleCount];
        for (int i = 0; i < triangleCount; i++)
        {
            colors[i] = -1;
        }

        CreateAdjacencyList();
        SetColorAdjacency(colors);

        colorBuffer.SetData(colors);

        material.SetBuffer("colors", colorBuffer);
        material.SetBuffer("indices", indicesBuffer);
        material.SetBuffer("vertices", vertexBuffer);
        material.SetBuffer("uvs", uvBuffer);
    }

    void OnDisable()
    {
        indicesBuffer?.Release();
        colorBuffer?.Release();
        vertexBuffer?.Release();
    }

    void CreateAdjacencyList()
    {
        for (int tri = 0; tri < triangleCount; tri++)
        {
            int v1 = meshTriIndices[tri * 3];
            int v2 = meshTriIndices[tri * 3 + 1];
            int v3 = meshTriIndices[tri * 3 + 2];

            AddEdge(v1, v2, tri);
            AddEdge(v2, v3, tri);
            AddEdge(v3, v1, tri);
        }

        adjacencyList = new List<int>[triangleCount];
        for (int i = 0; i < triangleCount; i++)
        {
            adjacencyList[i] = new List<int>();
        }

        foreach (var entry in edgeMap.Values)
        {
            var tris = entry;
            if (tris.Count == 2)
            {
                int t1 = tris[0];
                int t2 = tris[1];
                adjacencyList[t1].Add(t2);
                adjacencyList[t2].Add(t1);
            }
        }
    }

    void AddEdge(int e1, int e2, int triIndex)
    {
        Vector3 vert1 = meshVertices[e1];
        Vector3 vert2 = meshVertices[e2];

        // Make a consistent reference to the edge.
        var edge = (VertexMin(vert1, vert2), VertexMax(vert1, vert2));

        if (!edgeMap.ContainsKey(edge))
        {
            edgeMap[edge] = new List<int>();
        }

        edgeMap[edge].Add(triIndex);
    }

    Vector3 VertexMin(Vector3 a, Vector3 b)
    {
        if (a.x < b.x) return a;
        if (a.x > b.x) return b;

        if (a.y < b.y) return a;
        if (a.y > b.y) return b;

        if (a.z < b.z) return a;
        return b;
    }

    Vector3 VertexMax(Vector3 a, Vector3 b)
    {
        if (a.x > b.x) return a;
        if (a.x < b.x) return b;

        if (a.y > b.y) return a;
        if (a.y < b.y) return b;

        if (a.z > b.z) return a;
        return b;
    }

    void SetColorAdjacency(int[] colors)
    {
        // TODO implement later to introduce randomness.
        // int startingPoint = Mathf.FloorToInt(Random.value * triangleCount);

        for (int i = 0; i < triangleCount; i++)
        {
            int tri = i;
            List<int> usedColors = new List<int>();

            foreach (var adjTri in adjacencyList[tri])
            {
                int color = colors[adjTri];
                if (color != -1)
                    usedColors.Add(color);
            }

            for (int color = 0; color < 4; color++)
            {
                if (!usedColors.Contains(color))
                {
                    colors[tri] = color;
                    break;
                }
            }
        }
    }
}

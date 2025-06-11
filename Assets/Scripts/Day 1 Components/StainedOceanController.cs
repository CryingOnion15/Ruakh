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

    [Header("Colors")]
    public Color color1;
    public Color color2;
    public Color color3;

    protected int triangleCount = 0;

    protected ComputeBuffer vertexBuffer;
    protected ComputeBuffer indicesBuffer;
    protected ComputeBuffer colorBuffer;

    protected Dictionary<(int, int), List<int>> edgeMap;
    protected List<int>[] adjacencyList;

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
        edgeMap = new Dictionary<(int, int), List<int>>();

        Vector3[] vertices = oceanMesh.vertices;
        int[] indices = oceanMesh.triangles;

        int vertexCount = vertices.Length;
        triangleCount = indices.Length / 3;

        // Create the Vertex Buffer, Triangle Buffer, and Color Buffers.
        vertexBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 3);
        indicesBuffer = new ComputeBuffer(indices.Length, sizeof(int));
        colorBuffer = new ComputeBuffer(triangleCount, sizeof(int));

        // Set initial Buffer Data
        vertexBuffer.SetData(vertices);
        indicesBuffer.SetData(indices);

        int[] colors = new int[triangleCount];
        for (int i = 0; i < triangleCount; i++)
        {
            colors[i] = -1;
        }

        CreateAdjacencyList(indices);
        SetColorAdjacency(colors);

        colorBuffer.SetData(colors);

        material.SetBuffer("colors", colorBuffer);
        material.SetBuffer("indices", indicesBuffer);
        material.SetBuffer("vertices", vertexBuffer);
    }

    void OnDisable()
    {
        indicesBuffer?.Release();
        colorBuffer?.Release();
        vertexBuffer?.Release();
    }

    void CreateAdjacencyList(int[] triIndecies)
    {
        for (int tri = 0; tri < triangleCount; tri++)
        {
            int v1 = triIndecies[tri * 3];
            int v2 = triIndecies[tri * 3 + 1];
            int v3 = triIndecies[tri * 3 + 2];

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
        // Make a consistent reference to the edge.
        var edge = (Mathf.Min(e1, e2), Mathf.Max(e1, e2));

        if (!edgeMap.ContainsKey(edge))
        {
            edgeMap[edge] = new List<int>();
        }

        edgeMap[edge].Add(triIndex);
    }

    void SetColorAdjacency(int[] colors)
    {
        int startingPoint = Mathf.FloorToInt(Random.value * triangleCount);

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

        for (int i = 0; i < triangleCount; i++)
        {
            Debug.Log(colors[i]);
        }
    }
}

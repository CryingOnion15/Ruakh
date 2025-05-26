using UnityEngine;

[ExecuteAlways]
public class StainedOceanController : MonoBehaviour
{
    [Header("Mesh")]
    public Mesh oceanMesh;

    [Header("Shader")]
    public ComputeShader compShader;
    public Material material;

    protected int triangleCount = 0;

    protected ComputeBuffer vertexBuffer;
    protected ComputeBuffer indicesBuffer;
    protected ComputeBuffer colorBuffer;

    // // Start is called once before the first execution of Update after the MonoBehaviour is created
    // void Start()
    // {

    // }

    // Update is called once per frame
    void Update()
    {
        int kernelIndex = compShader.FindKernel("CSMain");

        // Dispatch with enough thread groups to cover all triangles
        int threadGroups = Mathf.CeilToInt(triangleCount / 64f);
        compShader.Dispatch(kernelIndex, threadGroups, 1, 1);
    }

    void OnEnable()
    {
        int kernel = compShader.FindKernel("CSMain");

        Vector3[] vertices = oceanMesh.vertices;
        int[] indices = oceanMesh.triangles;

        int vertexCount = vertices.Length;
        triangleCount = indices.Length / 3;

        // Create the Vertex Buffer, Triangle Buffer, and Color Buffers.
        vertexBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 3);
        indicesBuffer = new ComputeBuffer(indices.Length, sizeof(int));
        colorBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 4);

        // Set initial Buffer Data
        vertexBuffer.SetData(vertices);
        indicesBuffer.SetData(indices);

        Color[] colors = new Color[vertexCount];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = Color.white;

        colorBuffer.SetData(colors);

        // Set Buffer Data
        compShader.SetBuffer(kernel, "indices", indicesBuffer);
        compShader.SetBuffer(kernel, "colors", colorBuffer);

        material.SetBuffer("colors", colorBuffer);
        material.SetBuffer("vertices", vertexBuffer);
    }

    void OnDisable()
    {
        indicesBuffer?.Release();
        colorBuffer?.Release();
        vertexBuffer?.Release();
    }
}

using UnityEngine;

[ExecuteAlways]
public class StainedOceanController : MonoBehaviour
{
    [Header("Mesh")]
    public Mesh oceanMesh;

    [Header("Shader")]
    public ComputeShader compShader;
    public Material material;

    [Header("Colors")]
    public Color color1;
    public Color color2;
    public Color color3;

    protected int triangleCount = 0;

    protected ComputeBuffer vertexBuffer;
    protected ComputeBuffer indicesBuffer;
    protected ComputeBuffer colorBuffer;

    // Update is called once per frame
    void Update()
    {
        int kernelIndex = compShader.FindKernel("CSMain");

        // Dispatch with enough thread groups to cover all triangles
        int threadGroups = Mathf.CeilToInt(triangleCount / 64f);
        compShader.Dispatch(kernelIndex, threadGroups, 1, 1);

        material.SetBuffer("colors", colorBuffer);
        material.SetBuffer("indices", indicesBuffer);
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
        // VFX
        //vfx.SetMesh("OceanMesh", oceanMesh);

        Vector3[] vertices = oceanMesh.vertices;
        int[] indices = oceanMesh.triangles;

        int vertexCount = vertices.Length;
        triangleCount = indices.Length / 3;

        // Create the Vertex Buffer, Triangle Buffer, and Color Buffers.
        vertexBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 3);
        indicesBuffer = new ComputeBuffer(indices.Length, sizeof(int));
        colorBuffer = new ComputeBuffer(triangleCount, sizeof(float) * 4);

        // Set initial Buffer Data
        vertexBuffer.SetData(vertices);
        indicesBuffer.SetData(indices);

        Color[] colors = new Color[triangleCount];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = Color.white;

        colorBuffer.SetData(colors);

        int kernel = compShader.FindKernel("CSMain");

        // Set Buffer Data
        compShader.SetBuffer(kernel, "indices", indicesBuffer);
        compShader.SetBuffer(kernel, "colors", colorBuffer);
        compShader.SetVector("color1", color1);
        compShader.SetVector("color2", color2);
        compShader.SetVector("color3", color3);

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
}

using System.Collections.Generic;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.Rendering;

public class Day3RaycastController : MonoBehaviour
{
    public LayerMask layerMask;
    public Mesh meshData;

    public Material material;
    public ComputeShader compShader;
    public float colorFadeTime = 2f;

    public RenderTexture renderTexture;

    protected ComputeBuffer compBuffer;

    protected List<int> modifiedIndices = new List<int>();

    //protected RenderTexture renderTexture;

    // This assumes a 1024x1024 texture.
    protected int kernelIndex = 0;
    protected int dispatchX = 0;
    protected int dispatchY = 0;
    protected int bufferCount;
    protected int[] dataArray;

    protected int debugCounter = 0;

    void Start()
    {
        // renderTexture = new RenderTexture(1024, 1024, 0);
        // renderTexture.enableRandomWrite = true;
        // renderTexture.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat;
        // renderTexture.Create();

        bufferCount = renderTexture.width * renderTexture.height;

        //Initialize the buffer and set it to the shader.
        compBuffer = new ComputeBuffer(bufferCount, sizeof(int));

        /****** DEBUGGING ***/
        // int[] testData = new int[1024 * 1024];
        // testData[512 * 1024 + 512] = 1; // Center pixel = 1

        // kernelIndex = compShader.FindKernel("CSMain");

        // compShader.SetBuffer(kernelIndex, "dataBuffer", compBuffer);

        // compShader.SetTexture(kernelIndex, "Result", debugRenderTexture);

        // int[] readBack = new int[bufferCount];
        // compBuffer.SetData(testData);
        // compBuffer.GetData(readBack);
        // Debug.Log(readBack[512 * 1024 + 512]);
        // compShader.Dispatch(kernelIndex, 1024 / 16, 1024 / 16, 1);

        /********************/

        kernelIndex = compShader.FindKernel("CSMain");

        compShader.SetBuffer(kernelIndex, "dataBuffer", compBuffer);

        // Set the texture to the shader.
        compShader.SetTexture(kernelIndex, "Result", renderTexture);

        compShader.SetInt("textureWidth", renderTexture.width);

        //Set the dispatch size.
        dispatchX = Mathf.CeilToInt(renderTexture.width / 16f);
        dispatchY = Mathf.CeilToInt(renderTexture.height / 16f);

        dataArray = new int[bufferCount];
        System.Array.Clear(dataArray, 0, bufferCount);
        //Make sure the whole buffer is set to zero.
        compBuffer.SetData(dataArray);

        compShader.SetBuffer(kernelIndex, "dataBuffer", compBuffer);

        material.SetTexture("_DepthTexture", renderTexture);
    }

    void Update()
    {
        // dataArray[debugCounter] = 1;
        // debugCounter = (debugCounter + 1) % bufferCount;
        // compBuffer.SetData(dataArray);
        // compShader.Dispatch(kernelIndex, 1024 / 16, 1024 / 16, 1);
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -transform.up, out hit, Mathf.Infinity, layerMask))
        {
            var uv1 = meshData.uv[meshData.triangles[hit.triangleIndex * 3]];
            var uv2 = meshData.uv[meshData.triangles[hit.triangleIndex * 3 + 1]];
            var uv3 = meshData.uv[meshData.triangles[hit.triangleIndex * 3 + 2]];

            Vector3 vert1 = meshData.vertices[meshData.triangles[hit.triangleIndex * 3]];
            Vector3 vert2 = meshData.vertices[meshData.triangles[hit.triangleIndex * 3 + 1]];
            Vector3 vert3 = meshData.vertices[meshData.triangles[hit.triangleIndex * 3 + 2]];

            vert1 = hit.collider.transform.TransformPoint(vert1);
            vert2 = hit.collider.transform.TransformPoint(vert2);
            vert3 = hit.collider.transform.TransformPoint(vert3);
            Debug.DrawLine(vert1, vert2, Color.blue, 5f);
            Debug.DrawLine(vert2, vert3, Color.blue, 5f);
            Debug.DrawLine(vert3, vert1, Color.blue, 5f);

            SetPixelsToBuffer(uv1);
            SetPixelsToBuffer(uv2);
            SetPixelsToBuffer(uv3);

            // Set the delta float.
            compShader.SetFloat("delta", Time.deltaTime / colorFadeTime);

            // Update write to the buffer.
            compBuffer.SetData(dataArray);
            compShader.SetBuffer(kernelIndex, "dataBuffer", compBuffer);

            // Dispatch the shader. Assumes [16,16,1]
            compShader.Dispatch(kernelIndex, dispatchX, dispatchY, 1);
        }
    }

    // Add the star pattern of the pixels indices to true in the buffer.
    protected void SetPixelsToBuffer(Vector2 uv)
    {
        var centerX = Mathf.FloorToInt(uv.x * renderTexture.width);
        if (centerX == renderTexture.width)
            centerX = renderTexture.width - 1;

        var centerY = Mathf.FloorToInt(uv.y * renderTexture.height);
        if (centerY == renderTexture.height)
            centerY = renderTexture.height - 1;

        setRowOfPixels(centerY, centerX);

        // Add a row of pixels 2 down of center.
        if (centerY - 2 >= 0)
            setRowOfPixels(centerY - 2, centerX);

        // Add a row of pixels 1 down of center.
        if (centerY - 1 >= 0)
            setRowOfPixels(centerY - 2, centerX);

        // Add a row of pixels 1 up of center.
        if (centerY + 1 < renderTexture.height)
            setRowOfPixels(centerY + 1, centerX);

        // Add a row of pixels 2 up of center.
        if (centerY + 2 < renderTexture.height)
            setRowOfPixels(centerY + 2, centerX);
    }

    protected void setRowOfPixels(int y, int centerX)
    {
        int yOffset = y * renderTexture.width;

        for (int i = centerX - 2; i <= centerX + 2; i++)
        {
            if (i >= 0 && i < renderTexture.width)
            {
                dataArray[yOffset + i] = 1;
            }
        }
    }

    void OnDestroy()
    {
        compBuffer.Release();
    }
}

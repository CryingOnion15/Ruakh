using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ParticleMidpointCalculator : MonoBehaviour
{
    public Mesh mesh;
    public ComputeShader compShader;
    public Camera cameraRef;
    public int midpointsToCreate = 500;

    protected ComputeBuffer midpointBuffer;

    protected List<Vector2> meshUvs = new List<Vector2>();
    protected List<Vector2> validMidpoints = new List<Vector2>();
    protected Vector3[] midpoints;
    protected int[] meshTriIndices;
    protected float cameraDistance;
    protected float frustrumHeight;
    protected float frustrumWidth;
    protected float halfHeight;
    protected float halfWidth;

    protected Matrix4x4 inverseView;
    protected Matrix4x4 inverseProjetion;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Vector3 direction = transform.position - cameraRef.transform.position;
        cameraDistance = Vector3.Dot(cameraRef.transform.forward, direction);
        frustrumHeight =
            2.0f * cameraDistance * Mathf.Tan(cameraRef.fieldOfView * 0.5f * Mathf.Deg2Rad);
        frustrumWidth = frustrumHeight * cameraRef.aspect;

        halfWidth = frustrumWidth / 2;
        halfHeight = frustrumHeight / 2;

        inverseView = Matrix4x4.Inverse(cameraRef.worldToCameraMatrix);
        inverseProjetion = Matrix4x4.Inverse(cameraRef.projectionMatrix);

        Debug.Log("Height: " + frustrumHeight);
        Debug.Log("Width: " + frustrumWidth);

        meshTriIndices = mesh.triangles;
        mesh.GetUVs(0, meshUvs);

        int triangleCount = meshTriIndices.Length / 3;

        for (int i = 0; i < triangleCount; i++)
        {
            int triStart = i * 3;
            Vector2 uv1 = meshUvs[meshTriIndices[triStart]];
            Vector2 uv2 = meshUvs[meshTriIndices[triStart + 1]];
            Vector3 uv3 = meshUvs[meshTriIndices[triStart + 2]];

            if (uv1.y >= .5 && uv2.y >= .5 && uv3.y >= .5)
            {
                float midX = uv1.x + uv2.x + uv3.x / 3;
                float midY = uv1.y + uv2.y + uv3.y / 3;
                validMidpoints.Add(new Vector2(midX, midY));
            }

            //midpoints[i] = new Vector2(midX, midY);
        }

        int midpointsLength = (int)Mathf.Max(midpointsToCreate, validMidpoints.Count);
        midpoints = new Vector3[midpointsLength];

        // Create the Vertex Buffer, Triangle Buffer, and Color Buffers.
        midpointBuffer = ComputeBufferMap.CreateBuffer(
            "midpointsBuffer",
            midpointsLength,
            sizeof(float) * 3,
            gameObject
        );

        for (int i = 0; i < midpointsLength; i++)
        {
            if (i < validMidpoints.Count)
            {
                midpoints[i] = GetFrustrumLocation(validMidpoints[i]);
            }
            else
            {
                int randomIndex = Mathf.FloorToInt(Random.value * validMidpoints.Count - 1);
                midpoints[i] = GetFrustrumLocation(validMidpoints[randomIndex]);
            }
        }

        ShuffleMidpoints(midpoints);
        //DEBUGPrintMidpoints(midpoints);

        ComputeBufferMap.AssignBufferData("midpointsBuffer", midpoints);
        ComputeBufferMap.AssignBufferToComputerShader("midpointsBuffer", compShader, 0);
    }

    void ShuffleMidpoints(Vector3[] midpoints)
    {
        for (int i = 0; i < midpoints.Length; i++)
        {
            int index = Random.Range(0, i + 1);
            Vector2 value = midpoints[index];
            midpoints[index] = midpoints[i];
            midpoints[i] = value;
        }
    }

    Vector3 GetFrustrumLocation(Vector2 uv)
    {
        float x = (uv.x - .5f) * frustrumWidth;
        float y = (uv.y - .5f) * frustrumHeight;
        return cameraRef.transform.position + cameraRef.transform.rotation * (new Vector3(x, y, cameraDistance) / 10);
    }

    void DEBUGPrintMidpoints(Vector3[] midpoints)
    {
        for (int i = 0; i < midpoints.Length; i++)
        {
            Debug.Log(midpoints[i]);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (validMidpoints.Count == 0)
        {
            Vector3 worldPos = new Vector3(0, 0, -20);
            Gizmos.DrawSphere(worldPos, 20f);
            return;
        }
        else
        {
            Vector3 worldPos = new Vector3(0, 0, -20);
            Gizmos.DrawSphere(worldPos, 20f);
            return;
        }

        
        foreach (var uv in validMidpoints)
        {
            Vector3 worldPos = GetFrustrumLocation(uv);
            Gizmos.DrawSphere(worldPos, 1f);
        }
    }


    void OnDestroy()
    {
        ComputeBufferMap.RemoveBufferDependency("midpoints", gameObject);
    }
}

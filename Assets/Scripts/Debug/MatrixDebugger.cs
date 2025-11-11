using UnityEngine;

[ExecuteInEditMode]
public class MatrixDebugger : MonoBehaviour
{
    [SerializeField]
    protected GameObject[] objs;

    protected Matrix4x4 VPMatrix;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        VPMatrix = Shader.GetGlobalMatrix("_StainedShadowVPMatrix");
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var obj in objs)
        {
            Vector3 pos = obj.transform.position;
            Vector4 lightPosition = VPMatrix * new Vector4(pos.x, pos.y, pos.z, 1.0f);

            lightPosition.x /= lightPosition.w;
            lightPosition.y /= lightPosition.w;
            lightPosition.z /= lightPosition.w;

            LogUV(lightPosition, obj.name);
        }
    }

    void LogUV(Vector3 lightClip, string objectName) {
        Debug.Log(objectName + ":");
        Debug.Log($"U: {lightClip.x * 0.5f + 0.5f}, V: {lightClip.y * 0.5f + 0.5f}");
    }
}

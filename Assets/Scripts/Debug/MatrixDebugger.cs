using Unity.Mathematics;
using UnityEngine;

[ExecuteInEditMode]
public class OrthoLightFrustumDebugger : MonoBehaviour
{
    [Header("References")]
    public Camera gameCamera;
    public Light directionalLight;

    [Header("Debug Options")]
    public Color lineColor = Color.red;
    public float lineDuration = 0f; // 0 = frame only

    // Internal array for 8 world-space frustum corners
    private Vector3[] orthoCorners = new Vector3[8];

    void Update()
    {
        if (
            gameCamera == null
            || directionalLight == null
            || directionalLight.type != LightType.Directional
        )
            return;

        var camToWorld = gameCamera.transform.localToWorldMatrix;

        // Step 1: Get the 8 corners of the camera frustum in world space
        Vector3[] camCorners = new Vector3[8];

        Vector3[] tempCorners = new Vector3[4];
        gameCamera.CalculateFrustumCorners(
            new Rect(0, 0, 1, 1),
            gameCamera.nearClipPlane,
            Camera.MonoOrStereoscopicEye.Mono,
            tempCorners
        );

        // Convert to world space
        for (int i = 0; i < 4; i++)
            camCorners[i] = camToWorld.MultiplyPoint3x4(tempCorners[i]); // far plane
        gameCamera.CalculateFrustumCorners(
            new Rect(0, 0, 1, 1),
            gameCamera.farClipPlane,
            Camera.MonoOrStereoscopicEye.Mono,
            tempCorners
        );

        for (int i = 4; i < 8; i++)
            camCorners[i] = camToWorld.MultiplyPoint3x4(tempCorners[i - 4]); // near plane

        // Step 2: Build a light view matrix (look along -light direction)
        Vector3 right = directionalLight.transform.right;
        Vector3 up = directionalLight.transform.up;
        Vector3 forward = directionalLight.transform.forward;

        Vector3 center = Vector3.zero;
        for (int i = 0; i < camCorners.Length; i++)
        {
            center += camCorners[i];
        }
        center /= camCorners.Length;

        Vector3 lightPos = center - forward * 400;

        // Build matrix manually
        Matrix4x4 view = new Matrix4x4();
        view.SetRow(0, new Vector4(right.x, right.y, right.z, -Vector3.Dot(right, lightPos)));
        view.SetRow(1, new Vector4(up.x, up.y, up.z, -Vector3.Dot(up, lightPos)));
        view.SetRow(
            2,
            new Vector4(forward.x, forward.y, forward.z, -Vector3.Dot(forward, lightPos))
        );
        view.SetRow(3, new Vector4(0, 0, 0, 1));

        float l = float.PositiveInfinity;
        float r = float.NegativeInfinity;
        float b = float.PositiveInfinity;
        float t = float.NegativeInfinity;

        float factor = 2;

        for (int i = 0; i < camCorners.Length; i++)
        {
            Vector3 viewSpace = view.MultiplyPoint3x4(camCorners[i]);
            t = math.max(t, viewSpace.y * factor);
            b = math.min(b, viewSpace.y * factor);
            l = math.min(l, viewSpace.x * factor);
            r = math.max(r, viewSpace.x * factor);
        }

        // Step 4: Construct orthographic frustum corners in light space
        orthoCorners[0] = new Vector3(l, b, 0.1f); // near bottom-left
        orthoCorners[1] = new Vector3(r, b, 0.1f); // near bottom-right
        orthoCorners[2] = new Vector3(r, t, 0.1f); // near top-right
        orthoCorners[3] = new Vector3(l, t, 0.1f); // near top-left

        orthoCorners[4] = new Vector3(l, b, 1000f); // far bottom-left
        orthoCorners[5] = new Vector3(r, b, 1000f); // far bottom-right
        orthoCorners[6] = new Vector3(r, t, 1000f); // far top-right
        orthoCorners[7] = new Vector3(l, t, 1000f); // far top-left

        // Step 5: Transform corners back to world space
        Matrix4x4 invLightView = view.inverse;
        for (int i = 0; i < 8; i++)
            orthoCorners[i] = invLightView.MultiplyPoint3x4(orthoCorners[i]);

        // Step 6: Draw frustum lines in Game view
        for (int i = 0; i < 4; i++)
        {
            Debug.DrawLine(orthoCorners[i], orthoCorners[(i + 1) % 4], lineColor, lineDuration); // near
            Debug.DrawLine(
                orthoCorners[i + 4],
                orthoCorners[((i + 1) % 4) + 4],
                lineColor,
                lineDuration
            ); // far
            Debug.DrawLine(orthoCorners[i], orthoCorners[i + 4], lineColor, lineDuration); // sides
        }
    }
}

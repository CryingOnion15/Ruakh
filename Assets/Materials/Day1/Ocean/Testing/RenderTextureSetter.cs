using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CopyDepthToRenderTexture : MonoBehaviour
{
    public RenderTexture depthCopy;

    Camera cam;
    CommandBuffer cmd;
    static readonly int DepthTexID = Shader.PropertyToID("_CameraDepthTexture");

    void OnEnable()
    {
        cam = GetComponent<Camera>();

        if (depthCopy == null)
        {
            depthCopy = new RenderTexture(
                Screen.width,
                Screen.height,
                0,
                RenderTextureFormat.RFloat
            );
            depthCopy.enableRandomWrite = true;
            depthCopy.Create();
        }

        cmd = new CommandBuffer { name = "Copy Depth Texture" };
        cam.AddCommandBuffer(CameraEvent.AfterDepthTexture, cmd);
    }

    void OnDisable()
    {
        if (cmd != null)
        {
            cam.RemoveCommandBuffer(CameraEvent.AfterDepthTexture, cmd);
            cmd.Release();
        }

        if (depthCopy != null)
        {
            depthCopy.Release();
        }
    }

    void OnPreRender()
    {
        if (cmd == null)
            return;

        cmd.Clear();

        // Grab the depth texture from URP’s global binding
        var depthTex = Shader.GetGlobalTexture("_CameraDepthTexture");

        if (depthTex != null)
        {
            // Copy the global depth texture to our RenderTexture
            cmd.Blit(depthTex, depthCopy);
        }
        else
        {
            Debug.LogWarning(
                "No _CameraDepthTexture found — make sure Depth Texture is enabled in URP!"
            );
        }
    }

    void OnGUI()
    {
        // Draw it on screen for debugging
        if (depthCopy != null)
        {
            GUI.DrawTexture(new Rect(0, 0, 256, 256), depthCopy, ScaleMode.ScaleToFit, false);
        }
    }
}

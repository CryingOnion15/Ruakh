using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class StainedShadowRenderPass : ScriptableRenderPass
{
    protected Light dirLight;
    protected LayerMask drawMask;
    protected Material dataOverrideMaterial;
    protected int ShadowMapResolution = 1024;

    protected readonly ShaderTagId shaderTag = new ShaderTagId("UniversalForward");

    // Frustum corners variables. (To avoid per frame allocation.)
    protected Vector3[] corners = new Vector3[8];

    //protected Vector3[] cornersLightSpace = new Vector3[8];
    protected Vector3[] tempCorners = new Vector3[4];
    protected Rect viewportRect = new Rect(0, 0, 1, 1);

    // Light Matrices and Light Direction

    protected Vector3 lightDirection;
    protected Matrix4x4 lightViewMatrix;
    protected Matrix4x4 lightProjectionMatrix;

    // Texture Descriptions
    protected TextureDesc colorDesc;
    protected TextureDesc lumDesc;
    protected TextureDesc normalDesc;
    protected TextureDesc depthDesc;

    class PassData
    {
        internal RendererListHandle rendererListHandle;
        internal TextureHandle shadowColorTexture;
        internal TextureHandle shadowLumTexture;
        internal TextureHandle shadowNormalTexture;
        internal TextureHandle shadowDepthTexture;
        internal Matrix4x4 viewMat;
        internal Matrix4x4 projMat;
    }

    // Setup the render pass with necessary data.
    public void Setup(Light light, LayerMask mask, Material dataOverrideMat, int mapResolution)
    {
        dirLight = light;
        drawMask = mask;
        dataOverrideMaterial = dataOverrideMat;
        ShadowMapResolution = mapResolution;
        requiresIntermediateTexture = true;

        // Create the color desc.
        colorDesc = new TextureDesc(ShadowMapResolution, ShadowMapResolution)
        {
            colorFormat = GraphicsFormat.R32G32B32A32_SFloat,
            depthBufferBits = DepthBits.None,
            dimension = TextureDimension.Tex2D,
            name = "_StainedShadowColorMap",
            clearBuffer = true,
            clearColor = Color.clear,
        };

        lumDesc = new TextureDesc(ShadowMapResolution, ShadowMapResolution)
        {
            colorFormat = GraphicsFormat.R32G32B32A32_SFloat,
            depthBufferBits = DepthBits.None,
            dimension = TextureDimension.Tex2D,
            name = "_StainedShadowLumMap",
            clearBuffer = true,
            clearColor = Color.clear,
        };

        // Create the color desc.
        normalDesc = new TextureDesc(ShadowMapResolution, ShadowMapResolution)
        {
            colorFormat = GraphicsFormat.R32G32B32A32_SFloat,
            depthBufferBits = DepthBits.None,
            dimension = TextureDimension.Tex2D,
            name = "_StainedShadowNormalMap",
            clearBuffer = true,
            clearColor = Color.clear,
        };

        // Create the color desc.
        depthDesc = new TextureDesc(ShadowMapResolution, ShadowMapResolution)
        {
            colorFormat = GraphicsFormat.R32G32B32A32_SFloat,
            depthBufferBits = DepthBits.None,
            dimension = TextureDimension.Tex2D,
            name = "_StainedShadowDepthMap",
            clearBuffer = true,
        };
    }

    // Override to define the render pass instructions.
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        // Name the render pass.
        string passName = "Shadow Pass - Render Screen From Light";

        // Get frame data.
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();
        StainedGlassData glassData = frameData.Get<StainedGlassData>();
        Camera camera = cameraData.camera;

        // Create the render pass.
        using var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var data);

        TextureHandle shadowColorTexture = renderGraph.CreateTexture(colorDesc);
        TextureHandle shadowLumTexture = renderGraph.CreateTexture(lumDesc);
        TextureHandle shadowNormalTexture = renderGraph.CreateTexture(normalDesc);
        TextureHandle shadowDepthTexture = renderGraph.CreateTexture(depthDesc);

        // Update Light Data for renderings shadows.
        UpdateFrustumCorners(camera, corners);
        UpdateLightData();

        // Create Drawing Settings
        SortingCriteria sortFlags = SortingCriteria.RenderQueue;
        RenderQueueRange queueRange = RenderQueueRange.opaque;
        FilteringSettings filterSettings = new FilteringSettings(queueRange, drawMask);
        DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(
            shaderTag,
            renderingData,
            cameraData,
            lightData,
            sortFlags
        );

        RendererListParams rParams = new RendererListParams(
            renderingData.cullResults,
            drawSettings,
            filterSettings
        );

        var renderList = renderGraph.CreateRendererList(rParams);

        // Set PassData
        data.rendererListHandle = renderList;
        data.viewMat = lightViewMatrix;
        data.projMat = lightProjectionMatrix;
        data.shadowColorTexture = shadowColorTexture;
        data.shadowLumTexture = shadowLumTexture;
        data.shadowNormalTexture = shadowNormalTexture;
        data.shadowDepthTexture = shadowDepthTexture;

        // Set the builder settings and set the render functions.
        builder.SetRenderAttachment(shadowColorTexture, 0);
        builder.SetRenderAttachment(shadowNormalTexture, 1);
        builder.SetRenderAttachment(shadowLumTexture, 2);
        builder.SetRenderAttachment(shadowDepthTexture, 3);
        builder.UseRendererList(renderList);
        builder.SetGlobalTextureAfterPass(
            shadowColorTexture,
            Shader.PropertyToID("_StainedShadowColorMap")
        );
        builder.SetGlobalTextureAfterPass(
            shadowLumTexture,
            Shader.PropertyToID("_StainedShadowLumMap")
        );
        builder.SetGlobalTextureAfterPass(
            shadowNormalTexture,
            Shader.PropertyToID("_StainedShadowNormalMap")
        );
        builder.SetGlobalTextureAfterPass(
            shadowDepthTexture,
            Shader.PropertyToID("_StainedShadowDepthMap")
        );
        builder.AllowGlobalStateModification(true);
        builder.AllowPassCulling(false);
        builder.SetRenderFunc(
            (PassData data, RasterGraphContext context) =>
            {
                context.cmd.SetViewProjectionMatrices(data.viewMat, data.projMat);

                // Set Bias to prevent Shadow Acne
                context.cmd.SetGlobalDepthBias(0f, 0f);

                // Draw all geometry based on the renderer list.
                context.cmd.DrawRendererList(data.rendererListHandle);

                // Reset Command Buffer.
                //context.cmd.SetGlobalDepthBias(2.0f, 1.0f);
                context.cmd.SetViewProjectionMatrices(
                    camera.worldToCameraMatrix,
                    camera.projectionMatrix
                );
            }
        );

        // Set Global Textures for testing.

        Shader.SetGlobalMatrix("_StainedShadowVPMatrix", lightProjectionMatrix * lightViewMatrix);
        glassData.lightColorTextureHandle = shadowColorTexture;
        glassData.lightDepthTextureHandle = shadowDepthTexture;
        glassData.lightNormalTextureHandle = shadowNormalTexture;
        glassData.lightLumTextureHandle = shadowLumTexture;
    }

    protected void UpdateLightData()
    {
        Vector3 right = dirLight.transform.right;
        Vector3 up = dirLight.transform.up;
        Vector3 forward = -dirLight.transform.forward;

        Vector3 center = Vector3.zero;
        for (int i = 0; i < corners.Length; i++)
        {
            center += corners[i];
        }
        center /= corners.Length;

        Vector3 lightPos = center + forward * 200;

        // Build matrix manually
        Matrix4x4 view = new Matrix4x4();
        view.SetRow(0, new Vector4(right.x, right.y, right.z, -Vector3.Dot(right, lightPos)));
        view.SetRow(1, new Vector4(up.x, up.y, up.z, -Vector3.Dot(up, lightPos)));
        view.SetRow(
            2,
            new Vector4(forward.x, forward.y, forward.z, -Vector3.Dot(forward, lightPos))
        );
        view.SetRow(3, new Vector4(0, 0, 0, 1));

        lightViewMatrix = view;

        float l = float.PositiveInfinity;
        float r = float.NegativeInfinity;
        float b = float.PositiveInfinity;
        float t = float.NegativeInfinity;

        float factor = 1;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 viewSpace = lightViewMatrix.MultiplyPoint3x4(corners[i]);
            t = math.max(t, viewSpace.y * factor);
            b = math.min(b, viewSpace.y * factor);
            l = math.min(l, viewSpace.x * factor);
            r = math.max(r, viewSpace.x * factor);
        }

        // Create the Ortho Project Matrix from the AABB
        lightProjectionMatrix = GL.GetGPUProjectionMatrix(
            Matrix4x4.Ortho(l, r, t, b, 0.1f, 1000f),
            true
        );
    }

    /// <summary>
    /// // Updates frustum corners array for the current frame.
    /// [0-3] = near corners, [4-7] = far corners.
    /// </summary>
    /// <param name="camera">Camera of the current frame.</param>
    protected void UpdateFrustumCorners(Camera camera, Vector3[] corners)
    {
        var camToWorld = camera.transform.localToWorldMatrix;

        // Add near world points to the array.
        camera.CalculateFrustumCorners(
            viewportRect,
            camera.nearClipPlane,
            Camera.MonoOrStereoscopicEye.Mono,
            tempCorners
        );
        for (int i = 0; i < tempCorners.Length; i++)
            corners[i] = camToWorld.MultiplyPoint3x4(tempCorners[i]);

        // Add far world points to the array.
        camera.CalculateFrustumCorners(
            viewportRect,
            camera.farClipPlane,
            Camera.MonoOrStereoscopicEye.Mono,
            tempCorners
        );
        for (int i = 0; i < tempCorners.Length; i++)
            corners[i + 4] = camToWorld.MultiplyPoint3x4(tempCorners[i]);
    }
}

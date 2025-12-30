using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class CascadeData
{
    public TextureHandle cascadeColorTexture;
    public TextureHandle cascadeDepthTexture;
    public Vector3[][] cascadeCorners;
    public Matrix4x4[] cascadeViewMatricies;
    public Matrix4x4[] cascadeProjMatricies;
    public Matrix4x4[] cascadeViewProjMatricies;
    public readonly float[] CascadeBounds = new float[] { 0f, .1f, .25f, .5f, 1f };

    public CascadeData(RenderGraph rg, TextureDesc colorDesc, TextureDesc depthDesc, int cascades)
    {
        cascadeColorTexture = rg.CreateTexture(colorDesc);
        cascadeDepthTexture = rg.CreateTexture(depthDesc);
        cascadeCorners = new Vector3[cascades][];
        cascadeViewMatricies = new Matrix4x4[cascades];
        cascadeProjMatricies = new Matrix4x4[cascades];
        cascadeViewProjMatricies = new Matrix4x4[cascades];
    }
}

public class StainedShadowRenderPass : ScriptableRenderPass
{
    protected Light dirLight;
    protected LayerMask drawMask;
    protected Material overrideMaterial;
    protected int ShadowMapResolution = 1024;

    protected readonly ShaderTagId shaderTag = new ShaderTagId("UniversalForward");

    // Frustum corners variables. (To avoid per frame allocation.)
    protected Vector3[] corners = new Vector3[8];
    protected Vector3[] tempCorners = new Vector3[4];
    protected Rect viewportRect = new Rect(0, 0, 1, 1);

    // Light Matrices and Light Direction
    protected Vector3 lightDirection;

    //protected Matrix4x4 lightViewMatrix;
    //protected Matrix4x4 lightProjectionMatrix;

    class PassData
    {
        internal RendererListHandle rendererListHandle;
        internal Matrix4x4 viewMat;
        internal Matrix4x4 projMat;
    }

    // Setup the render pass with necessary data.
    public void Setup(Light light, LayerMask mask, Material dataOverrideMat, int mapResolution)
    {
        dirLight = light;
        drawMask = mask;
        overrideMaterial = dataOverrideMat;
        ShadowMapResolution = mapResolution;
        requiresIntermediateTexture = true;
    }

    // Override to define the render pass instructions.
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        CascadeData cData = InitializeCascadeTextures(renderGraph, 4);

        // Get frame data.
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();
        StainedGlassData glassData = frameData.Get<StainedGlassData>();
        Camera camera = cameraData.camera;

        // Update Light Data for renderings shadows.
        UpdateFrustumCorners(camera, corners);
        GenerateCascadeCorners(corners, cData);

        for (int i = 0; i < 4; i++)
        {
            // Name the render pass.
            string passName = $"Shadow Pass - Render Screen From Light - Cascade {i + 1}";

            // Create the render pass.
            using var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var data);

            UpdateLightDataForCascade(cData, i);

            // Create Drawing Settings
            SortingCriteria sortFlags = SortingCriteria.CommonOpaque;
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
            data.viewMat = cData.cascadeViewMatricies[i];
            data.projMat = cData.cascadeProjMatricies[i];

            // Set the builder settings and set the render functions.
            builder.SetRenderAttachment(cData.cascadeColorTexture, 0, AccessFlags.ReadWrite, 0, i);
            builder.SetRenderAttachment(cData.cascadeDepthTexture, 1, AccessFlags.ReadWrite, 0, i);
            builder.UseRendererList(renderList);
            builder.SetGlobalTextureAfterPass(
                cData.cascadeColorTexture,
                Shader.PropertyToID("_StainedShadowColorMap")
            );
            builder.SetGlobalTextureAfterPass(
                cData.cascadeDepthTexture,
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
                    context.cmd.SetViewProjectionMatrices(
                        camera.worldToCameraMatrix,
                        camera.projectionMatrix
                    );
                }
            );
        }

        SetShaderVariables(cData);
    }

    protected void SetShaderVariables(CascadeData cData)
    {
        // --- Set Camera Params ---
        // Build matrix manually
        Quaternion lightRot = Quaternion.LookRotation(
            dirLight.transform.forward,
            dirLight.transform.up
        );

        // Find the depth range.
        Matrix4x4 tempView = Matrix4x4.TRS(Vector3.zero, lightRot, Vector3.one).inverse;
        float l = float.PositiveInfinity;
        float r = float.NegativeInfinity;
        float b = float.PositiveInfinity;
        float t = float.NegativeInfinity;
        float far = float.NegativeInfinity;
        float near = float.PositiveInfinity;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 viewSpace = tempView.MultiplyPoint3x4(corners[i]);
            t = math.max(t, viewSpace.y);
            b = math.min(b, viewSpace.y);
            l = math.min(l, viewSpace.x);
            r = math.max(r, viewSpace.x);
            far = math.max(far, viewSpace.z);
            near = math.min(near, viewSpace.z);
        }

        // Set Global Shader Params Vector.
        // x = near, y = far, z = 1 / Far - Near, w = near / far - near
        Shader.SetGlobalVector(
            "_ShadowParams",
            new Vector4(near, far, 1 / (far - near), near / (far - near))
        );

        // -- Set Matrices --
        Shader.SetGlobalMatrixArray("_StainedShadowViewMatrix", cData.cascadeViewMatricies);
        Shader.SetGlobalMatrixArray("_StainedShadowProjMatrix", cData.cascadeProjMatricies);
        Shader.SetGlobalMatrixArray("_StainedShadowVPMatrix", cData.cascadeViewProjMatricies);

        // -- Set Other Values --
        Shader.SetGlobalVector(
            "_ShadowTexelSize",
            new Vector2(1 / ShadowMapResolution, 1 / ShadowMapResolution)
        );

        Shader.SetGlobalFloatArray("_StainedCascadeBounds", cData.CascadeBounds);
    }

    protected void UpdateLightDataForCascade(CascadeData cData, int cascadeIndex)
    {
        // Light Direction;
        Vector3 lightDirection = dirLight.transform.forward;
        Vector3[] casCorners = cData.cascadeCorners[cascadeIndex];

        Vector3 center = Vector3.zero;
        for (int i = 0; i < casCorners.Length; i++)
        {
            center += casCorners[i];
        }
        center /= casCorners.Length;

        // Build matrix manually
        Quaternion lightRot = Quaternion.LookRotation(lightDirection, dirLight.transform.up);

        // Find the depth range.
        Matrix4x4 tempView = Matrix4x4.TRS(Vector3.zero, lightRot, Vector3.one).inverse;
        float l = float.PositiveInfinity;
        float r = float.NegativeInfinity;
        float b = float.PositiveInfinity;
        float t = float.NegativeInfinity;
        float far = float.NegativeInfinity;
        float near = float.PositiveInfinity;

        for (int i = 0; i < casCorners.Length; i++)
        {
            Vector3 viewSpace = tempView.MultiplyPoint3x4(casCorners[i]);
            t = math.max(t, viewSpace.y);
            b = math.min(b, viewSpace.y);
            l = math.min(l, viewSpace.x);
            r = math.max(r, viewSpace.x);
            far = math.max(far, viewSpace.z);
            near = math.min(near, viewSpace.z);
        }

        float depthCenter = (far + near) * .5f;
        Vector3 lightPos = center - lightDirection * depthCenter;
        Matrix4x4 view = Matrix4x4.TRS(lightPos, lightRot, Vector3.one).inverse;
        cData.cascadeViewMatricies[cascadeIndex] = view;

        // Create the Ortho Project Matrix from the AABB
        cData.cascadeProjMatricies[cascadeIndex] = GL.GetGPUProjectionMatrix(
            Matrix4x4.Ortho(l, r, b, t, near, far),
            true
        );

        cData.cascadeViewProjMatricies[cascadeIndex] =
            cData.cascadeProjMatricies[cascadeIndex] * cData.cascadeViewMatricies[cascadeIndex];

        // Set Global Shader Params Vector.
        // x = near, y = far, z = 1 / Far - Near, w = near / far - near
        Shader.SetGlobalVector(
            "_ShadowParams",
            new Vector4(near, far, 1 / (far - near), near / (far - near))
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

    protected void GenerateCascadeCorners(Vector3[] corners, CascadeData cData)
    {
        Vector3 lowerLeft = corners[4] - corners[0];
        Vector3 upperLeft = corners[5] - corners[1];
        Vector3 upperRight = corners[6] - corners[2];
        Vector3 lowerRight = corners[7] - corners[3];

        for (int i = 0; i < cData.cascadeCorners.Length; i++)
        {
            float bound0 = cData.CascadeBounds[i];
            float bound1 = cData.CascadeBounds[i + 1];

            cData.cascadeCorners[i] = new Vector3[8];
            //Push corners in order like CalculateFrustumCorners.
            cData.cascadeCorners[i][0] = corners[0] + lowerLeft * bound0; // lower left near.
            cData.cascadeCorners[i][1] = corners[1] + upperLeft * bound0; // upper left near.
            cData.cascadeCorners[i][2] = corners[2] + upperRight * bound0; // upper right near.
            cData.cascadeCorners[i][3] = corners[3] + lowerRight * bound0; // lower right near.

            cData.cascadeCorners[i][4] = corners[0] + lowerLeft * bound1; // lower left far.
            cData.cascadeCorners[i][5] = corners[1] + upperLeft * bound1; // upper left far.
            cData.cascadeCorners[i][6] = corners[2] + upperRight * bound1; // upper right far.
            cData.cascadeCorners[i][7] = corners[3] + lowerRight * bound1; // lower right far.
        }
    }

    protected CascadeData InitializeCascadeTextures(RenderGraph rg, int cascades)
    {
        // Create the color desc.
        TextureDesc colorDesc = new TextureDesc(ShadowMapResolution, ShadowMapResolution)
        {
            colorFormat = GraphicsFormat.R32G32B32A32_SFloat,
            depthBufferBits = DepthBits.None,
            dimension = TextureDimension.Tex2DArray,
            name = "_StainedShadowColorMap",
            clearBuffer = true,
            slices = 4,
            clearColor = Color.clear,
        };

        // Create the depth desc.
        TextureDesc depthDesc = new TextureDesc(ShadowMapResolution, ShadowMapResolution)
        {
            colorFormat = GraphicsFormat.R32G32B32A32_SFloat,
            depthBufferBits = DepthBits.None,
            dimension = TextureDimension.Tex2DArray,
            name = "_StainedShadowDepthMap",
            clearBuffer = true,
            slices = 4,
            clearColor = Color.red,
        };

        return new CascadeData(rg, colorDesc, depthDesc, cascades);
    }
}

using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class CascadeData
{
    public Vector3[][] cascadeCorners;
    public Matrix4x4[] cascadeViewMatricies;
    public Matrix4x4[] cascadeProjMatricies;
    public Matrix4x4[] cascadeViewProjMatricies;
    public CullingResults[] cascadeCullData;
    public float[] MeterBounds;
    public Vector4[] ShadowParams;
    public int CascadeCount;
    public readonly float[] CascadeBounds = new float[] { 0f, .1f, .25f, .5f, 1f };

    public CascadeData(int cascades)
    {
        cascadeCorners = new Vector3[cascades][];
        CascadeCount = cascades;
        cascadeViewMatricies = new Matrix4x4[cascades];
        cascadeProjMatricies = new Matrix4x4[cascades];
        cascadeViewProjMatricies = new Matrix4x4[cascades];
        cascadeCullData = new CullingResults[cascades];
        MeterBounds = new float[cascades + 1];
        ShadowParams = new Vector4[cascades];
    }
}

public class StainedShadowRenderPass : ScriptableRenderPass
{
    protected Light dirLight;
    protected LayerMask drawMask;
    protected Material overrideMaterial;
    protected int ShadowMapResolution = 1024;
    protected TextureHandle cascadeColorTexture;
    protected TextureHandle cascadeDepthTexture;

    protected readonly ShaderTagId shaderTag = new ShaderTagId("UniversalForward");

    // Frustum corners variables. (To avoid per frame allocation.)
    protected Vector3[] corners = new Vector3[8];
    protected Vector3[] tempCorners = new Vector3[4];
    protected Rect viewportRect = new Rect(0, 0, 1, 1);

    // Light Matrices and Light Direction
    protected Vector3 lightDirection;
    protected Vector3 orthoPadding;
    protected CascadeData cData;

    class PassData
    {
        internal RendererListHandle rendererListHandle;
        internal Matrix4x4 viewMat;
        internal Matrix4x4 projMat;
    }

    // Setup the render pass with necessary data.
    public void Setup(
        Light light,
        LayerMask mask,
        Material dataOverrideMat,
        int mapResolution,
        Vector3 oPadding
    )
    {
        dirLight = light;
        drawMask = mask;
        overrideMaterial = dataOverrideMat;
        ShadowMapResolution = mapResolution;
        requiresIntermediateTexture = true;
        cData = new CascadeData(4);
        orthoPadding = oPadding;
    }

    // Override to define the render pass instructions.
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        InitializeCascadeTextures(renderGraph);

        // Get frame data.
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();
        //StainedGlassData glassData = frameData.Get<StainedGlassData>();
        Camera camera = cameraData.camera;

        UpdateFrustumCorners(camera, corners);
        GenerateCascadeCorners(camera, cData);
        UpdateLightDataForCascades(camera, cData);

        for (int i = 0; i < 4; i++)
        {
            // Name the render pass.
            string passName = $"Shadow Pass - Render Screen From Light - Cascade {i + 1}";

            // Create the render pass.
            using var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var data);

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
            builder.SetRenderAttachment(cascadeColorTexture, 0, AccessFlags.ReadWrite, 0, i);
            builder.SetRenderAttachment(cascadeDepthTexture, 1, AccessFlags.ReadWrite, 0, i);
            builder.UseRendererList(renderList);
            builder.SetGlobalTextureAfterPass(
                cascadeColorTexture,
                Shader.PropertyToID("_StainedShadowColorMap")
            );
            builder.SetGlobalTextureAfterPass(
                cascadeDepthTexture,
                Shader.PropertyToID("_StainedShadowDepthTexture")
            );
            builder.AllowGlobalStateModification(true);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(
                (PassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetViewProjectionMatrices(data.viewMat, data.projMat);

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

        SetShaderVariables(cData, camera);
    }

    protected void SetShaderVariables(CascadeData cData, Camera camera)
    {
        // Set Global Shader Params Vector.
        Shader.SetGlobalVectorArray("_ShadowParams", this.cData.ShadowParams);

        // -- Set Matrices --
        Shader.SetGlobalMatrixArray("_StainedShadowViewMatrix", cData.cascadeViewMatricies);
        Shader.SetGlobalMatrixArray("_StainedShadowProjMatrix", cData.cascadeProjMatricies);
        Shader.SetGlobalMatrixArray("_StainedShadowVPMatrix", cData.cascadeViewProjMatricies);

        // -- Set Other Values --
        Shader.SetGlobalVector(
            "_ShadowTexelSize",
            new Vector2(1.0f / ShadowMapResolution, 1.0f / ShadowMapResolution)
        );

        //Set the meter bounds.
        for (int i = 0; i < cData.MeterBounds.Length; i++)
        {
            cData.MeterBounds[i] = Mathf.Lerp(
                camera.nearClipPlane,
                camera.farClipPlane,
                cData.CascadeBounds[i]
            );
        }

        Shader.SetGlobalFloatArray("_StainedCascadeBounds", cData.MeterBounds);

        Shader.SetGlobalVector("_LightDirection", dirLight.transform.forward);
    }

    protected void InitializeCascadeTextures(RenderGraph rg)
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

        TextureDesc depthDesc = new TextureDesc(ShadowMapResolution, ShadowMapResolution)
        {
            format = GraphicsFormat.R32_SFloat,
            dimension = TextureDimension.Tex2DArray,
            name = "_StainedShadowDepthTexture",
            clearBuffer = true,
            slices = 4,
            clearColor = Color.clear,
        };

        cascadeColorTexture = rg.CreateTexture(colorDesc);
        cascadeDepthTexture = rg.CreateTexture(depthDesc);
    }

    protected void UpdateLightDataForCascades(Camera camera, CascadeData cData)
    {
        lightDirection = dirLight.transform.forward;

        // Stable up vector (avoid gimbal flip)
        Vector3 up =
            Mathf.Abs(Vector3.Dot(lightDirection, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;

        for (int i = 0; i < cData.CascadeCount; i++)
        {
            Vector3[] corners = cData.cascadeCorners[i];
            float maxY = float.NegativeInfinity;
            //float minZ = float

            // calculate cascade center.
            // Vector3 center = Vector3.zero;
            // for (int j = 0; j < 8; j++)
            // {
            //     center += corners[j];
            //     maxY = Math.Max(maxY, corners[j].y);
            // }
            // center /= 8;

            Vector3 lightPosition =
                camera.transform.position
                + camera.transform.forward
                    * (
                        Mathf.Lerp(
                            camera.nearClipPlane,
                            camera.farClipPlane,
                            cData.CascadeBounds[i]
                        ) - 10f
                    );
            lightPosition.y = Math.Max(camera.transform.position.y + 50f, maxY + 10f);
            //Vector3 lightPosition = camera.transform.forward + ;

            // Generate view Matrix
            Matrix4x4 view = Matrix4x4.LookAt(
                lightPosition,
                lightPosition + lightDirection,
                dirLight.transform.up
            );

            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int j = 0; j < corners.Length; j++)
            {
                Vector3 v = view.MultiplyPoint3x4(corners[j]);

                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
            }

            float l = min.x;
            float r = max.x;
            float b = min.y;
            float t = max.y;
            float near = min.z;
            float far = max.z;

            // // --------------------------------------------------
            // // Padding
            // // --------------------------------------------------
            // float xPad = (r - l) * orthoPadding.x;
            // float yPad = (t - b) * orthoPadding.y;
            // float zPad = (far - near) * orthoPadding.z;

            // l -= xPad;
            // r += xPad;
            // b -= yPad;
            // t += yPad;
            // near -= zPad;
            // far += zPad;

            // --------------------------------------------------
            // Texel snapping (reduces shimmer)
            // --------------------------------------------------
            float texelSizeX = (r - l) / ShadowMapResolution;
            float texelSizeY = (t - b) / ShadowMapResolution;

            l = Mathf.Floor(l / texelSizeX) * texelSizeX;
            r = Mathf.Ceil(r / texelSizeX) * texelSizeX;
            b = Mathf.Floor(b / texelSizeY) * texelSizeY;
            t = Mathf.Ceil(t / texelSizeY) * texelSizeY;

            // --------------------------------------------------
            // Orthographic Projection
            // --------------------------------------------------
            Matrix4x4 proj = GL.GetGPUProjectionMatrix(
                Matrix4x4.Ortho(l, r, b, t, near, far),
                true
            );

            // --------------------------------------------------
            // Store results
            // --------------------------------------------------
            cData.cascadeViewMatricies[i] = view;
            cData.cascadeProjMatricies[i] = proj;
            cData.cascadeViewProjMatricies[i] = proj * view;

            cData.ShadowParams[i] = new Vector4(near, far, 1f / (far - near), 0f);
        }
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

    protected void GenerateCascadeCorners(Camera camera, CascadeData cData)
    {
        for (int i = 0; i < cData.cascadeCorners.Length; i++)
        {
            float nearBound = cData.CascadeBounds[i];
            float farBound = cData.CascadeBounds[i + 1];

            float cascadeNear = Mathf.Lerp(camera.nearClipPlane, camera.farClipPlane, nearBound);
            float cascadeFar = Mathf.Lerp(camera.nearClipPlane, camera.farClipPlane, farBound);

            var camToWorld = camera.transform.localToWorldMatrix;

            cData.cascadeCorners[i] = new Vector3[8];

            // Add near world points to the array.
            camera.CalculateFrustumCorners(
                viewportRect,
                cascadeNear,
                Camera.MonoOrStereoscopicEye.Mono,
                tempCorners
            );
            for (int j = 0; j < tempCorners.Length; j++)
                cData.cascadeCorners[i][j] = camToWorld.MultiplyPoint3x4(tempCorners[j]);

            // Add far world points to the array.
            camera.CalculateFrustumCorners(
                viewportRect,
                cascadeFar,
                Camera.MonoOrStereoscopicEye.Mono,
                tempCorners
            );
            for (int j = 0; j < tempCorners.Length; j++)
                cData.cascadeCorners[i][j + 4] = camToWorld.MultiplyPoint3x4(tempCorners[j]);
        }
    }

    protected void GenerateCullingDataForCascades(
        CascadeData cData,
        Camera camera,
        ScriptableRenderContext context
    )
    {
        camera.TryGetCullingParameters(out var cullingParameters);

        for (int i = 0; i < cData.CascadeCount; i++)
        {
            var cParams = cullingParameters;
            cParams.isOrthographic = true;
            cParams.cullingMatrix = cData.cascadeViewProjMatricies[i];
            cParams.maximumVisibleLights = 0;
            cParams.shadowDistance = float.MaxValue;
            cData.cascadeCullData[i] = context.Cull(ref cParams);
        }
    }
}

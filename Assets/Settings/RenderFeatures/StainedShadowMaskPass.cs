using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class StainedShadowMaskPass : ScriptableRenderPass
{
    protected LayerMask drawMask;
    protected Material overrideMaterial;

    protected TextureDesc maskDesc;
    protected TextureHandle shadowMaskTexture;
    protected readonly ShaderTagId shaderTag = new ShaderTagId("UniversalForward");

    class PassData
    {
        internal RendererListHandle rendererListHandle;
    }

    // Setup the render pass with necessary data.
    public void Setup(LayerMask mask, Material dataOverrideMat)
    {
        drawMask = mask;
        overrideMaterial = dataOverrideMat;
        requiresIntermediateTexture = true;
    }

    // Override to define the render pass instructions.
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        // Get frame data.
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();

        maskDesc = new TextureDesc(
            cameraData.cameraTargetDescriptor.width,
            cameraData.cameraTargetDescriptor.height
        )
        {
            colorFormat = GraphicsFormat.R32G32B32A32_SFloat,
            depthBufferBits = DepthBits.None,
            dimension = TextureDimension.Tex2D,
            name = "_StainedShadowMask",
            clearBuffer = true,
            clearColor = Color.clear,
        };

        shadowMaskTexture = renderGraph.CreateTexture(maskDesc);

        // Name the render pass.
        string passName = $"Shadow Pass - Create Shadow Mask";

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

        drawSettings.overrideMaterial = overrideMaterial;
        drawSettings.overrideMaterialPassIndex = 0;

        RendererListParams rParams = new RendererListParams(
            renderingData.cullResults,
            drawSettings,
            filterSettings
        );

        var renderList = renderGraph.CreateRendererList(rParams);

        // Set PassData
        data.rendererListHandle = renderList;

        // Set the builder settings and set the render functions.
        builder.SetRenderAttachment(shadowMaskTexture, 0, AccessFlags.ReadWrite);
        builder.UseRendererList(renderList);
        builder.SetGlobalTextureAfterPass(
            shadowMaskTexture,
            Shader.PropertyToID("_StainedShadowMask")
        );
        builder.AllowGlobalStateModification(true);
        builder.AllowPassCulling(false);
        builder.SetRenderFunc(
            (PassData data, RasterGraphContext context) =>
            {
                // Draw all geometry based on the renderer list.
                context.cmd.DrawRendererList(data.rendererListHandle);
            }
        );
    }
}

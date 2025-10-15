using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class StainedGlassColorTexturePass : ScriptableRenderPass
{
    LayerMask layerMask;
    Material overrideMat;

    class PassData
    {
        internal RendererListHandle listHandle;
    }

    // Setup function for the render pass.
    public void Setup(LayerMask mask, Material oMaterial)
    {
        layerMask = mask;
        overrideMat = oMaterial;
        requiresIntermediateTexture = true;
    }

    // Override to define the render pass instructions.
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        // Get frame data.
        var cameraData = frameData.Get<UniversalCameraData>();
        var resourceData = frameData.Get<UniversalResourceData>();
        var renderingData = frameData.Get<UniversalRenderingData>();
        var lightData = frameData.Get<UniversalLightData>();
        var transferData = frameData.Create<FilteredTextureData>();

        // Create the filtered color texture.
        var destinationDesc = resourceData.activeColorTexture.GetDescriptor(renderGraph);
        destinationDesc.name = "Filtered Color Texture";

        var filteredColor = renderGraph.CreateTexture(destinationDesc);

        // Create the filtered depth data texture.
        var depthDesc = destinationDesc;
        depthDesc.name = "Filtered Depth Texture";
        depthDesc.colorFormat = GraphicsFormat.R32_SFloat;
        var filteredDepth = renderGraph.CreateTexture(depthDesc);

        // Create the filtered normal data texture.
        var normalDesc = destinationDesc;
        normalDesc.name = "Filtered Normals Texture";
        normalDesc.colorFormat = GraphicsFormat.R8G8B8A8_UNorm;
        var filteredNormals = renderGraph.CreateTexture(normalDesc);

        // Create the render pass builder.
        using var builder = renderGraph.AddRasterRenderPass<PassData>(
            "Draw Active Color Layer Mask",
            out var passData
        );

        // Create the Draw settings.
        SortingCriteria sortFlags = SortingCriteria.CommonOpaque;
        RenderQueueRange queueRange = RenderQueueRange.opaque;
        FilteringSettings filterSettings = new FilteringSettings(queueRange, layerMask);
        ShaderTagId tag = new ShaderTagId("UniversalForward");
        DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(
            tag,
            renderingData,
            cameraData,
            lightData,
            sortFlags
        );
        // Override the material to produce the data we need for the outline pass. (DataOverrideShader.hlsl)
        drawSettings.overrideMaterial = overrideMat;

        RendererListParams rParams = new RendererListParams(
            renderingData.cullResults,
            drawSettings,
            filterSettings
        );

        // Set the pass data.
        passData.listHandle = renderGraph.CreateRendererList(rParams);

        // Update the builder settings and set the render function.
        builder.UseAllGlobalTextures(true);
        builder.SetRenderAttachment(filteredColor, 0, AccessFlags.Write);
        builder.SetRenderAttachment(filteredDepth, 1, AccessFlags.Write);
        builder.SetRenderAttachment(filteredNormals, 2, AccessFlags.Write);
        builder.UseRendererList(passData.listHandle);
        builder.SetRenderFunc(
            (PassData data, RasterGraphContext context) =>
            {
                context.cmd.DrawRendererList(data.listHandle);
            }
        );

        // Set the transfer frame data.
        transferData.filteredTexture = filteredColor;
        transferData.filteredDepth = filteredDepth;
        transferData.filteredNormals = filteredNormals;
    }
}

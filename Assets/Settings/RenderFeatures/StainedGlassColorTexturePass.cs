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

    // Function used to transfer the material from the renderer feature to the render pass.
    public void Setup(LayerMask mask, Material oMaterial)
    {
        layerMask = mask;
        overrideMat = oMaterial;

        //The pass will read the current color texture. That needs to be an intermediate texture. It's not supported to use the BackBuffer as input texture.
        //By setting this property, URP will automatically create an intermediate texture. This has a performance cost so don't set this if you don't need it.
        //It's good practice to set it here and not from the RenderFeature. This way, the pass is selfcontaining and you can use it to directly enqueue the pass from a monobehaviour without a RenderFeature.
        requiresIntermediateTexture = true;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        // Get frame data.
        var cameraData = frameData.Get<UniversalCameraData>();
        var resourceData = frameData.Get<UniversalResourceData>();
        var renderingData = frameData.Get<UniversalRenderingData>();
        var lightData = frameData.Get<UniversalLightData>();
        var transferData = frameData.Create<FilteredTextureData>();

        // Create the layer texture for color.
        var destinationDesc = resourceData.activeColorTexture.GetDescriptor(renderGraph);
        destinationDesc.name = "Filtered Color Texture";

        var layerTexture = renderGraph.CreateTexture(destinationDesc);

        // Create the depth texture.
        var depthDesc = destinationDesc;
        depthDesc.name = "Filtered Depth Texture";
        depthDesc.colorFormat = GraphicsFormat.R32_SFloat;
        var filteredDepth = renderGraph.CreateTexture(depthDesc);

        // Create the normal texture.
        var normalDesc = destinationDesc;
        normalDesc.name = "Filtered Normals Texture";
        normalDesc.colorFormat = GraphicsFormat.R8G8B8A8_UNorm;
        var filteredNormals = renderGraph.CreateTexture(normalDesc);

        // Create the render pass.
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
        drawSettings.overrideMaterial = overrideMat;

        RendererListParams rParams = new RendererListParams(
            renderingData.cullResults,
            drawSettings,
            filterSettings
        );

        //Set the pass data.
        passData.listHandle = renderGraph.CreateRendererList(rParams);

        builder.UseAllGlobalTextures(true);
        builder.SetRenderAttachment(layerTexture, 0, AccessFlags.Write);
        builder.SetRenderAttachment(filteredDepth, 1, AccessFlags.Write);
        builder.SetRenderAttachment(filteredNormals, 2, AccessFlags.Write); //???
        builder.UseRendererList(passData.listHandle);
        builder.SetRenderFunc(
            (PassData data, RasterGraphContext context) =>
            {
                context.cmd.DrawRendererList(data.listHandle);
            }
        );

        // Set the transfer data.
        transferData.filteredTexture = layerTexture;
        transferData.filteredDepth = filteredDepth;
        transferData.filteredNormals = filteredNormals;
    }
}

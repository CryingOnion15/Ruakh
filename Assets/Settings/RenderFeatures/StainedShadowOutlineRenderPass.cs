using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class StainedShadowOutlineRenderPass : ScriptableRenderPass
{
    Material outlineBlitMaterial;

    TextureDesc outlineDesc;

    TextureHandle outlineTexture;

    class PassData { }

    // Setup the render pass with necessary data.
    public void Setup(Material blitMaterial, EdgeDetectionSettings settings, int mapResolution)
    {
        outlineBlitMaterial = blitMaterial;

        // Set Material values
        outlineBlitMaterial.SetFloat("_OutlineThickness", settings.edgeThickness);
        outlineBlitMaterial.SetFloat("_depthThreshold", settings.depthThreshold);
        outlineBlitMaterial.SetFloat("_normalThreshold", settings.normalThreshold);
        outlineBlitMaterial.SetFloat("_colorThreshold", settings.colorThreshold);

        requiresIntermediateTexture = true;

        // Create the color desc.
        outlineDesc = new TextureDesc(mapResolution, mapResolution)
        {
            colorFormat = GraphicsFormat.R32G32B32A32_SFloat,
            depthBufferBits = DepthBits.None,
            dimension = TextureDimension.Tex2D,
            name = "_StainedShadowOutlineMap",
            clearBuffer = true,
            clearColor = Color.clear,
        };
    }

    // Override to define the render pass instructions.
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        // Get frame data.
        StainedGlassData glassData = frameData.Get<StainedGlassData>();

        // Create the render pass.
        using var builder = renderGraph.AddRasterRenderPass<PassData>(
            "Shadow Pass - Draw Stained Shadows Outlines",
            out var data
        );

        // Create Texture Handle
        outlineTexture = renderGraph.CreateTexture(outlineDesc);

        // Set the builder settings and set the render functions.
        builder.UseTexture(glassData.lightLumTextureHandle);
        builder.UseTexture(glassData.lightDepthTextureHandle);
        builder.UseTexture(glassData.lightNormalTextureHandle);
        builder.SetRenderAttachment(outlineTexture, 0);
        builder.SetGlobalTextureAfterPass(
            outlineTexture,
            Shader.PropertyToID("_LightSpaceOutlineTexture")
        );
        builder.AllowPassCulling(false);
        builder.SetRenderFunc(
            (PassData data, RasterGraphContext context) =>
            {
                outlineBlitMaterial.SetTexture("_ColorTex", glassData.lightLumTextureHandle);
                outlineBlitMaterial.SetTexture("_DepthTex", glassData.lightDepthTextureHandle);
                outlineBlitMaterial.SetTexture("_NormalTex", glassData.lightNormalTextureHandle);
                Blitter.BlitTexture(context.cmd, Vector4.one, outlineBlitMaterial, 0);
                Blitter.Cleanup();
            }
        );
    }
}

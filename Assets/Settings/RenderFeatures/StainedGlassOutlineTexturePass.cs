using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class StainedGlassOutlineTexturePass : ScriptableRenderPass
{
    Material outlineBlitMaterial;

    class PassData { }

    // Setup function for the render pass.
    public void Setup(Material blit, EdgeDetectionSettings settings)
    {
        outlineBlitMaterial = blit;

        // Set Material values
        outlineBlitMaterial.SetFloat("_OutlineThickness", settings.edgeThickness);
        outlineBlitMaterial.SetFloat("_depthThreshold", settings.depthThreshold);
        outlineBlitMaterial.SetFloat("_normalThreshold", settings.normalThreshold);
        outlineBlitMaterial.SetFloat("_colorThreshold", settings.colorThreshold);

        requiresIntermediateTexture = true;
    }

    // Override to define the render pass instructions.
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var glassData = frameData.Get<StainedGlassData>();
        var cameraData = frameData.Get<UniversalCameraData>();

        var screenSpaceOutlineDesc = new TextureDesc(
            cameraData.camera.scaledPixelWidth,
            cameraData.camera.scaledPixelHeight
        )
        {
            colorFormat = GraphicsFormat.R32G32B32A32_SFloat,
            depthBufferBits = DepthBits.None,
            dimension = TextureDimension.Tex2D,
            name = "_ScreenSpaceOutlineTexture",
            clearBuffer = true,
            clearColor = Color.clear,
        };

        var screenSpaceOutlineTexture = renderGraph.CreateTexture(screenSpaceOutlineDesc);

        var blitParams = new RenderGraphUtils.BlitMaterialParameters(
            glassData.screenColorTextureHandle,
            screenSpaceOutlineTexture,
            outlineBlitMaterial,
            0
        );

        using var builder = renderGraph.AddRasterRenderPass<PassData>(
            "Outline Pass - Create Outline Texture.",
            out PassData data
        );

        // TODO Add Global Texture Assignment
        builder.UseTexture(glassData.screenColorTextureHandle);
        builder.UseTexture(glassData.screenDepthTextureHandle);
        builder.UseTexture(glassData.screenNormalTextureHandle);
        builder.SetRenderAttachment(screenSpaceOutlineTexture, 0);
        builder.SetGlobalTextureAfterPass(
            screenSpaceOutlineTexture,
            Shader.PropertyToID("_ScreenSpaceOutlineTexture")
        );
        builder.AllowPassCulling(false);
        builder.SetRenderFunc(
            (PassData data, RasterGraphContext context) =>
            {
                outlineBlitMaterial.SetTexture("_ColorTex", glassData.screenColorTextureHandle);
                outlineBlitMaterial.SetTexture("_DepthTex", glassData.screenDepthTextureHandle);
                outlineBlitMaterial.SetTexture("_NormalTex", glassData.screenNormalTextureHandle);
                Blitter.BlitTexture(context.cmd, Vector4.one, outlineBlitMaterial, 0);
                Blitter.Cleanup();
            }
        );
    }
}

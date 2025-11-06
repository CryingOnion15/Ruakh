using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class StainedGlassPostProcessPass : ScriptableRenderPass
{
    protected Material postProcessMaterial;

    // Unused
    class PassData { }

    // Setup the render pass with necessary data.
    public void Setup(Material ppMat, Color outlineColor)
    {
        postProcessMaterial = ppMat;
        postProcessMaterial.SetColor("_OutlineColor", outlineColor);
        requiresIntermediateTexture = true;
    }

    // Override to define the render pass instructions.
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        // Name the render pass.
        string passName = "Stained Glass - Post Process";

        // Get frame data.
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

        // Create the render pass.
        using var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var data);

        // Set the builder settings and set the render functions.
        builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
        builder.UseTexture(resourceData.activeDepthTexture);
        builder.AllowGlobalStateModification(true);
        builder.AllowPassCulling(false);
        builder.SetRenderFunc(
            (PassData data, RasterGraphContext context) =>
            {
                Blitter.BlitTexture(context.cmd, Vector4.zero, postProcessMaterial, 0);
                Blitter.Cleanup();
            }
        );
    }
}

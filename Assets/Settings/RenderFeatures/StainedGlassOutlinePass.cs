using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

[Serializable]
// Settings class used for edge detection properties.
public class EdgeDetectionSettings
{
    public int edgeThickness = 3;
    public Color edgeColor = Color.black;
    public float depthThreshold = .001f;
    public float normalThreshold = .25f;
    public float colorThreshold = 2.0f;
}

public class StainedGlassOutlinePass : ScriptableRenderPass
{
    // Material used in the blit operation.
    Material material;

    // Used to find properties on a shader.
    private static readonly int OutlineThicknessProperty = Shader.PropertyToID("_OutlineThickness");
    private static readonly int OutlineColorProperty = Shader.PropertyToID("_OutlineColor");
    private static readonly int DepthThresholdProperty = Shader.PropertyToID("_depthThreshold");
    private static readonly int NormalThresholdProperty = Shader.PropertyToID("_normalThreshold");
    private static readonly int ColorThresholdProperty = Shader.PropertyToID("_colorThreshold");

    class PassData
    {
        internal Material blitMaterial;
    }

    // Setup the render pass with necessary data.
    public void Setup(Material mat, EdgeDetectionSettings settings)
    {
        material = mat;
        material.SetFloat(OutlineThicknessProperty, settings.edgeThickness);
        material.SetColor(OutlineColorProperty, settings.edgeColor);
        material.SetFloat(DepthThresholdProperty, settings.depthThreshold);
        material.SetFloat(NormalThresholdProperty, settings.normalThreshold);
        material.SetFloat(ColorThresholdProperty, settings.colorThreshold);

        //The pass will read the current color texture. That needs to be an intermediate texture. It's not supported to use the BackBuffer as input texture.
        //By setting this property, URP will automatically create an intermediate texture. This has a performance cost so don't set this if you don't need it.
        //It's good practice to set it here and not from the RenderFeature. This way, the pass is selfcontaining and you can use it to directly enqueue the pass from a monobehaviour without a RenderFeature.
        requiresIntermediateTexture = true;
    }

    // Override to define the render pass instructions.
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        // Name the render pass.
        string passName = "Outline Pass - Copy Active Color";

        // Get the frame data needed.
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        FilteredTextureData textureData = frameData.Get<FilteredTextureData>();

        // Create the render pass.
        using var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var data);

        // Assign the material to the pass data.
        data.blitMaterial = material;

        // Create a target texture to assing to the render attachment.
        var target = renderGraph.CreateTexture(resourceData.cameraColor.GetDescriptor(renderGraph));

        // Set the builder settings and set the render functions.
        builder.SetRenderAttachment(target, 0, AccessFlags.ReadWrite);
        builder.UseTexture(textureData.filteredTexture);
        builder.UseTexture(textureData.filteredDepth);
        builder.UseTexture(textureData.filteredNormals);
        builder.UseAllGlobalTextures(true);
        builder.AllowPassCulling(false);
        builder.SetRenderFunc(
            (PassData data, RasterGraphContext context) =>
            {
                // Set the material textures.
                material.SetTexture("_FilteredColor", textureData.filteredTexture);
                material.SetTexture("_FilteredDepth", textureData.filteredDepth);
                material.SetTexture("_FilteredNormals", textureData.filteredNormals);

                // Blit the target texture with via the material.
                Blitter.BlitTexture(context.cmd, target, Vector2.one, data.blitMaterial, 0);
            }
        );

        // Set the result to the camera color.
        resourceData.cameraColor = target;
    }
}

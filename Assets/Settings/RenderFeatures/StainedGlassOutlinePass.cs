using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

[Serializable]
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

    private static readonly int OutlineThicknessProperty = Shader.PropertyToID("_OutlineThickness");
    private static readonly int OutlineColorProperty = Shader.PropertyToID("_OutlineColor");
    private static readonly int DepthThresholdProperty = Shader.PropertyToID("_depthThreshold");
    private static readonly int NormalThresholdProperty = Shader.PropertyToID("_normalThreshold");
    private static readonly int ColorThresholdProperty = Shader.PropertyToID("_colorThreshold");

    class PassData
    {
        internal Material blitMaterial;
    }

    // Function used to transfer the material from the renderer feature to the render pass.
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

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        string passName = "Outline Pass - Copy Active Color";
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        var textureData = frameData.Get<FilteredTextureData>();

        // Create the render pass.
        using var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var data);

        //Assign the material to the pass data.
        data.blitMaterial = material;

        // Set the builder settings.
        var target = renderGraph.CreateTexture(resourceData.cameraColor.GetDescriptor(renderGraph));

        builder.SetRenderAttachment(target, 0, AccessFlags.ReadWrite);
        //builder.UseTexture(resourceData.cameraColor);
        builder.UseTexture(textureData.filteredTexture);
        builder.UseTexture(textureData.filteredDepth);
        builder.UseTexture(textureData.filteredNormals);

        //builder.UseTexture(textureData.filteredTexture);
        builder.UseAllGlobalTextures(true);
        builder.AllowPassCulling(false);
        builder.SetRenderFunc(
            (PassData data, RasterGraphContext context) =>
            {
                material.SetTexture("_FilteredColor", textureData.filteredTexture);
                material.SetTexture("_FilteredDepth", textureData.filteredDepth);
                material.SetTexture("_FilteredNormals", textureData.filteredNormals);

                Blitter.BlitTexture(context.cmd, target, Vector2.one, data.blitMaterial, 0);
            }
        );

        resourceData.cameraColor = target;
    }
}

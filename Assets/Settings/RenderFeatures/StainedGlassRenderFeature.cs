using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

[Serializable]
// Settings class used for edge detection properties.
public class EdgeDetectionSettings
{
    public float edgeThickness = 3;
    public Color edgeColor = Color.black;
    public float depthThreshold = .001f;
    public float normalThreshold = .25f;
    public float colorThreshold = 2.0f;
}

// Data class used to define the ContextItem to pass between the render passes.
public class StainedGlassData : ContextItem
{
    public TextureHandle screenColorTextureHandle; // In lumenaince
    public TextureHandle screenDepthTextureHandle;
    public TextureHandle screenNormalTextureHandle;

    public TextureHandle lightColorTextureHandle;
    public TextureHandle lightLumTextureHandle;
    public TextureHandle lightDepthTextureHandle;
    public TextureHandle lightNormalTextureHandle;

    public override void Reset()
    {
        screenDepthTextureHandle = TextureHandle.nullHandle;
        screenColorTextureHandle = TextureHandle.nullHandle;
        screenNormalTextureHandle = TextureHandle.nullHandle;
        lightDepthTextureHandle = TextureHandle.nullHandle;
        lightColorTextureHandle = TextureHandle.nullHandle;
        lightLumTextureHandle = TextureHandle.nullHandle;
        lightNormalTextureHandle = TextureHandle.nullHandle;
    }
}

public class StainedGlassRenderFeature : ScriptableRendererFeature
{
    [SerializeField]
    RenderPassEvent outlinePassEvent = RenderPassEvent.AfterRenderingShadows;

    [SerializeField]
    RenderPassEvent maskEvent = RenderPassEvent.AfterRenderingOpaques;

    [SerializeField]
    RenderPassEvent postProcessEvent = RenderPassEvent.AfterRenderingPostProcessing;

    [Header("Outline & Shadow Texture Fields")]
    [SerializeField]
    LayerMask outlineMask;

    [SerializeField]
    LayerMask allOpaqueMask;

    [SerializeField]
    Material dataOverrideMaterial;

    [SerializeField]
    Material shadowMaskOverrideMaterial;

    [SerializeField]
    Material outlineBlitMaterial;

    [SerializeField]
    Material shadowDataOverrideMaterial;

    [SerializeField]
    Material shadowOutlineBlitMaterial;

    [SerializeField]
    EdgeDetectionSettings outlineSettings;

    [SerializeField]
    Vector3 OrthoPaddingMultiplier = new Vector3(0.02f, 0.02f, 0.02f);

    [SerializeField]
    int shadowMapResolution = 1024;

    [Header("Post Process Fields")]
    [SerializeField]
    Material postProcessDrawMaterial;

    // Render Passes
    StainedGlassColorTexturePass texturePass;

    StainedGlassOutlineTexturePass outlinePass;

    protected StainedShadowRenderPass shadowRenderPass;
    protected StainedShadowMaskPass shadowMaskPass;
    protected StainedShadowOutlineRenderPass shadowOutlinePass;

    StainedGlassPostProcessPass postProcessPass;

    /// <inheritdoc/>
    public override void Create()
    {
        // Create Passes
        texturePass = new StainedGlassColorTexturePass();
        postProcessPass = new StainedGlassPostProcessPass();
        outlinePass = new StainedGlassOutlineTexturePass();
        shadowRenderPass = new StainedShadowRenderPass();
        shadowMaskPass = new StainedShadowMaskPass();
        shadowOutlinePass = new StainedShadowOutlineRenderPass();

        // Set Pass Events.
        texturePass.renderPassEvent = outlinePassEvent;
        outlinePass.renderPassEvent = outlinePassEvent;
        shadowRenderPass.renderPassEvent = outlinePassEvent;
        shadowOutlinePass.renderPassEvent = outlinePassEvent;

        shadowMaskPass.renderPassEvent = maskEvent;

        postProcessPass.renderPassEvent = postProcessEvent;
    }

    /// <summary>
    /// Add all render passes to the feature.
    /// </summary>
    /// <param name="renderer"></param>
    /// <param name="renderingData"></param>
    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData
    )
    {
        if (
            RenderSettings.sun != null
            && dataOverrideMaterial != null
            && outlineBlitMaterial != null
            && postProcessDrawMaterial != null
            && shadowOutlineBlitMaterial != null
            && shadowDataOverrideMaterial != null
            && shadowMaskOverrideMaterial != null
        )
        {
            // Setup passed with needed information.
            texturePass.Setup(outlineMask, dataOverrideMaterial);
            outlinePass.Setup(outlineBlitMaterial, outlineSettings);
            postProcessPass.Setup(postProcessDrawMaterial, outlineSettings.edgeColor);
            shadowRenderPass.Setup(
                RenderSettings.sun,
                outlineMask,
                shadowDataOverrideMaterial,
                shadowMapResolution,
                OrthoPaddingMultiplier
            );
            shadowOutlinePass.Setup(
                shadowOutlineBlitMaterial,
                outlineSettings,
                shadowMapResolution
            );
            shadowMaskPass.Setup(allOpaqueMask, shadowMaskOverrideMaterial);

            //Queue Camera outline passes.
            renderer.EnqueuePass(texturePass);
            renderer.EnqueuePass(outlinePass);

            // Queue Shadow outline passes.
            renderer.EnqueuePass(shadowRenderPass);

            // Queue shadow mask pass.
            renderer.EnqueuePass(shadowMaskPass);

            // Queue post process draw pass.
            postProcessPass.ConfigureInput(ScriptableRenderPassInput.Depth);
            // Queue Post Process Pass.
            renderer.EnqueuePass(postProcessPass);
        }
    }
}

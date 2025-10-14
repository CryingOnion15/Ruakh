using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class FilteredTextureData : ContextItem
{
    public TextureHandle filteredTexture;
    public TextureHandle filteredDepth;
    public TextureHandle filteredNormals;

    public override void Reset()
    {
        filteredTexture = TextureHandle.nullHandle;
        filteredDepth = TextureHandle.nullHandle;
        filteredNormals = TextureHandle.nullHandle;
    }
}

public class StainedGlassOpaqueFeature : ScriptableRendererFeature
{
    // Inputs in the inspector to change the settings for the renderer feature.
    [SerializeField]
    RenderPassEvent m_PassEvent = RenderPassEvent.AfterRenderingTransparents;

    [Header("Texture Filter")]
    [SerializeField]
    LayerMask layerMask;

    [SerializeField]
    Material textureOverideMaterial;

    [Header("Outline Draw")]
    [SerializeField]
    Material outlineMaterial;

    [SerializeField]
    EdgeDetectionSettings outlineSettings;

    StainedGlassColorTexturePass texturePass;
    StainedGlassOutlinePass outlinePass;

    /// <inheritdoc/>
    public override void Create()
    {
        texturePass = new StainedGlassColorTexturePass();
        outlinePass = new StainedGlassOutlinePass();

        // Configures where the render pass should be injected.
        outlinePass.renderPassEvent = m_PassEvent;
        texturePass.renderPassEvent = m_PassEvent;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData
    )
    {
        // Early exit if override material is null.
        if (textureOverideMaterial == null)
        {
            Debug.LogWarning("Texture override material is null and will be skipped.");
            return;
        }

        // Early exit if outline material is null.
        if (outlineMaterial == null)
        {
            Debug.LogWarning("Outline material is null and will be skipped.");
            return;
        }

        //Enque the render passes.
        texturePass.ConfigureInput(
            ScriptableRenderPassInput.Depth
                | ScriptableRenderPassInput.Normal
                | ScriptableRenderPassInput.Color
        );
        // Perform the filtering pass.
        texturePass.Setup(layerMask, textureOverideMaterial);
        renderer.EnqueuePass(texturePass);

        // Perform the outline pass.
        outlinePass.ConfigureInput(
            ScriptableRenderPassInput.Depth
                | ScriptableRenderPassInput.Normal
                | ScriptableRenderPassInput.Color
        );
        outlinePass.Setup(outlineMaterial, outlineSettings);
        renderer.EnqueuePass(outlinePass);
    }
}

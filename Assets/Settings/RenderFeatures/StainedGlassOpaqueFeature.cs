using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// Data class used to define the ContextItem to pass between the render passes.
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

    // Render Passes
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

        // Setup and configure the filering pass.
        texturePass.ConfigureInput(
            ScriptableRenderPassInput.Depth
                | ScriptableRenderPassInput.Normal
                | ScriptableRenderPassInput.Color
        );
        texturePass.Setup(layerMask, textureOverideMaterial);

        // Queue the filtering pass.
        renderer.EnqueuePass(texturePass);

        // Setup and configure the outline pass.
        outlinePass.ConfigureInput(
            ScriptableRenderPassInput.Depth
                | ScriptableRenderPassInput.Normal
                | ScriptableRenderPassInput.Color
        );
        outlinePass.Setup(outlineMaterial, outlineSettings);

        // Queue the outline pass.
        renderer.EnqueuePass(outlinePass);
    }
}

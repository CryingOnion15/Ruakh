// using UnityEngine;
// using UnityEngine.Rendering.Universal;

// public class StainedShadowRenderFeature : ScriptableRendererFeature
// {
//     [SerializeField]
//     RenderPassEvent m_PassEvent = RenderPassEvent.AfterRenderingTransparents;

//     [SerializeField]
//     LayerMask shadowMask;

//     protected StainedShadowRenderPass stainedShadowPass;
//     protected StainedShadowDrawRenderPass stainedDrawPass;

//     /// <inheritdoc/>
//     public override void Create()
//     {
//         stainedShadowPass = new StainedShadowRenderPass();
//         stainedShadowPass.Setup(RenderSettings.sun, shadowMask);

//         stainedDrawPass = new StainedShadowDrawRenderPass();
//         stainedDrawPass.Setup();

//         // Configures where the render pass should be injected.
//         stainedShadowPass.renderPassEvent = m_PassEvent;
//         stainedDrawPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
//     }

//     // Here you can inject one or multiple render passes in the renderer.
//     // This method is called when setting up the renderer once per-camera.
//     public override void AddRenderPasses(
//         ScriptableRenderer renderer,
//         ref RenderingData renderingData
//     )
//     {
//         if (RenderSettings.sun != null)
//         {
//             // Queue the outline pass.
//             renderer.EnqueuePass(stainedShadowPass);
//             renderer.EnqueuePass(stainedDrawPass);
//         }
//     }
// }

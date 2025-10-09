using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OceanFloorHaloFeature : ScriptableRendererFeature
{
    class HaloPass : ScriptableRenderPass
    {
        public ComputeShader compShader;
        public Material oceanFloorMaterial;

        RenderTexture edgeMask;
        RenderTexture haloResult;

        int detectKernel;
        int spreadKernel;
        int threadGroupX;
        int threadGroupY;

        public float threshold = 0.001f;
        public float radius = 5f;
        public float intensity = 1f;

        public void Setup(int width, int height, Camera camera)
        {
            // Render textures
            edgeMask = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat);
            edgeMask.enableRandomWrite = true;
            edgeMask.Create();

            haloResult = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat);
            haloResult.enableRandomWrite = true;
            haloResult.Create();

            // Compute shader kernels
            detectKernel = compShader.FindKernel("CSDetectEdges");
            spreadKernel = compShader.FindKernel("CSSpreadHalo");

            threadGroupX = Mathf.CeilToInt(width / 8f);
            threadGroupY = Mathf.CeilToInt(height / 8f);

            // Make sure depth texture is generated
            camera.depthTextureMode = DepthTextureMode.Depth;
        }

        public override void Execute(
            ScriptableRenderContext context,
            ref RenderingData renderingData
        )
        {
            if (compShader == null || oceanFloorMaterial == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get("HaloComputeShader");

            // Set compute shader params
            cmd.SetComputeTextureParam(compShader, detectKernel, "_Result", edgeMask);
            cmd.SetComputeTextureParam(
                compShader,
                detectKernel,
                "_CameraDepthTexture",
                Shader.GetGlobalTexture("_CameraDepthTexture")
            );
            cmd.SetComputeFloatParam(compShader, "_Threshold", threshold);
            cmd.SetComputeFloatParam(compShader, "_Radius", radius);
            cmd.SetComputeFloatParam(compShader, "_Intensity", intensity);

            Camera camera = renderingData.cameraData.camera;
            cmd.SetComputeFloatParam(compShader, "_CameraNear", camera.nearClipPlane);
            cmd.SetComputeFloatParam(compShader, "_CameraFar", camera.farClipPlane);

            // Dispatch compute
            cmd.DispatchCompute(compShader, detectKernel, threadGroupX, threadGroupY, 1);
            // Uncomment if using spreadKernel
            //cmd.SetComputeTextureParam(compShader, spreadKernel, "_EdgeMask", edgeMask);
            //cmd.SetComputeTextureParam(compShader, spreadKernel, "_Result", haloResult);
            //cmd.DispatchCompute(compShader, spreadKernel, threadGroupX, threadGroupY, 1);

            // Assign result to material
            oceanFloorMaterial.SetTexture("_GlowMask", edgeMask);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    public ComputeShader compShader;
    public Material oceanFloorMaterial;
    public float threshold = 0.001f;
    public float radius = 5f;
    public float intensity = 1f;

    HaloPass haloPass;

    public override void Create()
    {
        haloPass = new HaloPass
        {
            compShader = compShader,
            oceanFloorMaterial = oceanFloorMaterial,
            threshold = threshold,
            radius = radius,
            intensity = intensity,
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques,
        };
    }

    // Called by URP to inject the pass
    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData
    )
    {
        if (haloPass != null)
        {
            Camera cam = renderingData.cameraData.camera;
            haloPass.Setup(Screen.width, Screen.height, cam);
            renderer.EnqueuePass(haloPass);
        }
    }
}

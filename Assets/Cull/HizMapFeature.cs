using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HizMapFeature : ScriptableRendererFeature
{

    public RenderPassEvent pass_event = RenderPassEvent.BeforeRenderingPostProcessing;
    public ComputeShader BaseCs;

    public static int GetHiZMapSize(ref RenderTextureDescriptor desc)
    {
        var screenSize = Mathf.Max(desc.width, desc.height);
        var textureSize = Mathf.NextPowerOfTwo(screenSize);
        return textureSize;
    }

    class HizMapPass : ScriptableRenderPass
    {
        public ComputeShader BaseCs;
        public RenderTargetIdentifier color_rt;
        public int TextureSize;
        RenderTexture hiz_map;
        CommandBuffer cmd_;
        int size = 2048;
        public HizMapPass()
        {
            cmd_ = new CommandBuffer();
            cmd_.name = "HizMapCmd";
            hiz_map = new RenderTexture(size, size, 24);
            hiz_map.enableRandomWrite = true;
            hiz_map.Create();
        }
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {

        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ////cmd_.Blit(renderingData.cameraData.targetTexture, hiz_map);
            //cmd_.SetComputeTextureParam(BaseCs, 0, Shader.PropertyToID("Result"), hiz_map);
            //cmd_.DispatchCompute(BaseCs, 0, 2048 / 8, 2048 / 8, 1);
            cmd_.Blit(color_rt, hiz_map);
            cmd_.Blit(hiz_map, color_rt);

            context.ExecuteCommandBuffer(cmd_);
            cmd_.Clear();
        }

        /// Cleanup any allocated resources that were created during the execution of this render pass.
        public override void FrameCleanup(CommandBuffer cmd)
        {
        }
    }

    HizMapPass hiz_pass;

    public override void Create()
    {
        hiz_pass = new HizMapPass();
        // Configures where the render pass should be injected.
        hiz_pass.renderPassEvent = pass_event;
        hiz_pass.BaseCs = BaseCs;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        hiz_pass.color_rt = renderer.cameraColorTarget;
        renderer.EnqueuePass(hiz_pass);
    }
}



using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FullscreenBlurFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material blurMaterial;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRendering;
    }

    public Settings settings = new Settings();

    class BlurPass : ScriptableRenderPass
    {
        private Material material;
        private RTHandle source;
        private RTHandle tempTexture;

        public BlurPass(Material material)
        {
            this.material = material;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // Game + Base Camera만 처리
            if (renderingData.cameraData.cameraType != CameraType.Game ||
                renderingData.cameraData.renderType != CameraRenderType.Base)
            {
                source = null;
                return;
            }

            source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;

            RenderingUtils.ReAllocateIfNeeded(
                ref tempTexture,
                desc,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_TempFullscreenBlurTex"
            );
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null || source == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get("Fullscreen Blur");

            Blitter.BlitCameraTexture(cmd, source, tempTexture, material, 0);
            Blitter.BlitCameraTexture(cmd, tempTexture, source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    private BlurPass blurPass;

    public override void Create()
    {
        blurPass = new BlurPass(settings.blurMaterial)
        {
            renderPassEvent = settings.renderPassEvent
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.blurMaterial == null)
            return;

        // ❗ 여기서는 cameraColorTargetHandle에 절대 접근하지 않음
        renderer.EnqueuePass(blurPass);
    }
}



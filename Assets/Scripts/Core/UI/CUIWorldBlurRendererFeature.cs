using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

///<summary>
/// UI 월드 블러 텍스처 캡처 렌더러 피처
///</summary>
public sealed class CUIWorldBlurRendererFeature : ScriptableRendererFeature
{
    private const string GlobalTextureName = "_UIWorldBlurTexture";
    private const string TargetShaderName = "TinyHero/UI/World Blur Overlay";

    [Serializable]
    private enum eDownsample
    {
        FULL = 1,
        HALF = 2,
        QUARTER = 4
    }

    [Serializable]
    private sealed class CaptureSettings
    {
        [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        [SerializeField] private eDownsample downsample = eDownsample.HALF;

        public RenderPassEvent RenderPassEvent => renderPassEvent;
        public int DownsampleValue => ( int ) downsample;
    }

    private sealed class CapturePass : ScriptableRenderPass
    {
        private int globalTextureShaderId;
        private int downsampleValue;
        private RTHandle outputHandle;

        ///<summary>
        /// 월드 블러 캡처 패스 구성
        /// </summary>
        public void Setup( int _globalTextureShaderId, int _downsampleValue, RTHandle _outputHandle )
        {
            globalTextureShaderId = _globalTextureShaderId;
            downsampleValue = Mathf.Max( 1, _downsampleValue );
            outputHandle = _outputHandle;
            ConfigureInput( ScriptableRenderPassInput.Color );
        }

        ///<summary>
        /// 렌더 그래프 기반 월드 블러 텍스처 생성
        /// </summary>
        public override void RecordRenderGraph( RenderGraph _renderGraph, ContextContainer _frameData )
        {
            UniversalResourceData resourceData = _frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = _frameData.Get<UniversalCameraData>();
            TextureHandle sourceTextureHandle = resourceData.activeColorTexture;
            TextureHandle destinationTextureHandle;
            RenderTextureDescriptor textureDescriptor = cameraData.cameraTargetDescriptor;
            int textureWidth;
            int textureHeight;

            if ( resourceData.isActiveTargetBackBuffer )
            {
                return;
            }

            if ( !sourceTextureHandle.IsValid() )
            {
                return;
            }

            textureWidth = Mathf.Max( 1, textureDescriptor.width / downsampleValue );
            textureHeight = Mathf.Max( 1, textureDescriptor.height / downsampleValue );

            textureDescriptor.width = textureWidth;
            textureDescriptor.height = textureHeight;
            textureDescriptor.msaaSamples = 1;
            textureDescriptor.depthStencilFormat = GraphicsFormat.None;
            textureDescriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateHandleIfNeeded( ref outputHandle, textureDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: GlobalTextureName );
            Shader.SetGlobalTexture( globalTextureShaderId, outputHandle );

            destinationTextureHandle = _renderGraph.ImportTexture( outputHandle );

            _renderGraph.AddBlitPass( sourceTextureHandle, destinationTextureHandle, Vector2.one, Vector2.zero, passName: "UI World Blur Capture" );
        }
    }

    [SerializeField] private CaptureSettings captureSettings = new CaptureSettings();

    private CapturePass capturePass;
    private int globalTextureShaderId;
    private RTHandle outputHandle;

    ///<summary>
    /// 렌더러 피처 초기 구성
    /// </summary>
    public override void Create()
    {
        globalTextureShaderId = Shader.PropertyToID( GlobalTextureName );

        if ( capturePass == null )
        {
            capturePass = new CapturePass();
        }

        capturePass.renderPassEvent = captureSettings.RenderPassEvent;
    }

    ///<summary>
    /// 카메라별 월드 블러 캡처 패스 등록
    /// </summary>
    public override void AddRenderPasses( ScriptableRenderer _renderer, ref RenderingData _renderingData )
    {
        Camera targetCamera = _renderingData.cameraData.camera;
        RenderTextureDescriptor textureDescriptor;
        int textureWidth;
        int textureHeight;

        if ( targetCamera == null )
        {
            return;
        }

        if ( _renderingData.cameraData.cameraType != CameraType.Game )
        {
            return;
        }

        textureDescriptor = _renderingData.cameraData.cameraTargetDescriptor;
        textureWidth = Mathf.Max( 1, textureDescriptor.width / captureSettings.DownsampleValue );
        textureHeight = Mathf.Max( 1, textureDescriptor.height / captureSettings.DownsampleValue );
        textureDescriptor.width = textureWidth;
        textureDescriptor.height = textureHeight;
        textureDescriptor.msaaSamples = 1;
        textureDescriptor.depthStencilFormat = GraphicsFormat.None;
        textureDescriptor.depthBufferBits = 0;

        RenderingUtils.ReAllocateHandleIfNeeded( ref outputHandle, textureDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: GlobalTextureName );
        Shader.SetGlobalTexture( globalTextureShaderId, outputHandle );
        UpdateMaterialBindings();
        capturePass.Setup( globalTextureShaderId, captureSettings.DownsampleValue, outputHandle );
        _renderer.EnqueuePass( capturePass );
    }

    ///<summary>
    /// 월드 블러 셰이더 머티리얼 텍스처 바인딩 갱신
    /// </summary>
    private void UpdateMaterialBindings()
    {
        Material[] materials = Resources.FindObjectsOfTypeAll<Material>();

        if ( outputHandle == null )
        {
            return;
        }

        for ( int index = 0; index < materials.Length; index++ )
        {
            Material material = materials[ index ];

            if ( material == null )
            {
                continue;
            }

            if ( material.shader == null )
            {
                continue;
            }

            if ( material.shader.name != TargetShaderName )
            {
                continue;
            }

            if ( !material.HasProperty( globalTextureShaderId ) )
            {
                continue;
            }

            material.SetTexture( globalTextureShaderId, outputHandle );
        }
    }

    ///<summary>
    /// 렌더러 피처 정리
    /// </summary>
    protected override void Dispose( bool _disposing )
    {
        if ( outputHandle != null )
        {
            outputHandle.Release();
            outputHandle = null;
        }
    }
}

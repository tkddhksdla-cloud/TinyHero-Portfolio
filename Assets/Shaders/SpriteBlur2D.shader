Shader "TinyHero/Sprite Blur 2D"
{
    Properties
    {
        [ MainTexture ] _MainTex ( "Sprite Texture", 2D ) = "white" {}
        _BlurSize ( "Blur Size", Range( 0, 8 ) ) = 1
        _AlphaScale ( "Alpha Scale", Range( 0, 1 ) ) = 1
        [ MaterialToggle ] _ZWrite ( "ZWrite", Float ) = 0

        [ HideInInspector ] _Color ( "Tint", Color ) = ( 1, 1, 1, 1 )
        [ HideInInspector ] PixelSnap ( "Pixel snap", Float ) = 0
        [ HideInInspector ] _RendererColor ( "RendererColor", Color ) = ( 1, 1, 1, 1 )
        [ HideInInspector ] _AlphaTex ( "External Alpha", 2D ) = "white" {}
        [ HideInInspector ] _EnableExternalAlpha ( "Enable External Alpha", Float ) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [ _ZWrite ]

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            TEXTURE2D( _MainTex );
            SAMPLER( sampler_MainTex );
            UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX( _MainTex );

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            CBUFFER_START( UnityPerMaterial )
                half4 _Color;
                float4 _MainTex_TexelSize;
                half _BlurSize;
                half _AlphaScale;
            CBUFFER_END

            /// <summary>
            /// 스프라이트 정점 데이터를 월드 렌더링용 출력으로 변환한다.
            /// </summary>
            Varyings UnlitVertex( Attributes input )
            {
                UNITY_SKINNED_VERTEX_COMPUTE( input );
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite( input.positionOS, unity_SpriteProps.xy );

                Varyings output = CommonUnlitVertex( input );
                output.color = input.color * _Color * unity_SpriteColor;

                return output;
            }

            /// <summary>
            /// 스프라이트 주변 텍셀을 샘플링해 블러된 색상을 계산한다.
            /// </summary>
            half4 SampleBlurColor( float2 uv )
            {
                float2 blurOffset = _MainTex_TexelSize.xy * _BlurSize;

                half4 centerSample = SAMPLE_TEXTURE2D( _MainTex, sampler_MainTex, uv ) * 0.22702703h;
                half4 horizontalPositiveSample = SAMPLE_TEXTURE2D( _MainTex, sampler_MainTex, uv + float2( blurOffset.x, 0.0f ) ) * 0.19459459h;
                half4 horizontalNegativeSample = SAMPLE_TEXTURE2D( _MainTex, sampler_MainTex, uv - float2( blurOffset.x, 0.0f ) ) * 0.19459459h;
                half4 verticalPositiveSample = SAMPLE_TEXTURE2D( _MainTex, sampler_MainTex, uv + float2( 0.0f, blurOffset.y ) ) * 0.19459459h;
                half4 verticalNegativeSample = SAMPLE_TEXTURE2D( _MainTex, sampler_MainTex, uv - float2( 0.0f, blurOffset.y ) ) * 0.19459459h;
                half4 diagonalTopRightSample = SAMPLE_TEXTURE2D( _MainTex, sampler_MainTex, uv + blurOffset ) * 0.06081081h;
                half4 diagonalBottomLeftSample = SAMPLE_TEXTURE2D( _MainTex, sampler_MainTex, uv - blurOffset ) * 0.06081081h;
                half4 diagonalTopLeftSample = SAMPLE_TEXTURE2D( _MainTex, sampler_MainTex, uv + float2( -blurOffset.x, blurOffset.y ) ) * 0.06081081h;
                half4 diagonalBottomRightSample = SAMPLE_TEXTURE2D( _MainTex, sampler_MainTex, uv + float2( blurOffset.x, -blurOffset.y ) ) * 0.06081081h;

                half4 blurColor = centerSample;
                blurColor += horizontalPositiveSample;
                blurColor += horizontalNegativeSample;
                blurColor += verticalPositiveSample;
                blurColor += verticalNegativeSample;
                blurColor += diagonalTopRightSample;
                blurColor += diagonalBottomLeftSample;
                blurColor += diagonalTopLeftSample;
                blurColor += diagonalBottomRightSample;

                return blurColor;
            }

            /// <summary>
            /// 블러된 스프라이트 텍스처와 틴트 색상을 합성해 최종 픽셀 색상을 만든다.
            /// </summary>
            half4 UnlitFragment( Varyings input ) : SV_Target
            {
                half4 blurColor = SampleBlurColor( input.uv );
                half4 finalColor = blurColor * input.color;
                finalColor.a *= _AlphaScale;

#if defined(DEBUG_DISPLAY)
                SurfaceData2D surfaceData;
                InputData2D inputData;
                half4 debugColor = 0;

                InitializeSurfaceData( finalColor.rgb, finalColor.a, surfaceData );
                InitializeInputData( input.uv, inputData );
                SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS( inputData, input.positionWS, input.positionCS, _MainTex );

                if ( CanDebugOverrideOutputColor( surfaceData, inputData, debugColor ) )
                {
                    return debugColor;
                }
#endif

                return finalColor;
            }
            ENDHLSL
        }
    }
}

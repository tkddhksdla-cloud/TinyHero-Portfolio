Shader "TinyHero/UI/World Blur Overlay"
{
    Properties
    {
        [PerRendererData] _MainTex ( "Sprite Texture", 2D ) = "white" {}
        _Color ( "Tint", Color ) = ( 1, 1, 1, 0.85 )
        _BlurSize ( "Blur Size", Range( 0, 8 ) ) = 1.5
        _AlphaScale ( "Alpha Scale", Range( 0, 1 ) ) = 1

        [HideInInspector] _UIWorldBlurTexture ( "World Blur Texture", 2D ) = "black" {}
        [HideInInspector] _TextureSampleAdd ( "Texture Sample Add", Vector ) = ( 0, 0, 0, 0 )

        _StencilComp ( "Stencil Comparison", Float ) = 8
        _Stencil ( "Stencil ID", Float ) = 0
        _StencilOp ( "Stencil Operation", Float ) = 0
        _StencilWriteMask ( "Stencil Write Mask", Float ) = 255
        _StencilReadMask ( "Stencil Read Mask", Float ) = 255
        _ColorMask ( "Color Mask", Float ) = 15

        [Toggle( UNITY_UI_CLIP_RECT )] _UseUIClipRect ( "Use UI Clip Rect", Float ) = 0
        [Toggle( UNITY_UI_ALPHACLIP )] _UseUIAlphaClip ( "Use UI Alpha Clip", Float ) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            sampler2D _UIWorldBlurTexture;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _UIWorldBlurTexture_TexelSize;
            half _BlurSize;
            half _AlphaScale;

            struct Attributes
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            /// <summary>
            /// UI 버텍스 데이터를 화면 블러 샘플링 데이터로 변환
            /// </summary>
            Varyings Vert( Attributes _input )
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID( _input );
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( output );

                output.worldPosition = _input.vertex;
                output.vertex = UnityObjectToClipPos( output.worldPosition );
                output.texcoord = _input.texcoord;
                output.color = _input.color * _Color;

                return output;
            }

            /// <summary>
            /// 화면 좌표 기준 월드 블러 샘플 색상 계산
            /// </summary>
            fixed4 SampleWorldBlurColor( float2 _screenUv )
            {
                float2 blurOffset = _UIWorldBlurTexture_TexelSize.xy * _BlurSize;
                fixed4 centerSample = tex2D( _UIWorldBlurTexture, _screenUv ) * 0.22702703h;
                fixed4 horizontalPositiveSample = tex2D( _UIWorldBlurTexture, _screenUv + float2( blurOffset.x, 0.0f ) ) * 0.19459459h;
                fixed4 horizontalNegativeSample = tex2D( _UIWorldBlurTexture, _screenUv - float2( blurOffset.x, 0.0f ) ) * 0.19459459h;
                fixed4 verticalPositiveSample = tex2D( _UIWorldBlurTexture, _screenUv + float2( 0.0f, blurOffset.y ) ) * 0.19459459h;
                fixed4 verticalNegativeSample = tex2D( _UIWorldBlurTexture, _screenUv - float2( 0.0f, blurOffset.y ) ) * 0.19459459h;
                fixed4 diagonalTopRightSample = tex2D( _UIWorldBlurTexture, _screenUv + blurOffset ) * 0.06081081h;
                fixed4 diagonalBottomLeftSample = tex2D( _UIWorldBlurTexture, _screenUv - blurOffset ) * 0.06081081h;
                fixed4 diagonalTopLeftSample = tex2D( _UIWorldBlurTexture, _screenUv + float2( -blurOffset.x, blurOffset.y ) ) * 0.06081081h;
                fixed4 diagonalBottomRightSample = tex2D( _UIWorldBlurTexture, _screenUv + float2( blurOffset.x, -blurOffset.y ) ) * 0.06081081h;
                fixed4 blurColor = centerSample;

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
            /// 월드 블러와 UI 마스크를 합성한 최종 색상 계산
            /// </summary>
            fixed4 Frag( Varyings _input ) : SV_Target
            {
                float2 screenUv = _input.vertex.xy / _ScreenParams.xy;
                fixed4 spriteSample = tex2D( _MainTex, _input.texcoord ) + _TextureSampleAdd;
                fixed4 blurColor;
                fixed4 finalColor;

                #if UNITY_UV_STARTS_AT_TOP
                screenUv.y = 1.0f - screenUv.y;
                #endif

                blurColor = SampleWorldBlurColor( screenUv );
                finalColor.rgb = blurColor.rgb * _input.color.rgb;
                finalColor.a = spriteSample.a * _input.color.a * _AlphaScale;

                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *= UnityGet2DClipping( _input.worldPosition.xy, _ClipRect );
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip( finalColor.a - 0.001h );
                #endif

                return finalColor;
            }
            ENDCG
        }
    }
}

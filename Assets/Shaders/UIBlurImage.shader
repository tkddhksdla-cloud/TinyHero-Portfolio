Shader "TinyHero/UI/Blur Image"
{
    Properties
    {
        [PerRendererData] _MainTex ( "Sprite Texture", 2D ) = "white" {}
        _Color ( "Tint", Color ) = ( 1, 1, 1, 1 )
        _BlurSize ( "Blur Size", Range( 0, 8 ) ) = 1
        _AlphaScale ( "Alpha Scale", Range( 0, 1 ) ) = 1

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
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_TexelSize;
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
            /// UI 버텍스 데이터를 화면 출력용 데이터로 변환
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
            /// UI 텍스처 블러 샘플 색상 계산
            /// </summary>
            fixed4 SampleBlurColor( float2 _uv )
            {
                float2 blurOffset;
                fixed4 centerSample;
                fixed4 horizontalPositiveSample;
                fixed4 horizontalNegativeSample;
                fixed4 verticalPositiveSample;
                fixed4 verticalNegativeSample;
                fixed4 diagonalTopRightSample;
                fixed4 diagonalBottomLeftSample;
                fixed4 diagonalTopLeftSample;
                fixed4 diagonalBottomRightSample;
                fixed4 blurColor;

                blurOffset = _MainTex_TexelSize.xy * _BlurSize;

                centerSample = ( tex2D( _MainTex, _uv ) + _TextureSampleAdd ) * 0.22702703h;
                horizontalPositiveSample = ( tex2D( _MainTex, _uv + float2( blurOffset.x, 0.0f ) ) + _TextureSampleAdd ) * 0.19459459h;
                horizontalNegativeSample = ( tex2D( _MainTex, _uv - float2( blurOffset.x, 0.0f ) ) + _TextureSampleAdd ) * 0.19459459h;
                verticalPositiveSample = ( tex2D( _MainTex, _uv + float2( 0.0f, blurOffset.y ) ) + _TextureSampleAdd ) * 0.19459459h;
                verticalNegativeSample = ( tex2D( _MainTex, _uv - float2( 0.0f, blurOffset.y ) ) + _TextureSampleAdd ) * 0.19459459h;
                diagonalTopRightSample = ( tex2D( _MainTex, _uv + blurOffset ) + _TextureSampleAdd ) * 0.06081081h;
                diagonalBottomLeftSample = ( tex2D( _MainTex, _uv - blurOffset ) + _TextureSampleAdd ) * 0.06081081h;
                diagonalTopLeftSample = ( tex2D( _MainTex, _uv + float2( -blurOffset.x, blurOffset.y ) ) + _TextureSampleAdd ) * 0.06081081h;
                diagonalBottomRightSample = ( tex2D( _MainTex, _uv + float2( blurOffset.x, -blurOffset.y ) ) + _TextureSampleAdd ) * 0.06081081h;

                blurColor = centerSample;
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
            /// UI 블러 최종 색상 계산
            /// </summary>
            fixed4 Frag( Varyings _input ) : SV_Target
            {
                fixed4 blurColor;
                fixed4 finalColor;

                blurColor = SampleBlurColor( _input.texcoord );
                finalColor = blurColor * _input.color;

                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *= UnityGet2DClipping( _input.worldPosition.xy, _ClipRect );
                #endif

                finalColor.a *= _AlphaScale;

                #ifdef UNITY_UI_ALPHACLIP
                clip( finalColor.a - 0.001h );
                #endif

                return finalColor;
            }
            ENDCG
        }
    }
}

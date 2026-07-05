#ifndef SPRITE_BLUR_2D_INCLUDED
#define SPRITE_BLUR_2D_INCLUDED

void SpriteBlur_float( Texture2D< float4 > Texture, float2 UV, float BlurSize, SamplerState Sampler, out float4 RGBA, out float R, out float G, out float B, out float A )
{
    uint width = 0;
    uint height = 0;
    Texture.GetDimensions( width, height );

    float2 texelSize = float2( 0.0, 0.0 );

    if ( width > 0 && height > 0 )
    {
        texelSize = float2( 1.0 / width, 1.0 / height ) * BlurSize;
    }

    float centerWeight = 0.22702703;
    float axialWeight = 0.19459459;
    float diagonalWeight = 0.06081081;
    float totalWeight = centerWeight + ( axialWeight * 4.0 ) + ( diagonalWeight * 4.0 );

    float4 blurColor = SAMPLE_TEXTURE2D( Texture, Sampler, UV ) * centerWeight;
    blurColor += SAMPLE_TEXTURE2D( Texture, Sampler, UV + float2( texelSize.x, 0.0 ) ) * axialWeight;
    blurColor += SAMPLE_TEXTURE2D( Texture, Sampler, UV - float2( texelSize.x, 0.0 ) ) * axialWeight;
    blurColor += SAMPLE_TEXTURE2D( Texture, Sampler, UV + float2( 0.0, texelSize.y ) ) * axialWeight;
    blurColor += SAMPLE_TEXTURE2D( Texture, Sampler, UV - float2( 0.0, texelSize.y ) ) * axialWeight;
    blurColor += SAMPLE_TEXTURE2D( Texture, Sampler, UV + texelSize ) * diagonalWeight;
    blurColor += SAMPLE_TEXTURE2D( Texture, Sampler, UV - texelSize ) * diagonalWeight;
    blurColor += SAMPLE_TEXTURE2D( Texture, Sampler, UV + float2( -texelSize.x, texelSize.y ) ) * diagonalWeight;
    blurColor += SAMPLE_TEXTURE2D( Texture, Sampler, UV + float2( texelSize.x, -texelSize.y ) ) * diagonalWeight;
    blurColor /= totalWeight;

    RGBA = blurColor;
    R = blurColor.r;
    G = blurColor.g;
    B = blurColor.b;
    A = blurColor.a;
}

void SpriteBlur_half( Texture2D< float4 > Texture, half2 UV, half BlurSize, SamplerState Sampler, out half4 RGBA, out half R, out half G, out half B, out half A )
{
    float4 blurColor;
    float red;
    float green;
    float blue;
    float alpha;

    SpriteBlur_float( Texture, UV, BlurSize, Sampler, blurColor, red, green, blue, alpha );

    RGBA = ( half4 ) blurColor;
    R = ( half ) red;
    G = ( half ) green;
    B = ( half ) blue;
    A = ( half ) alpha;
}

#endif

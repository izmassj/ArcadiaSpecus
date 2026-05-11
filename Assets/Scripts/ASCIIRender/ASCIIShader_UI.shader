Shader "Custom/ASCIIShader_UI"
{
    Properties
    {
        _MainTex("Raw Image Texture", 2D) = "white" {}
        _CharTex("Character Map", 2D) = "white" {}
        _tilesX("X Characters", Float) = 64
        _tilesY("Y Characters", Float) = 36
        _charCount("Number of Characters", Float) = 8
        _brightness("Brightness", Float) = 1
        _monochromatic("Monochromatic", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass
        {
            Name "ASCII_UI"

            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_CharTex);
            SAMPLER(sampler_CharTex);

            float _tilesX;
            float _tilesY;
            float _charCount;
            float _brightness;
            float _monochromatic;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                float2 cellUV = float2(
                    floor(uv.x * _tilesX) / _tilesX,
                    floor(uv.y * _tilesY) / _tilesY
                );

                float4 src = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, cellUV);

                float gray = dot(src.rgb, float3(0.299, 0.587, 0.114));
                gray = saturate(gray * _brightness);

                float charIndex = floor(gray * (_charCount - 1.0) + 0.5);

                float2 localUV = frac(float2(uv.x * _tilesX, uv.y * _tilesY));

                float2 charUV = float2(
                    (charIndex + localUV.x) / _charCount,
                    localUV.y
                );

                float glyph = SAMPLE_TEXTURE2D(_CharTex, sampler_CharTex, charUV).r;

                if (_monochromatic > 0.5)
                {
                    return half4(0.0, glyph * gray, 0.0, src.a) * IN.color;
                }

                return half4(src.rgb * glyph, src.a) * IN.color;
            }

            ENDHLSL
        }
    }
}
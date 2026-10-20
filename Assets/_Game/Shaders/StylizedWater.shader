// Cartoon water for the wave pool (URP).
// Vertex color R = foam amount (written by WaveWater every frame), G = extra brightness on wave faces.
// Steep faces turn a lighter teal, foam is broken up by moving noise, lighting is soft-stepped like a toon shader.
Shader "SurfPool/StylizedWater"
{
    Properties
    {
        _DeepColor ("Deep water", Color) = (0.04, 0.36, 0.72, 1)
        _FaceColor ("Wave face", Color) = (0.12, 0.76, 0.82, 1)
        _FoamColor ("Foam", Color) = (1, 1, 1, 1)
        _FoamNoiseScale ("Foam noise scale", Float) = 1.7
        _FoamThreshold ("Foam threshold", Range(0, 1)) = 0.3
        _FresnelPower ("Fresnel power", Float) = 3
        _SparkleSize ("Sparkle size", Range(10, 400)) = 140
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _FaceColor;
                half4 _FoamColor;
                float _FoamNoiseScale;
                half _FoamThreshold;
                half _FresnelPower;
                half _SparkleSize;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half4 color : COLOR;
                half fogFactor : TEXCOORD2;
            };

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                normal = IS_FRONT_VFACE(face, normal, -normal);
                float3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);

                // Steeper wave faces are lighter and greener, like light shining through the wave.
                half steep = saturate((1.0 - normal.y) * 2.2 + input.color.g);
                half3 water = lerp(_DeepColor.rgb, _FaceColor.rgb, steep);

                // Foam from the vertex color, broken into cartoon blobs by two layers of moving noise.
                float2 uv = input.positionWS.xz * _FoamNoiseScale;
                float noise = ValueNoise(uv + _Time.y * 0.35) * 0.6 + ValueNoise(uv * 2.3 - _Time.y * 0.5) * 0.4;
                half foamValue = input.color.r * (0.55 + noise * 0.9);
                half foam = smoothstep(_FoamThreshold, _FoamThreshold + 0.08, foamValue);
                half3 albedo = lerp(water, _FoamColor.rgb, foam);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half ndl = saturate(dot(normal, mainLight.direction));
                half toon = lerp(0.6, 1.0, smoothstep(0.05, 0.5, ndl));
                half shadow = lerp(0.65, 1.0, mainLight.shadowAttenuation);
                half3 ambient = SampleSH(normal) * 0.45;
                half3 color = albedo * (mainLight.color * toon * shadow + ambient);

                // Hard-edged sun sparkle and a light rim at grazing angles, both only on open water.
                half3 halfDir = normalize(mainLight.direction + viewDir);
                half sparkle = step(0.5, pow(saturate(dot(normal, halfDir)), _SparkleSize));
                color += mainLight.color * sparkle * 0.5 * (1.0 - foam);
                half fresnel = pow(1.0 - saturate(dot(normal, viewDir)), _FresnelPower);
                color = lerp(color, _FaceColor.rgb * 1.15, fresnel * 0.3 * (1.0 - foam));

                color = MixFog(color, input.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half DepthFrag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}

// Sand and land (URP): base color x vertex color (optional), broken up by soft world-space noise so big sand areas
// are never one flat color, with optional faceted (flat per triangle) lighting for the low-poly dunes and far islands.
// Vertex color on the island ring (Editor/Shore.cs): RGB = dry sand, wet sand at the waterline, sea floor, jungle and
// rock on the far islands; A = how grassy a dune is (crisp-edged grass patches drawn from world noise, denser where A
// is higher).
Shader "SurfPool/Ground"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _VertexColors ("Use vertex colors", Range(0, 1)) = 0
        _Faceted ("Faceted (flat per triangle)", Range(0, 1)) = 0
        _NoiseScale ("Color noise scale (per meter)", Float) = 0.045
        _NoiseStrength ("Color noise strength", Range(0, 0.3)) = 0.07
        _GrainStrength ("Fine grain strength", Range(0, 0.2)) = 0.035
        _GrassColor ("Dune grass", Color) = (0.6, 0.68, 0.36, 1)
        _GrassColor2 ("Dune grass (dark)", Color) = (0.44, 0.56, 0.3, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _VertexColors;
            half _Faceted;
            float _NoiseScale;
            half _NoiseStrength;
            half _GrainStrength;
            half4 _GrassColor, _GrassColor2;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), u.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), u.x), u.y);
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

            half4 Frag(Varyings input) : SV_Target
            {
                float3 smoothNormal = normalize(input.normalWS);
                float3 flatNormal = normalize(cross(ddy(input.positionWS), ddx(input.positionWS)));
                flatNormal *= sign(dot(flatNormal, smoothNormal) + 1e-4);
                float3 normal = normalize(lerp(smoothNormal, flatNormal, _Faceted));

                half3 albedo = _BaseColor.rgb * lerp(half3(1, 1, 1), input.color.rgb, _VertexColors);
                float2 p = input.positionWS.xz;
                // Grass patches with crisp, ragged edges; vertex A sets how much of the ground they cover.
                half grassy = input.color.a * _VertexColors;
                if (grassy > 0.01)
                {
                    float gn = ValueNoise(p * 0.11) * 0.55 + ValueNoise(p * 0.37 + 5.0) * 0.3 + ValueNoise(p * 1.3 + 9.0) * 0.15;
                    float threshold = 1.05 - grassy * 0.55;
                    half grass = smoothstep(threshold - 0.012, threshold + 0.012, gn);
                    half3 green = lerp(_GrassColor2.rgb, _GrassColor.rgb, ValueNoise(p * 0.21 + 3.0));
                    albedo = lerp(albedo, green, grass);
                }
                // Big soft patches (sun-bleached vs. darker sand) plus a fine grain that fades out with distance so
                // it never shimmers far away.
                float patches = ValueNoise(p * _NoiseScale) * 0.65 + ValueNoise(p * _NoiseScale * 3.1 + 11.0) * 0.35;
                albedo *= 1.0 + (patches - 0.5) * 2.0 * _NoiseStrength;
                float dist = distance(input.positionWS, _WorldSpaceCameraPos);
                float grain = ValueNoise(p * 2.7) - 0.5;
                albedo *= 1.0 + grain * 2.0 * _GrainStrength * saturate(1.0 - dist / 40.0);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half ndl = saturate(dot(normal, mainLight.direction));
                half3 color = albedo * (mainLight.color * ndl * mainLight.shadowAttenuation + SampleSH(normal));
                color = MixFog(color, input.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirection = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirection = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirection));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

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

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex NormalsVert
            #pragma fragment NormalsFrag

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };

            Varyings NormalsVert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 NormalsFrag(Varyings input) : SV_Target { return half4(NormalizeNormalPerPixel(input.normalWS), 0); }
            ENDHLSL
        }
    }
}

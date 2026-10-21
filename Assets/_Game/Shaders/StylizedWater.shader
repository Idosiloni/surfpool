// Cartoon water for the wave pool (URP).
// Vertex color (written by WaveWater every frame): R = foam amount, G = height up the wave (0 trough .. 1 crest),
// B = shallowness (pale aqua over the shallow beach end), A = foam freshness (old foam breaks into lace).
// Steep faces turn a lighter teal and glow when the sun shines through them from behind (like real pool waves),
// foam is broken up by moving noise, lighting is soft-stepped like a toon shader.
Shader "SurfPool/StylizedWater"
{
    Properties
    {
        _DeepColor ("Deep water", Color) = (0.04, 0.36, 0.72, 1)
        _FaceColor ("Wave face (rim)", Color) = (0.12, 0.76, 0.82, 1)
        _FaceLow ("Face: bottom", Color) = (0.039, 0.29, 0.369, 1)
        _FaceMid ("Face: lower middle", Color) = (0.055, 0.498, 0.573, 1)
        _FaceHigh ("Face: upper middle", Color) = (0.098, 0.69, 0.69, 1)
        _FaceTop ("Face: top", Color) = (0.31, 0.867, 0.722, 1)
        _LipBright ("Lip (thin, lit from behind)", Color) = (0.659, 1.0, 0.851, 1)
        _ShallowColor ("Shallow water", Color) = (0.38, 0.86, 0.84, 1)
        _GlowColor ("Sunlit face glow", Color) = (0.45, 1.0, 0.82, 1)
        _GlowStrength ("Sunlit face glow strength", Range(0, 2)) = 0.9
        _TubeColor ("Inside of the tube (lip underside)", Color) = (0.03, 0.42, 0.52, 1)
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
                half4 _FaceLow, _FaceMid, _FaceHigh, _FaceTop, _LipBright;
                half4 _ShallowColor;
                half4 _GlowColor;
                half _GlowStrength;
                half4 _TubeColor;
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
                half inside = IS_FRONT_VFACE(face, 0.0, 1.0); // the underside: inside of a barrel's lip
                normal = IS_FRONT_VFACE(face, normal, -normal);
                float3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);

                // A wave face is dark low down (you look into deep water: thick water swallows the red) and bright aqua
                // at the top, where the lip is thin and the light comes through it. Vertex G = height up the wave.
                half steep = saturate((1.0 - normal.y) * 2.2);
                half hf = saturate(input.color.g);
                half3 deep = lerp(_DeepColor.rgb, _ShallowColor.rgb, input.color.b);
                half3 faceColor = lerp(_FaceLow.rgb, _FaceMid.rgb, smoothstep(0.0, 0.5, hf));
                faceColor = lerp(faceColor, _FaceHigh.rgb, smoothstep(0.5, 0.78, hf));
                faceColor = lerp(faceColor, _FaceTop.rgb, smoothstep(0.78, 0.94, hf));
                half onWave = saturate(steep * 1.8) * saturate(hf * 5.0);
                half3 water = lerp(deep, faceColor, onWave);
                half lip = smoothstep(0.9, 1.0, hf);
                water = lerp(water, _LipBright.rgb, lip * 0.85);
                water = lerp(water, _TubeColor.rgb, inside * 0.7);

                // Foam from the vertex color, broken into cartoon blobs by two layers of moving noise. Old foam
                // (low freshness) needs more to show and gets holes punched in it, so trails turn into lace.
                float2 uv = input.positionWS.xz * _FoamNoiseScale;
                float noise = ValueNoise(uv + _Time.y * 0.35) * 0.6 + ValueNoise(uv * 2.3 - _Time.y * 0.5) * 0.4;
                half age = 1.0 - input.color.a;
                half foamValue = input.color.r * (0.55 + noise * 0.9);
                half threshold = _FoamThreshold + age * 0.4;
                half foam = smoothstep(threshold, threshold + 0.08, foamValue);
                // Lace: old foam keeps only thin veins where two noise layers cross a narrow band.
                float lace = ValueNoise(input.positionWS.xz * 3.1 + 17.0) * 0.6 + ValueNoise(input.positionWS.xz * 7.3) * 0.4;
                half vein = 1.0 - smoothstep(0.06, 0.14 + (1.0 - age) * 0.4, abs(lace - 0.5));
                foam *= lerp(1.0, vein, saturate(age * 1.3));
                half3 albedo = lerp(water, _FoamColor.rgb, foam);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half ndl = saturate(dot(normal, mainLight.direction));
                half toon = lerp(0.6, 1.0, smoothstep(0.05, 0.5, ndl));
                half shadow = lerp(0.65, 1.0, mainLight.shadowAttenuation);
                half3 ambient = SampleSH(normal) * 0.45;
                half3 color = albedo * (mainLight.color * toon * shadow + ambient);

                // Sun shining through a steep, thin face from behind it: a bright lagoon glow (strongest near the
                // top, where the water is thinnest).
                half behind = pow(saturate(dot(viewDir, -mainLight.direction) * 0.5 + 0.5), 3.0);
                half thin = smoothstep(0.6, 1.0, hf) * saturate(0.4 + steep);
                color += _GlowColor.rgb * mainLight.color * behind * thin * _GlowStrength * (1.0 - foam) * shadow;

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

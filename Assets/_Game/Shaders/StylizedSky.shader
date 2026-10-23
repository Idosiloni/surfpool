// Cartoon skybox (URP): a deep blue zenith fading to a pale horizon that matches the fog (so far islands melt into
// it), a warm glow along the horizon around a low sun, a soft sun disc, and slow drifting cartoon clouds with lit
// tops and shaded bellies. DayCycle darkens it at dusk through _SkyTint and _Exposure (same names as Unity's
// procedural sky), and the sun's own height warms it in the morning, the evening and on Golden Hour days.
Shader "SurfPool/StylizedSky"
{
    Properties
    {
        _SkyTint ("Zenith", Color) = (0.24, 0.5, 0.93, 1)
        _HorizonColor ("Horizon", Color) = (0.74, 0.87, 0.96, 1)
        _GroundColor ("Below the horizon", Color) = (0.42, 0.66, 0.8, 1)
        _SunsetColor ("Low sun glow", Color) = (1.0, 0.63, 0.38, 1)
        _HorizonFog ("Horizon matches fog", Range(0, 1)) = 0.6
        _Exposure ("Exposure", Range(0, 4)) = 1
        _SunSize ("Sun size", Range(0, 1)) = 0.35
        _CloudColor ("Cloud top", Color) = (1, 0.99, 0.96, 1)
        _CloudShade ("Cloud belly", Color) = (0.7, 0.78, 0.9, 1)
        _CloudCover ("Cloud cover (lower = more)", Range(0, 1)) = 0.56
        _CloudScale ("Cloud size (lower = bigger)", Float) = 0.9
        _CloudSpeed ("Cloud drift", Float) = 0.006
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _SkyTint, _HorizonColor, _GroundColor, _SunsetColor, _CloudColor, _CloudShade;
                half _HorizonFog, _Exposure, _SunSize, _CloudCover;
                float _CloudScale, _CloudSpeed;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; };

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

            float Clouds(float2 p)
            {
                float n = ValueNoise(p) * 0.5;
                n += ValueNoise(p * 2.03 + 3.1) * 0.27;
                n += ValueNoise(p * 4.1 + 7.7) * 0.15;
                n += ValueNoise(p * 8.3 + 1.3) * 0.08;
                return n;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 d = normalize(input.direction);
                float3 sun = normalize(_MainLightPosition.xyz);
                float y = d.y;

                // Zenith to horizon, and a slightly deeper color under the horizon line (only ever seen far out to sea).
                half3 sky = lerp(_HorizonColor.rgb, _SkyTint.rgb, pow(saturate(y), 0.5));
                sky = y < 0 ? lerp(_HorizonColor.rgb, _GroundColor.rgb, saturate(-y * 5.0)) : sky;
                half nearHorizon = 1.0 - saturate(abs(y) * 7.0);
                sky = lerp(sky, unity_FogColor.rgb, nearHorizon * _HorizonFog);

                // Low sun: a warm band along the horizon, strongest toward the sun.
                half low = 1.0 - saturate((sun.y - 0.05) / 0.45); // morning and late afternoon, not only at sunset
                half towardSun = pow(saturate(dot(normalize(float3(d.x, 0, d.z) + 1e-4), normalize(float3(sun.x, 0, sun.z) + 1e-4)) * 0.5 + 0.5), 3.0);
                half band = 1.0 - saturate(abs(y) * 3.5);
                sky = lerp(sky, _SunsetColor.rgb, low * band * (0.25 + 0.75 * towardSun) * 0.8);

                // Clouds on a flat layer above: projected so they shrink and flatten toward the horizon.
                half3 color = sky;
                if (y > 0.0)
                {
                    float2 p = d.xz / (y + 0.15) * _CloudScale + _Time.y * _CloudSpeed * float2(1.0, 0.35);
                    float n = Clouds(p);
                    float shade = Clouds(p - sun.xz * 0.06);
                    half cover = smoothstep(_CloudCover, _CloudCover + 0.025, n) * smoothstep(0.02, 0.18, y);
                    half lit = smoothstep(0.35, 0.65, 0.55 + (n - shade) * 7.0);
                    half3 cloud = lerp(_CloudShade.rgb, _CloudColor.rgb, lit);
                    cloud = lerp(cloud, cloud * _SunsetColor.rgb * 1.2, low * 0.55);
                    color = lerp(sky, cloud, cover * 0.95);
                }

                // Sun: a soft-edged disc and a wide glow.
                half cosA = dot(d, sun);
                half radius = 0.0004 + _SunSize * 0.0025;
                half disc = 1.0 - smoothstep(radius * 0.7, radius, 1.0 - cosA);
                half glow = pow(saturate(cosA), 220.0) * 0.6 + pow(saturate(cosA), 18.0) * 0.18;
                half3 sunColor = _MainLightColor.rgb / max(1.0, max(_MainLightColor.r, max(_MainLightColor.g, _MainLightColor.b)));
                color += sunColor * (disc * 2.5 + glow) * step(-0.05, y);

                return half4(color * _Exposure, 1);
            }
            ENDHLSL
        }
    }
}

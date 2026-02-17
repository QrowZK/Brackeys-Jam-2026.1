Shader "Custom/URP/ToonDitherOutline"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)

        _ShadeSteps ("Shade Steps", Range(1,6)) = 3
        _ShadowThreshold ("Shadow Threshold", Range(0,1)) = 0.5
        _ShadowSoftness ("Shadow Softness", Range(0,0.5)) = 0.05

        _Fade ("Dither Fade", Range(0,1)) = 1
        _DitherScale ("Dither Scale", Range(0.5,3)) = 1

        [Toggle(_OUTLINE_ON)] _OutlineOn ("Outline On", Float) = 0
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0,0.05)) = 0.01
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" "RenderType"="Opaque" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // URP lighting keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            float4 _BaseColor;
            float _ShadeSteps;
            float _ShadowThreshold;
            float _ShadowSoftness;

            float _Fade;
            float _DitherScale;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
                float4 screenPos   : TEXCOORD3;
            };

            // 4x4 Bayer threshold in [0..1)
            float Bayer4x4(float2 pixel)
            {
                int2 p = int2(pixel) & 3;

                int x = p.x;
                int y = p.y;

                int b =
                    (y == 0) ? ((x == 0) ? 0  : (x == 1) ? 8  : (x == 2) ? 2  : 10) :
                    (y == 1) ? ((x == 0) ? 12 : (x == 1) ? 4  : (x == 2) ? 14 : 6 ) :
                    (y == 2) ? ((x == 0) ? 3  : (x == 1) ? 11 : (x == 2) ? 1  : 9 ) :
                               ((x == 0) ? 15 : (x == 1) ? 7  : (x == 2) ? 13 : 5 );

                return (b + 0.5) / 16.0;
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS = normalize(nrm.normalWS);
                OUT.uv = IN.uv;

                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);

                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // Dither fade, opaque clip
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float2 pixel = screenUV * _ScreenParams.xy * _DitherScale;
                float threshold = Bayer4x4(pixel);
                clip(_Fade - threshold);

                // Base color
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                // Lighting
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                float3 N = normalize(IN.normalWS);
                float3 L = normalize(mainLight.direction);

                float ndotl = saturate(dot(N, -L));

                // Shadow attenuation from URP
                float shadowAtten = mainLight.shadowAttenuation;

                // Toon banding
                float lit = ndotl * shadowAtten;

                // Optional soft threshold then steps
                float t0 = _ShadowThreshold - _ShadowSoftness;
                float t1 = _ShadowThreshold + _ShadowSoftness;
                float band = smoothstep(t0, t1, lit);

                float steps = max(1.0, _ShadeSteps);
                float stepped = floor(band * steps) / (steps - 0.0001);

                // Mix between a shadow tint and lit
                float3 shadowTint = albedo.rgb * 0.55;
                float3 litColor = albedo.rgb;

                float3 finalRgb = lerp(shadowTint, litColor, stepped);

                return half4(finalRgb, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _OUTLINE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _OutlineColor;
            float _OutlineWidth;

            float _Fade;
            float _DitherScale;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 screenPos   : TEXCOORD0;
            };

            float Bayer4x4(float2 pixel)
            {
                int2 p = int2(pixel) & 3;
                int x = p.x;
                int y = p.y;

                int b =
                    (y == 0) ? ((x == 0) ? 0  : (x == 1) ? 8  : (x == 2) ? 2  : 10) :
                    (y == 1) ? ((x == 0) ? 12 : (x == 1) ? 4  : (x == 2) ? 14 : 6 ) :
                    (y == 2) ? ((x == 0) ? 3  : (x == 1) ? 11 : (x == 2) ? 1  : 9 ) :
                               ((x == 0) ? 15 : (x == 1) ? 7  : (x == 2) ? 13 : 5 );

                return (b + 0.5) / 16.0;
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                #if defined(_OUTLINE_ON)
                    float3 n = normalize(IN.normalOS);
                    float3 pos = IN.positionOS.xyz + n * _OutlineWidth;
                #else
                    float3 pos = IN.positionOS.xyz;
                #endif

                float4 hcs = TransformObjectToHClip(pos);
                OUT.positionHCS = hcs;
                OUT.screenPos = ComputeScreenPos(hcs);

                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                #if !defined(_OUTLINE_ON)
                    discard;
                #endif

                // Keep outline consistent with fade
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float2 pixel = screenUV * _ScreenParams.xy * _DitherScale;
                float threshold = Bayer4x4(pixel);
                clip(_Fade - threshold);

                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}

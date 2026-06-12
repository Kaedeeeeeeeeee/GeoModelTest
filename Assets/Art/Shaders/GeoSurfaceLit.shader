// GeoSurfaceLit —— 坡度感知的 URP Lit 着色器（露头/植被系统 Phase 2）
// 平地（倾角小）→ 草地外观；陡坡/崖面（倾角大）→ 保留地层岩面。
// 混合阈值 _CoveredAngle / _OutcropAngle 与 OutcropSurface/OutcropConfig 共用同一套语义。
//
// 只保留主渲染 ForwardLit pass；阴影/深度交给 FallBack "Universal Render Pipeline/Lit"
// 自动提供（避免自写 ShadowCaster 误用 Shadows.hlsl 导致 LerpWhiteTo 编译错误）。
Shader "GeoModel/GeoSurfaceLit"
{
    Properties
    {
        [Header(Strata  steep outcrop)]
        _StrataColor("Strata Color", Color) = (0.5, 0.45, 0.4, 1)
        _StrataTex("Strata Albedo", 2D) = "white" {}

        [Header(Grass  flat covered)]
        _GrassColor("Grass Color", Color) = (0.36, 0.49, 0.24, 1)
        _GrassTex("Grass Albedo", 2D) = "white" {}
        _GrassTiling("Grass Tiling", Float) = 4

        [Header(Slope blend  degrees)]
        _CoveredAngle("Covered Angle (full grass below)", Range(0, 89)) = 22
        _OutcropAngle("Outcrop Angle (full rock above)", Range(1, 90)) = 32

        [Header(Surface)]
        _Smoothness("Smoothness", Range(0, 1)) = 0.1
        _Metallic("Metallic", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _StrataTex_ST;
                float4 _StrataColor;
                float4 _GrassColor;
                float  _GrassTiling;
                float  _CoveredAngle;
                float  _OutcropAngle;
                float  _Smoothness;
                float  _Metallic;
            CBUFFER_END

            TEXTURE2D(_StrataTex); SAMPLER(sampler_StrataTex);
            TEXTURE2D(_GrassTex);  SAMPLER(sampler_GrassTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrm = GetVertexNormalInputs(IN.normalOS);
                OUT.positionHCS = pos.positionCS;
                OUT.positionWS  = pos.positionWS;
                OUT.normalWS    = nrm.normalWS;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _StrataTex);
                OUT.fogFactor   = ComputeFogFactor(pos.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);

                // 倾角（度）：0 = 水平，90 = 竖直崖面
                float upDot    = saturate(dot(N, float3(0, 1, 0)));
                float slopeDeg = degrees(acos(upDot));
                // grassAmount：1 = 满草（平地），0 = 满岩（陡坡）
                float grassAmount = 1.0 - smoothstep(_CoveredAngle, _OutcropAngle, slopeDeg);

                half4 strata = SAMPLE_TEXTURE2D(_StrataTex, sampler_StrataTex, IN.uv) * _StrataColor;
                float2 guv   = IN.positionWS.xz * _GrassTiling;          // 草色顶投影，避免拉伸
                half4 grass  = SAMPLE_TEXTURE2D(_GrassTex, sampler_GrassTex, guv) * _GrassColor;

                half3 albedo = lerp(strata.rgb, grass.rgb, grassAmount);

                SurfaceData sd = (SurfaceData)0;
                sd.albedo     = albedo;
                sd.metallic   = _Metallic;
                sd.specular   = 0;
                sd.smoothness = _Smoothness;
                sd.occlusion  = 1.0;
                sd.emission   = 0;
                sd.alpha      = 1.0;

                InputData id = (InputData)0;
                id.positionWS      = IN.positionWS;
                id.normalWS        = N;
                id.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    id.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                #else
                    id.shadowCoord = float4(0, 0, 0, 0);
                #endif
                id.fogCoord                  = IN.fogFactor;
                id.bakedGI                   = SampleSH(N);
                id.normalizedScreenSpaceUV   = GetNormalizedScreenSpaceUV(IN.positionHCS);
                id.shadowMask                = half4(1, 1, 1, 1);

                half4 color = UniversalFragmentPBR(id, sd);
                color.rgb = MixFog(color.rgb, IN.fogFactor);
                return color;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}

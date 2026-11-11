Shader "Paralysis/InteractionOutline"
{
    Properties
    {
        _Color ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Range(0.0001, 0.05)) = 0.004
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+10"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings OutlineVertex(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                float3 normalVS = TransformWorldToViewDir(normalWS, true);

                // Project the view-space normal with the projection matrix.
                // TransformViewToProjection is a built-in-pipeline helper and
                // is not defined by URP, which broke the WebGL/GLES3 build.
                float2 offset = mul((float2x2)UNITY_MATRIX_P, normalVS.xy);

                float4 positionCS = TransformWorldToHClip(positionWS);

                // Expand along the normal in view space, then convert that
                // direction to a screen-space offset. This keeps the outline
                // roughly the same thickness at any distance.
                positionCS.xy += offset * positionCS.w * _OutlineWidth;

                output.positionCS = positionCS;
                return output;
            }

            half4 OutlineFragment(Varyings input) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}

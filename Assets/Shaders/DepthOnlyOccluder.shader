Shader "Godzilla/DepthOnlyOccluder"
{
    // Writes depth only. Rendered just before opaque geometry so real walls (MRUK EffectMesh)
    // hide the giant everywhere except through the window holes; passthrough shows through
    // because nothing is written to color.
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry-1" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "UniversalForward" }
            ColorMask 0
            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}

Shader "GPUTerrain/Terrain"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderer" }
        LOD 100

        Pass
        {
            HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #pragma vertex vert
            #pragma fragment frag

            StructuredBuffer<uint> terrain;
            uint m_sideLength;
            float2 cameraSizeWS;
            float2 cameraCoordsWS;
            float tileSize;

            Varyings vert(uint vertex_id : SV_VertexID, uint nodeInstance_id : SV_InstanceID) // : SV_POSITION
            {
                Varyings output = AnimateVector(vertex_id, nodeInstance_id);
                return output;
            }

            TEXTURE2D_ARRAY(_Textures);
            SAMPLER(sampler_Textures);

            half4 frag(Varyings input) : SV_TARGET
            {
                //half4 color = tex2D(_MainTex, input.uv);
                half4 color = SAMPLE_TEXTURE2D_ARRAY(_Textures, sampler_Textures, input.uv, input.textureI);
                clip(color.a - 0.01);  // Clip pixels with very low alpha to avoid sorting issues
                return color;
            }
            ENDHLSL
        }
    }
}

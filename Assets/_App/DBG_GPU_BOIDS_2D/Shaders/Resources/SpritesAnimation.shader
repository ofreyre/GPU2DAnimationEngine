Shader "GPUSprites/SpritesAnimation"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Textures("Textures", 2DArray) = "" {}
    }

    HLSLINCLUDE

    struct Sprite
    {
        uint textureI;
        float2 uv;
        float2 sizeuv;
    };

    struct NodeInstance
    {
        uint entityInstanceI;
        uint order;
    };

    struct NodeInstanceFrame
    {
        uint spriteI;
        float3x3 transformOS;
        float flip;
    };

    struct EntityInstance
    {
        uint entityI;
        uint currentClipI;
        float4 transformWS;
        float time;
    };

    struct EntityInstanceProps
    {
        float3 direction;
    };

    StructuredBuffer<Sprite> sprites;
    StructuredBuffer<NodeInstance> nodeInstances;
    StructuredBuffer<NodeInstanceFrame> nodeInstancesFrame;
    StructuredBuffer<EntityInstance> entityInstances;

    //uint nodeInstancesC;
    ENDHLSL

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite On          // Enable ZWrite to write to the depth buffer
        ZTest LEqual       // Enable ZTest to perform depth testing

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #pragma vertex vert
            #pragma fragment frag
            //#pragma target 5.0

            /* 1--2
            *  |  |
            *  0--3   */
            static const float3 vertices[4] = {
                float3(0, 0, 1),
                float3(0, 1, 1),
                float3(1, 1, 1),
                float3(1, 0, 1)
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                //float3 positionWS : TEXCOORD1;
                uint textureI : TEXCOORD1;
                float4 positionCS : SV_POSITION;
            };

            sampler2D _MainTex;

            Varyings AnimateVector(uint vertexI, uint nodeInstanceI)
            {
                float3 v = vertices[vertexI % 4];
                NodeInstance nodeInstance = nodeInstances[nodeInstanceI];
                EntityInstance entityInstance = entityInstances[nodeInstance.entityInstanceI];

                NodeInstanceFrame nodeInstanceFrame = nodeInstancesFrame[nodeInstanceI];
                Sprite sprite = sprites[nodeInstanceFrame.spriteI];

                Varyings o;
                o.uv = sprite.uv + v.xy * sprite.sizeuv;

                float3 pWS = entityInstance.transformWS.xyz + mul(nodeInstanceFrame.transformOS, v) * entityInstance.transformWS.w * float3(nodeInstanceFrame.flip,1,1);
                pWS.z = entityInstance.transformWS.y * 10 - nodeInstance.order * 0.01;
                o.positionCS = TransformWorldToHClip(pWS);

                o.textureI = sprite.textureI;
                return o;
            }
            
            //instance_id is the nodeInstanceI
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

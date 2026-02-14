Shader "GPUSprites/SpritesAnimation 1"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    HLSLINCLUDE

    struct Entity
    {
        uint nodesC;
        uint clipsStartI;
    };

    struct Clip
    {
        uint frameStartI;
        uint framesC;
        float frameDuration;
    };

    struct Sprite
    {
        float2 uv;
        float2 sizeuv;
    };

    struct Frame
    {
        float3x3 transformOS;
        uint spriteI;
    };

    struct EntityInstance
    {
        uint entityI;
        uint currentClipI;
        float4 transformWS;
        float time;
    };

    struct NodeInstance
    {
        uint entityInstanceI;
        uint order;
    };

    struct EntityInstanceProps
    {
        float3 direction;
    };

    StructuredBuffer<Entity> entities;
    StructuredBuffer<Clip> clips;
    StructuredBuffer<Sprite> sprites;
    StructuredBuffer<Frame> frames;
    StructuredBuffer<EntityInstance> entityInstances;
    StructuredBuffer<NodeInstance> nodeInstances;
    StructuredBuffer<EntityInstanceProps> entityInstanceProps;

    uint nodeInstancesC;
    float time;
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
                float4 positionCS : SV_POSITION;
            };

            sampler2D _MainTex;

            Varyings AnimateVector(uint vertexI, uint nodeInstanceI)
            {
                float3 v = vertices[vertexI % 4];
                NodeInstance nodeInstance = nodeInstances[nodeInstanceI];
                EntityInstance entityInstance = entityInstances[nodeInstance.entityInstanceI];
                Entity entity = entities[entityInstance.entityI];
                Clip clip = clips[entity.clipsStartI + entityInstance.currentClipI];

                uint frameI = uint(time / clip.frameDuration) % clip.framesC;
                uint frameNodeI = clip.frameStartI + frameI * entity.nodesC + nodeInstance.order;
                Frame frame = frames[frameNodeI];
                Sprite sprite = sprites[frame.spriteI];

                EntityInstanceProps eiProps = entityInstanceProps[nodeInstance.entityInstanceI];

                Varyings o;
                o.uv = sprite.uv + v.xy * sprite.sizeuv;

                float3 xDir; //= eiProps.direction.x >= 0 ? float3(1, 1, 1) : float3(-1, 1, 1);
                if (eiProps.direction.x >= 0)
                {
                    xDir = float3(1, 1, 1);
                }
                else
                {
                    xDir = float3(-1, 1, 1);
                }

                float3 pWS = entityInstance.transformWS.xyz + mul(frame.transformOS, v) * entityInstance.transformWS.w * xDir;
                pWS.z = entityInstance.transformWS.y * 10 - nodeInstance.order * 0.01;
                o.positionCS = TransformWorldToHClip(pWS);
                return o;
            }
            
            //instance_id is the nodeInstanceI
            Varyings vert(uint vertex_id : SV_VertexID, uint nodeInstance_id : SV_InstanceID) // : SV_POSITION
            {
                Varyings output = AnimateVector(vertex_id, nodeInstance_id);
                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                half4 color = tex2D(_MainTex, input.uv);
                clip(color.a - 0.01);  // Clip pixels with very low alpha to avoid sorting issues
                return color;
            }
            ENDHLSL
        }
    }
}

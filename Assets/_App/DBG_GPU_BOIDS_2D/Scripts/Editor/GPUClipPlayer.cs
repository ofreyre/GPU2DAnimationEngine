
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class GPUClipPlayer
{   struct AnimatedSprite
    {
        public Vector2 uv;
        public Vector2 sizeuv;
        public Vector2 scaleWS;
        public Matrix3x3 transformWS;
    };

    List<Vector4> m_transforms = new List<Vector4>() { new Vector4(0,0,0,0)};
    bool m_playing;
    ComputeShader m_spritesToTextureCompute;
    RenderTexture m_tarText;
    Texture2D m_tarText2D;
    Texture2DArray m_srcTextures;
    GPUAnimationData m_animationData;
    CancellationTokenSource m_cancellationTokenSource;
    uint m_x, m_y, m_z;
    int m_kernel;

    Vector3[] m_spriteVertices = new Vector3[] {
        new Vector3(0, 0, 1), new Vector3(1, 0, 1),
        new Vector3(0, 1, 1), new Vector3(1, 1, 1),
    };

    Vector3[] m_spriteVerticesRes = new Vector3[4];

    ComputeBuffer m_entitiesBuffer;
    ComputeBuffer m_clipsBuffer;
    ComputeBuffer m_spritesBuffer;
    ComputeBuffer m_framesBuffer;
    ComputeBuffer m_clipRectsBuffer;
    ComputeBuffer m_entityInstancesBuffer;
    ComputeBuffer m_nodeInstancesBuffer;

    List<NodeInstance> m_nodeInstances = new List<NodeInstance>();
    List<EntityInstance> m_entityInstances = new List<EntityInstance>();

    int m_shaderTime_id;
    Action m_frameCompleteHandler;

    int m_tarTextureWidth;
    int m_tarTextureHeight;

    public GPUClipPlayer(GPUAnimationData animationData, int tarTextureWidth, int tarTextureHeight, Action frameCompleteHandler)
    {
        m_tarTextureWidth = tarTextureWidth;
        m_tarTextureHeight = tarTextureHeight;

        m_frameCompleteHandler = frameCompleteHandler;

        m_spritesToTextureCompute = Resources.Load<ComputeShader>("SpritesToTextureCompute");
        m_tarText = new RenderTexture(tarTextureWidth, tarTextureHeight, 24, RenderTextureFormat.ARGB32)
        {
            enableRandomWrite = true
        };
        m_tarText.filterMode = FilterMode.Point;

        m_animationData = animationData;

        m_srcTextures = m_animationData.m_textures;
    }
    ~GPUClipPlayer()
    {
        m_tarText.Release();
        Stop();
    }

    public void Play(int entityI, int clipI)
    {
        Stop();
        m_cancellationTokenSource = new CancellationTokenSource();
        //_Play(entityI, clipI);
        //Task.Run(() => _Play1(entityI, clipI));

        if (!m_playing)
        {
            //_PlayCPU(entityI, clipI);
            Play1(entityI, clipI);
        }

        //Task.Run(() => _Play(entityI, clipI));
    }

    void Play1(int entityI, int clipI)
    {
        m_playing = true;

        Debug.Log("m_srcTextures = " + m_srcTextures);

        m_kernel = m_spritesToTextureCompute.FindKernel("CSMain");
        m_spritesToTextureCompute.SetTexture(m_kernel, "srcTextures", m_srcTextures);
        m_spritesToTextureCompute.SetTexture(m_kernel, "Result", m_tarText);
        m_spritesToTextureCompute.SetVector("srcTexSize", new Vector4(m_animationData.m_texturesSize, m_animationData.m_texturesSize, 0, 0));
        m_spritesToTextureCompute.SetVector("tarTexSize", new Vector4(m_tarTextureWidth, m_tarTextureHeight, 0, 0));
        m_spritesToTextureCompute.SetVector("bkgColor", new Vector4(0.3f, 0.3f, 0.3f, 1));

        m_entitiesBuffer = new ComputeBuffer(m_animationData.m_entities.Length, sizeof(int) * 2, ComputeBufferType.Structured);
        m_entitiesBuffer.SetData(m_animationData.GetShaderEntities());
        m_spritesToTextureCompute.SetBuffer(m_kernel, "entities", m_entitiesBuffer);

        m_clipsBuffer = new ComputeBuffer(m_animationData.m_clips.Length, sizeof(int) * 2 + sizeof(float), ComputeBufferType.Structured);
        m_clipsBuffer.SetData(m_animationData.m_clips);
        m_spritesToTextureCompute.SetBuffer(m_kernel, "clips", m_clipsBuffer);

        m_spritesBuffer = new ComputeBuffer(m_animationData.m_sprites.Length, sizeof(int) + sizeof(float) * 4, ComputeBufferType.Structured);
        m_spritesBuffer.SetData(m_animationData.m_sprites);
        m_spritesToTextureCompute.SetBuffer(m_kernel, "sprites", m_spritesBuffer);

        m_framesBuffer = new ComputeBuffer(m_animationData.m_frames.Length, sizeof(int) + sizeof(float) * 9, ComputeBufferType.Structured);
        m_framesBuffer.SetData(m_animationData.GetShaderFrames());
        m_spritesToTextureCompute.SetBuffer(m_kernel, "frames", m_framesBuffer);

        m_clipRectsBuffer = new ComputeBuffer(m_animationData.m_clipRects.Length, sizeof(float) * 4, ComputeBufferType.Structured);
        m_clipRectsBuffer.SetData(m_animationData.m_clipRects);
        m_spritesToTextureCompute.SetBuffer(m_kernel, "clipRects", m_clipRectsBuffer);

        m_spritesToTextureCompute.GetKernelThreadGroupSizes(m_kernel, out m_x, out m_y, out m_z);
        m_x = (uint)m_tarTextureWidth / m_x;
        m_y = (uint)m_tarTextureHeight / m_y;

        m_shaderTime_id = Shader.PropertyToID("time");

        m_entityInstances.Clear();
        m_nodeInstances.Clear();
        m_animationData.GetEntityInstances(m_animationData.m_prefabEntities[entityI].prefab.name, m_transforms, m_entityInstances, m_nodeInstances);
        EntityInstance entityInstance = m_entityInstances[0];
        entityInstance.currentClipI = clipI;
        m_entityInstances[0] = entityInstance;        

        m_entityInstancesBuffer = new ComputeBuffer(m_entityInstances.Count, sizeof(int) * 2 + sizeof(float) * 5, ComputeBufferType.Structured);
        m_entityInstancesBuffer.SetData(m_entityInstances);
        m_spritesToTextureCompute.SetBuffer(m_kernel, "entityInstances", m_entityInstancesBuffer);

        m_nodeInstancesBuffer = new ComputeBuffer(m_nodeInstances.Count, sizeof(int) * 2, ComputeBufferType.Structured);
        m_nodeInstancesBuffer.SetData(m_nodeInstances);
        m_spritesToTextureCompute.SetBuffer(m_kernel, "nodeInstances", m_nodeInstancesBuffer);
        m_spritesToTextureCompute.SetInt("nodeInstancesC", m_nodeInstances.Count);

        _Play1(entityI, clipI);
    }

    public async void _Play1(int entityI, int clipI)
    {

        Clip clip = m_animationData.m_clips[clipI];
        float frameDuration = clip.frameDuration;

        //float time = DateTime.Now.Millisecond / 1000f;
        float time = Time.realtimeSinceStartup;

        string orders = "";
        for (int i = 0; i < m_nodeInstances.Count; i++)
        {
            orders += m_nodeInstances[i].order + ", ";
        }

        float t1 = time;
        while (m_playing)
        {
            float t = Time.realtimeSinceStartup;
            if (t >= time)
            {
                t1 = t;
                time += frameDuration;
                m_frameCompleteHandler.Invoke();
                m_spritesToTextureCompute.SetFloat(m_shaderTime_id, t);
                m_spritesToTextureCompute.Dispatch(m_kernel, (int)m_x, (int)m_y, (int)m_z);
            }
            await Task.Yield();
        }
    }

    public async void _PlayCPU(int entityI, int clipI)
    {
        m_tarText = m_animationData.m_renTex;
        m_playing = true;
        m_tarText2D = new Texture2D(m_tarTextureWidth, m_tarTextureHeight, TextureFormat.ARGB32, false);
        m_entityInstances.Clear();
        m_nodeInstances.Clear();
        m_animationData.GetEntityInstances(m_animationData.m_prefabEntities[entityI].prefab.name, m_transforms, m_entityInstances, m_nodeInstances);
        Entity entity_ = m_animationData.m_entities[entityI];

        int srcTextureWidth = m_srcTextures.width;
        int srcTextureHeight = m_srcTextures.height;
        int nodeInstancesC = m_nodeInstances.Count;
        float time = 0;

        for (int y=0;y< m_tarTextureHeight;y++)
        {
            for(int x = 0; x < m_tarTextureWidth; x++)
            {
                bool painted = false;
                for (int nodeInstanceI = 0; nodeInstanceI < nodeInstancesC; nodeInstanceI++)
                {
                    NodeInstance nodeInstance = m_nodeInstances[nodeInstanceI];
                    EntityInstance entityInstance = m_entityInstances[nodeInstance.entityInstanceI];
                    Entity entity = m_animationData.m_entities[entityInstance.entityI];
                    Clip clip = m_animationData.m_clips[entity.clipsStartI + entityInstance.currentClipI];

                    Vector4 clipRect = m_animationData.m_clipRects[entity.clipsStartI + entityInstance.currentClipI];
                    Vector3 rectOffset = new Vector3(-clipRect.x, -clipRect.y, 0);
                    Vector3 rectScale = new Vector3(
                        m_tarTextureWidth / clipRect.z,
                        m_tarTextureHeight / clipRect.w,
                        1
                        );

                    int frameI = ((int)(time / clip.frameDuration)) % clip.framesC;
                    int frameNodeI = clip.frameStartI + frameI * entity.nodesC + nodeInstance.order;
                    Frame frame = m_animationData.m_frames[frameNodeI];

                    Vector3 v_lb = mul(frame.transformOS * m_spriteVertices[0] + rectOffset, rectScale);
                    Vector3 v_lt = mul(frame.transformOS * m_spriteVertices[2] + rectOffset, rectScale);
                    Vector3 v_rt = mul(frame.transformOS * m_spriteVertices[3] + rectOffset, rectScale);
                    Vector3 v_rb = mul(frame.transformOS * m_spriteVertices[1] + rectOffset, rectScale);

                    /*
                    **  B--C
                    **  |  |
                    **  A--D
                    */

                    Vector2 ab = new Vector2(v_lt.x, v_lt.y) - new Vector2(v_lb.x, v_lb.y);
                    Vector2 bc = new Vector2(v_rt.x, v_rt.y) - new Vector2(v_lt.x, v_lt.y);
                    Vector2 cd = new Vector2(v_rb.x, v_rb.y) - new Vector2(v_rt.x, v_rt.y);
                    Vector2 da = new Vector2(v_lb.x, v_lb.y) - new Vector2(v_rb.x, v_rb.y);

                    Vector2 ap = new Vector2(x, y) - new Vector2(v_lb.x, v_lb.y);
                    Vector2 bp = new Vector2(x, y) - new Vector2(v_lt.x, v_lt.y);
                    Vector2 cp = new Vector2(x, y) - new Vector2(v_rt.x, v_rt.y);
                    Vector2 dp = new Vector2(x, y) - new Vector2(v_rb.x, v_rb.y);

                    float cross1 = cross2x2(ab, ap);
                    float cross2 = cross2x2(bc, bp);
                    float cross3 = cross2x2(cd, cp);
                    float cross4 = cross2x2(da, dp);

                    if (
                        (cross1 >= 0 && cross2 >= 0 && cross3 >= 0 && cross4 >= 0) ||
                        (cross1 <= 0 && cross2 <= 0 && cross3 <= 0 && cross4 <= 0)
                        )
                    {
                        Matrix2x2 m = new Matrix2x2(
                            -da.x, ab.x,
                            -da.y, ab.y
                            );
                        

                        m = inverse2x2(m);
                        Vector2 vvv = new Vector2(x - v_lb.x, y - v_lb.y);
                        Vector2 normXY = m * new Vector2(x - v_lb.x, y - v_lb.y);

                        GPUSprite sprite = m_animationData.m_sprites[frame.spriteI];
                        Vector2 uv = sprite.uv + normXY * sprite.sizeuv;
                        Vector2 texXY = new Vector2(uv.x * srcTextureWidth, uv.y * srcTextureHeight);
                        //Color color = srcTexture[texXY];
                        //Result[texCoord.xy] = color;

                        Color color = m_animationData.m_srcTextures[sprite.textureI].GetPixel((int)texXY.x, (int)texXY.y);
                        //m_tarText2D.SetPixel(x, y, new Color(1, 0, 0, 1));
                        m_tarText2D.SetPixel(x, y, color);
                        painted = true;
                        break;

                        //Result[texCoord.xy] = float4(1, 0, 0, 1);
                    }
                }
                if (!painted)
                {
                    //Result[texCoord.xy] = float4(1, 0, 0, 1);
                    m_tarText2D.SetPixel(x, y, new Color(0, 0, 0, 1));
                }
            }
            await Task.Yield();
        }

        m_tarText2D.Apply();

        m_animationData.m_renTex.enableRandomWrite = true;
        RenderTexture.active = m_animationData.m_renTex;
        Graphics.Blit(m_tarText2D, m_animationData.m_renTex);
        RenderTexture.active = null;
        m_playing = false;
    }

    public void Stop()
    {
        m_playing = false;
        if (m_cancellationTokenSource != null)
        {
            m_cancellationTokenSource.Cancel();
            m_cancellationTokenSource.Dispose();
            m_cancellationTokenSource = null;
        }

        if(m_entitiesBuffer != null) m_entitiesBuffer.Dispose();
        if (m_clipsBuffer != null) m_clipsBuffer.Dispose();
        if (m_spritesBuffer != null) m_spritesBuffer.Dispose();
        if (m_framesBuffer != null) m_framesBuffer.Dispose();
        if (m_clipRectsBuffer != null) m_clipRectsBuffer.Dispose();
        if (m_entityInstancesBuffer != null) m_entityInstancesBuffer.Dispose();
        if (m_nodeInstancesBuffer != null) m_nodeInstancesBuffer.Dispose();
    }

    public RenderTexture tarText { get { return m_tarText; } }
    public Texture2D tarText2D { get { return m_tarText2D; } }

    Vector3 mul(Vector3 v0, Vector3 v1)
    {
        return new Vector3(v0.x * v1.x, v0.y * v1.y, v0.z * v1.z);
    }

    float cross2x2(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    Matrix2x2 inverse2x2(Matrix2x2 m)
    {
        float det = m._00 * m._11 - m._01 * m._10;
        float invDet = 1.0f / det;

        Matrix2x2 inv;
        inv._00 = m._11 * invDet;
        inv._01 = -m._01 * invDet;
        inv._10 = -m._10 * invDet;
        inv._11 = m._00 * invDet;

        return inv;
    }
}

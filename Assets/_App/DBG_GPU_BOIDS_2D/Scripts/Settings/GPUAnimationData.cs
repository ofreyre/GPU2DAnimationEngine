using UnityEngine;
using System;
using System.Collections.Generic;

public class GPUAnimationData : ScriptableObject
{
    [Serializable]
    public class PrefabEntity
    {
        public GameObject prefab;
        public int entityI;
    }

    #region Baked Data
    public ComputeShader m_computerSpriteAnimation;
    public Material m_spritesAnimationMaterial;
    [HideInInspector] public PrefabEntity[] m_prefabEntities;
    [HideInInspector] public GPUSprite[] m_sprites;
    [HideInInspector] public Entity[] m_entities;
    [HideInInspector] public Node[] m_nodes;
    [HideInInspector] public Clip[] m_clips;
    [HideInInspector] public Frame[] m_frames;
    [HideInInspector] public Texture2DArray m_textures;
    [HideInInspector] public Vector4[] m_frameRects;
    [HideInInspector] public Vector4[] m_clipRects;
    [HideInInspector] public Vector4[] m_entityRects;
    [HideInInspector] public RenderTexture m_renTex;
    [HideInInspector] public int m_texturesSize;
    [HideInInspector] public Texture2D[] m_srcTextures;
    #endregion

    #region Runtime Animation
    public ComputeBuffer entitiesBuffer { get; private set; }
    public ComputeBuffer clipsBuffer { get; private set; }
    public ComputeBuffer spritesBuffer { get; private set; }
    public ComputeBuffer framesBuffer { get; private set; }
    //public ComputeBuffer clipRectsBuffer { get; private set; }
    public ComputeBuffer entityInstancesBuffer { get; private set; }
    public ComputeBuffer nodeInstancesBuffer { get; private set; }
    public ComputeBuffer nodeInstancesFrameBuffer { get; private set; }
    public int shaderTime_id { get; private set; }
    public int nodeInstancesCount { get; private set; }
    public int entityInstancesCount { get; private set; }
    public float worldViewHeight { get; private set; }
    public float worldViewWidth { get; private set; }
    public Bounds bounds { get; private set; }

    int m_kernelHandleSpriteAnimation;
    int[] m_computerSpriteAnimationArgs;
    #endregion

    #region Access Methods
    public PrefabEntity GetPrefabEntity(string prefabName)
    { 
        for(int i=0;i< m_prefabEntities.Length;i++)
        {
            if (prefabName.CompareTo(m_prefabEntities[i].prefab.name) == 0)
                return m_prefabEntities[i];
        }
        return null;
    }

    public PrefabEntity GetPrefabEntity(int entityIndex)
    {
        for (int i = 0; i < m_prefabEntities.Length; i++)
        {
            if (m_prefabEntities[i].entityI == entityIndex)
                return m_prefabEntities[i];
        }
        return null;
    }

    public int GetEntityNodesCount(int EntityI)
    {
        return m_entities[EntityI].nodesC;
    }

    public void GetEntityInstances(int entityIndex, List<Vector4> transformWS, List<EntityInstance> entityInstances, List<NodeInstance> nodeInstances)
    {
        if(entityIndex < m_entities.Length)
        {
            PrefabEntity prefabEntity = GetPrefabEntity(entityIndex);
            GetEntityInstances(prefabEntity, transformWS, entityInstances, nodeInstances);
        }
    }

    public void GetEntityInstances(string prefabName, List<Vector4> transformWS, List<EntityInstance> entityInstances, List<NodeInstance> nodeInstances)
    {
        PrefabEntity prefabEntity = GetPrefabEntity(prefabName);
        GetEntityInstances(prefabEntity, transformWS, entityInstances, nodeInstances);
    }

    public void GetEntityInstances(PrefabEntity prefabEntity, List<Vector4> transformWS, List<EntityInstance> entityInstances, List<NodeInstance> nodeInstances)
    {
        if (prefabEntity != null)
        {
            for (int i = 0; i < transformWS.Count; i++)
            {
                entityInstances.Add(new EntityInstance
                {
                    entityI = prefabEntity.entityI,
                    currentClipI = 0,
                    transformWS = transformWS[i],
                    time = 0
                });
                GetEntityNodes(m_entities[prefabEntity.entityI], entityInstances.Count - 1, nodeInstances);
            }
        }
    }

    public void GetEntityNodes(Entity entity, int entityInstanceI, List<NodeInstance> nodeInstances)
    {
        for(int i = 0; i < entity.nodesC; i++)
        {
            nodeInstances.Add(new NodeInstance
            {
                entityInstanceI = entityInstanceI,
                order = m_nodes[entity.nodesStartI + i].order
            });
        }
    }

    public _GPUSprite[] GetShaderSprites()
    {
        _GPUSprite[] sprites = new _GPUSprite[m_sprites.Length];
        for (int i = 0; i < m_entities.Length; i++)
        {
            sprites[i] = new _GPUSprite(m_sprites[i]);
        }
        return sprites;
    }

    public _Entity[] GetShaderEntities()
    {
        _Entity[] entities = new _Entity[m_entities.Length];
        for (int i=0;i< m_entities.Length;i++)
        {
            entities[i] = new _Entity(m_entities[i]);
        }
        return entities;
    }

    public _Clip[] GetShaderClips()
    {
        _Clip[] clips = new _Clip[m_clips.Length];
        for (int i = 0; i < m_clips.Length; i++)
        {
            clips[i] = new _Clip(m_clips[i]);
        }
        return clips;
    }

    public _Frame[] GetShaderFrames()
    {
        _Frame[] frames = new _Frame[m_frames.Length];
        for (int i = 0; i < m_frames.Length; i++)
        {
            frames[i] = new _Frame(m_frames[i]);
        }
        return frames;
    }
    #endregion

    #region Sprites Animation
    public void InitBounds()
    {
        worldViewHeight = Camera.main.orthographicSize * 2;
        worldViewWidth = Camera.main.aspect * worldViewHeight;
        bounds = new Bounds(Vector3.zero, new Vector3(worldViewWidth, worldViewWidth, 8));
    }

    public void InitSpritesAnimationShader(List<EntityInstance> entityInstances, List<NodeInstance> nodeInstances)
    {
        entityInstancesCount = entityInstances.Count;
        shaderTime_id = Shader.PropertyToID("time");
        InitAnimationBuffers();
        InitEntityBuffers(entityInstances, nodeInstances);
        InitSpriteAnimationBuffers();
    }

    void InitAnimationBuffers()
    {
        entitiesBuffer = new ComputeBuffer(m_entities.Length, sizeof(int) * 2, ComputeBufferType.Structured);
        entitiesBuffer.SetData(GetShaderEntities());
        //m_spritesAnimationMaterial.SetBuffer("entities", entitiesBuffer);

        clipsBuffer = new ComputeBuffer(m_clips.Length, sizeof(int) * 2 + sizeof(float), ComputeBufferType.Structured);
        clipsBuffer.SetData(m_clips);
        //m_spritesAnimationMaterial.SetBuffer("clips", clipsBuffer);

        spritesBuffer = new ComputeBuffer(m_sprites.Length, sizeof(int) + sizeof(float) * 4, ComputeBufferType.Structured);
        spritesBuffer.SetData(m_sprites);
        m_spritesAnimationMaterial.SetBuffer("sprites", spritesBuffer);

        framesBuffer = new ComputeBuffer(m_frames.Length, sizeof(int) + sizeof(float) * 9, ComputeBufferType.Structured);
        framesBuffer.SetData(GetShaderFrames());
        //m_spritesAnimationMaterial.SetBuffer("frames", framesBuffer);

        /*
        clipRectsBuffer = new ComputeBuffer(m_clipRects.Length, sizeof(float) * 4, ComputeBufferType.Structured);
        clipRectsBuffer.SetData(m_clipRects);
        m_spritesAnimationMaterial.SetBuffer("clipRects", clipRectsBuffer);
        */

        //m_spritesAnimationMaterial.SetTexture("_MainTex", m_texture);
        m_spritesAnimationMaterial.SetTexture("_Textures", m_textures);
    }

    void InitEntityBuffers(List<EntityInstance> entityInstances, List<NodeInstance> nodeInstances)
    {
        nodeInstancesCount = nodeInstances.Count;
        entityInstancesBuffer = new ComputeBuffer(entityInstances.Count, sizeof(int) * 2 + sizeof(float) * 5, ComputeBufferType.Structured);
        entityInstancesBuffer.SetData(entityInstances);
        m_spritesAnimationMaterial.SetBuffer("entityInstances", entityInstancesBuffer);

        nodeInstancesBuffer = new ComputeBuffer(nodeInstancesCount, sizeof(int) * 2, ComputeBufferType.Structured);
        nodeInstancesBuffer.SetData(nodeInstances);
        m_spritesAnimationMaterial.SetBuffer("nodeInstances", nodeInstancesBuffer);

        nodeInstancesFrameBuffer = new ComputeBuffer(nodeInstancesCount, sizeof(int) + sizeof(float) * 10, ComputeBufferType.Structured);
        m_spritesAnimationMaterial.SetBuffer("nodeInstancesFrame", nodeInstancesFrameBuffer);

        //m_spritesAnimationMaterial.SetInt("nodeInstancesC", nodeInstancesCount);
    }

    void InitSpriteAnimationBuffers()
    {
        m_kernelHandleSpriteAnimation = m_computerSpriteAnimation.FindKernel("CSMain");

        m_computerSpriteAnimation.SetBuffer(m_kernelHandleSpriteAnimation, "entities", entitiesBuffer);

        m_computerSpriteAnimation.SetBuffer(m_kernelHandleSpriteAnimation, "clips", clipsBuffer);

        m_computerSpriteAnimation.SetBuffer(m_kernelHandleSpriteAnimation, "frames", framesBuffer);

        m_computerSpriteAnimation.SetBuffer(m_kernelHandleSpriteAnimation, "entityInstances", entityInstancesBuffer);

        m_computerSpriteAnimation.SetBuffer(m_kernelHandleSpriteAnimation, "nodeInstances", nodeInstancesBuffer);

        m_computerSpriteAnimation.SetBuffer(m_kernelHandleSpriteAnimation, "nodeInstancesFrame", nodeInstancesFrameBuffer);

        m_computerSpriteAnimation.SetInt("nodeInstancesC", nodeInstancesCount);

        m_computerSpriteAnimationArgs = UtilsComputeShader.GetThreadGroups(
                    m_computerSpriteAnimation,
                    m_kernelHandleSpriteAnimation,
                    new Vector3Int(nodeInstancesCount, 0, 0)
            );
    }

    public void SetEntityInstancePropsBuffer(ComputeBuffer value)
    {
        //m_spritesAnimationMaterial.SetBuffer("entityInstancesProps", value); 
        m_computerSpriteAnimation.SetBuffer(m_kernelHandleSpriteAnimation, "entityInstancesProps", value);
    }

    public void Render(float time)
    {
        m_computerSpriteAnimation.SetFloat(shaderTime_id, time);
        m_computerSpriteAnimation.Dispatch(m_kernelHandleSpriteAnimation, m_computerSpriteAnimationArgs[0], m_computerSpriteAnimationArgs[1], m_computerSpriteAnimationArgs[2]);
        Graphics.DrawProcedural(m_spritesAnimationMaterial, bounds, MeshTopology.Triangles, 9, nodeInstancesCount);
    }

    public void Dispose()
    {
        if (entitiesBuffer != null) entitiesBuffer.Dispose();
        if (clipsBuffer != null) clipsBuffer.Dispose();
        if (spritesBuffer != null) spritesBuffer.Dispose();
        if (framesBuffer != null) framesBuffer.Dispose();
        //if (clipRectsBuffer != null) clipRectsBuffer.Dispose();
        if (entityInstancesBuffer != null) entityInstancesBuffer.Dispose();
        if (nodeInstancesBuffer != null) nodeInstancesBuffer.Dispose();
        if (nodeInstancesFrameBuffer != null) nodeInstancesFrameBuffer.Dispose();
    }
    #endregion
}

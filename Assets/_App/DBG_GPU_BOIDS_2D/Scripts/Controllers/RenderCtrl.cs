using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class RenderCtrl : MonoBehaviour
{
    [SerializeField] GPUAnimationData m_animationData;
    [SerializeField] ComputeShader m_computerSimpleMovement;
    [SerializeField] string m_shaderSimpleMovementKernel;
    [SerializeField] int m_entitieInstancesC = 100;

    #region Movement Buffers
    int m_kernelSimpleMovementHandle;
    ComputeBuffer m_entityPropsBuffer;
    ComputeBuffer m_entityInstancePropsBuffer;
    int[] m_computerSimpleMovementArgs;
    #endregion

    int m_entitiesInstancesAnimatedCount;
    int m_nodesInstancesAnimatedCount;
    int m_entitiesStatic;
    int m_nodesStaticCount;
    int m_shaderDeltaTime_id;

    // Start is called before the first frame update
    void Start()
    {
        InitSpriteAnimationShader();
        InitMovementBuffers();
    }

    #region Init Shaders
    void InitSpriteAnimationShader()
    {
        m_animationData.InitBounds();

        List<EntityInstance> entityInstances = new List<EntityInstance>();
        List<NodeInstance> nodeInstances = new List<NodeInstance>();

        List<Vector4> entityPositions = new List<Vector4>();

        for (int i = 0; i < m_animationData.m_entities.Length; i++)
        {
            for (int j = 0; j < m_entitieInstancesC; j++)
            {
                entityPositions.Add(new Vector4(
                    Random.Range(-m_animationData.worldViewWidth * 0.5f + 1, m_animationData.worldViewWidth * 0.5f - 1),
                    Random.Range(-m_animationData.worldViewHeight * 0.5f, m_animationData.worldViewHeight * 0.5f - 1.5f),
                    1,
                    1
                    ));
            }
            m_animationData.GetEntityInstances(i, entityPositions, entityInstances, nodeInstances);
            entityPositions.Clear();
        }

        m_entitiesInstancesAnimatedCount = entityInstances.Count;
        m_nodesInstancesAnimatedCount = nodeInstances.Count;
        m_animationData.InitSpritesAnimationShader(entityInstances, nodeInstances);
    }

    private void InitMovementBuffers()
    {
        EntityProps[] entityProps = new EntityProps[m_animationData.m_entities.Length];
        for (int i=0;i< m_animationData.m_entities.Length;i++)
        {
            entityProps[i] = new EntityProps
            {
                walkSpeed = Random.Range(0.5f, 0.6f),
                runSpeed = Random.Range(1, 1.1f),
                life = 1
            };
        }

        List<EntityInstanceProps> entityInstanceProps = new List<EntityInstanceProps>();
        for (int i = 0; i < m_entitiesInstancesAnimatedCount; i++)
        {
            float angle = Random.Range(0, 360.0f) * Mathf.Deg2Rad;
            entityInstanceProps.Add(new EntityInstanceProps
            {
                direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0)
            });
        }

        m_kernelSimpleMovementHandle = m_computerSimpleMovement.FindKernel(m_shaderSimpleMovementKernel);

        m_computerSimpleMovement.SetBuffer(m_kernelSimpleMovementHandle, "entityInstances", m_animationData.entityInstancesBuffer);

        m_entityPropsBuffer = new ComputeBuffer(entityProps.Length, sizeof(float) * 3, ComputeBufferType.Structured);
        m_entityPropsBuffer.SetData(entityProps);
        m_computerSimpleMovement.SetBuffer(m_kernelSimpleMovementHandle, "entityProps", m_entityPropsBuffer);

        m_entityInstancePropsBuffer = new ComputeBuffer(entityInstanceProps.Count, sizeof(float) * 3, ComputeBufferType.Structured);
        m_entityInstancePropsBuffer.SetData(entityInstanceProps);
        m_computerSimpleMovement.SetBuffer(m_kernelSimpleMovementHandle, "entityInstancesProps", m_entityInstancePropsBuffer);

        m_animationData.SetEntityInstancePropsBuffer(m_entityInstancePropsBuffer);

        m_computerSimpleMovementArgs = UtilsComputeShader.GetThreadGroups(
                    m_computerSimpleMovement,
                    m_kernelSimpleMovementHandle,
                    new Vector3Int(m_entitiesInstancesAnimatedCount, 0, 0)
            );
                        
        m_computerSimpleMovement.SetInt("entityInstancesC", m_entitieInstancesC * 3);
        m_computerSimpleMovement.SetVector("viewRect", 
            new Vector4(-m_animationData.worldViewWidth *0.5f - 1.5f, -m_animationData.worldViewHeight * 0.5f - 2.5f, 
            m_animationData.worldViewWidth * 0.5f + 1.5f, m_animationData.worldViewHeight * 0.5f + 0.1f));

        m_shaderDeltaTime_id = Shader.PropertyToID("deltaTime");
    }
    #endregion

    private void Render()
    {
        m_computerSimpleMovement.SetFloat(m_shaderDeltaTime_id, Time.deltaTime);
        m_computerSimpleMovement.Dispatch(m_kernelSimpleMovementHandle, m_computerSimpleMovementArgs[0], m_computerSimpleMovementArgs[1], m_computerSimpleMovementArgs[2]);
        m_animationData.Render(Time.time);
    }

    public void Dispose()
    {
        m_animationData.Dispose();
        if (m_entityPropsBuffer != null) m_entityPropsBuffer.Dispose();
        if (m_entityInstancePropsBuffer != null) m_entityInstancePropsBuffer.Dispose();
    }

    // Update is called once per frame
    void Update()
    {
        Render();
    }

    private void OnDestroy()
    {
        Dispose();
    }
}

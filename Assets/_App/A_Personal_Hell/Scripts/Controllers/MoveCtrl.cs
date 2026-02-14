using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCtrl : MonoBehaviour
{
    [SerializeField] Material m_material;
    [SerializeField] string m_shaderProp_playerTarget;
    [SerializeField] InputMove m_inputMove;

    int m_shaderProp_playerTargetID;

    void Start()
    {
        m_shaderProp_playerTargetID = Shader.PropertyToID(m_shaderProp_playerTarget);
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 target = m_inputMove.GetValue();
        //m_material.SetVector(m_shaderProp_playerTargetID, target);
        Shader.SetGlobalVector(m_shaderProp_playerTargetID, target);
    }
}

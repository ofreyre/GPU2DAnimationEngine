using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputMouseDirection : MonoBehaviour
{
    [SerializeField] Transform m_playerBody;
    Camera m_camera;

    private void Start()
    {
        m_camera = Camera.main;
    }

    public Vector3 GetValue()
    {
        var mouseWorldPos = m_camera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = m_playerBody.position.z;
        return mouseWorldPos - m_playerBody.position;
    }
}

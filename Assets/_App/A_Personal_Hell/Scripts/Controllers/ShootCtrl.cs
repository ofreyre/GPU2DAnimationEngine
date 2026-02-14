using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootCtrl : MonoBehaviour
{
    [SerializeField] Transform m_playerBody;
    [SerializeField] InputMouseDirection m_inputMousePosition;

    void Update()
    {
        Vector3 weaponDirection = m_inputMousePosition.GetValue();
    }
}

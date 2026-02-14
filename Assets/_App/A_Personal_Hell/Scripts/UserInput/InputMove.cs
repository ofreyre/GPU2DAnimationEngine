using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputMove : MonoBehaviour
{
    [SerializeField] Transform m_playerBody;

    public Vector3 GetValue()
    {
        Vector3 playerTarget = Vector3.zero;
        if (Input.GetKey(KeyCode.A)) playerTarget += Vector3.left;
        else if (Input.GetKey(KeyCode.D)) playerTarget += Vector3.right;

        if (Input.GetKey(KeyCode.S)) playerTarget += Vector3.down;
        else if (Input.GetKey(KeyCode.W)) playerTarget += Vector3.up;

        return m_playerBody.position + playerTarget;
    }
}

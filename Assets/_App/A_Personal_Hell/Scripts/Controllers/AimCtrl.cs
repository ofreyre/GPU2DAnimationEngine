using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class AimCtrl : MonoBehaviour
{
    [SerializeField] Transform m_playerBody;
    [SerializeField] InputMouseDirection m_inputMousePosition;

    bool m_flipped = false;

    // Update is called once per frame
    void LateUpdate()
    {
        Vector3 bodyDirection = m_inputMousePosition.GetValue();
        float angle = 0;
        if (bodyDirection.x < 0)
        {
            if (m_flipped)
            {
                m_flipped = false;
            }
            angle = math.degrees(math.atan2(-bodyDirection.y, -bodyDirection.x));
        }
        else
        {
            if (!m_flipped)
            {
                m_flipped = true;
            }
            angle = math.degrees(math.atan2(bodyDirection.y, bodyDirection.x));
        }
        //Debug.Log("angle = " + angle);

        m_playerBody.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImputWeaponSelection : MonoBehaviour
{
    public int GetValue()
    {
        for (int i = 0; i < (int)KeyCode.Alpha6 - (int)KeyCode.Alpha1 + 1; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                return i;
            }
        }
        return -1;
    }
}

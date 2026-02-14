using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponsCtrl : MonoBehaviour
{
    [SerializeField] ImputWeaponSelection m_imputWeaponSelection;
    [SerializeField] GameObject[] m_weapons;

    int m_weapon;

    private void Start()
    {
        SetWeapon(0);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.anyKey)
        {
            int newWeapon = m_imputWeaponSelection.GetValue();
            if (newWeapon > -1 && newWeapon < m_weapons.Length && newWeapon != m_weapon)
            {
                SetWeapon(newWeapon);
            }
        }
    }

    void SetWeapon(int newWeapon)
    {
        m_weapons[m_weapon].SetActive(false);
        m_weapon = newWeapon;
        m_weapons[m_weapon].SetActive(true);
    }
}

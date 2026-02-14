using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Threading.Tasks;
using static UnityEngine.GraphicsBuffer;

[CreateAssetMenu(fileName = "TextureAnimationClips", menuName = "DBG/New Texture Animation Clips")]
public class GPUAnimatorSetting: ScriptableObject
{
    [Serializable]
    public class PrefabEntitySettings
    {
        public GameObject prefab;
    }

    public PrefabEntitySettings[] prefabs;
    public Sprite[] sprites;

    Texture2D m_texture;
    
}

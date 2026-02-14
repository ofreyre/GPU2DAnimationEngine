using UnityEditor;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[CustomEditor(typeof(GPUAnimationData))]
public class GPUAnimationDataInspector : Editor
{

    GPUAnimationData m_target;
    int m_selectedPrefab = -1;
    int m_selectedClip = -1;
    GPUClipPlayer m_GPUClipPlayer;
    int m_tarTextureWidth = 128, m_tarTextureHeight = 128;
    Color m_defaultColor;

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];

        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();

        return result;
    }

    private void OnEnable()
    {
        m_target = (GPUAnimationData)target;
        //var animatedSpritesBuffer = new ComputeBuffer(amount, sizeof(float) * 15, ComputeBufferType.Structured);
        //animatedSpritesBuffer.SetData(positions);
        m_GPUClipPlayer = new GPUClipPlayer(m_target, m_tarTextureWidth, m_tarTextureHeight, Repaint);
    }

    private void OnDestroy()
    {
        StopAnimation();
    }

    /*
    public override bool RequiresConstantRepaint()
    {
        return true;
    }
    */

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        DisplayClips();
    }

    public void DisplayClips()
    {
        for(int i=0;i < m_target.m_prefabEntities.Length;i++)
        {
            m_defaultColor = GUI.color;
            if (m_selectedPrefab == i)
            {
                GUI.color = Color.cyan;
            }
            if (GUILayout.Button(m_target.m_prefabEntities[i].prefab.name))
            {
                m_selectedClip = -1;
                StopAnimation();
                if (m_selectedPrefab == i)
                {
                    m_selectedPrefab = -1;
                }
                else
                {
                    m_selectedPrefab = i;
                    m_selectedClip = 0;
                    PlayAnimation();
                }
            }

            GUI.color = m_defaultColor;
            if (m_selectedPrefab == i)
            {
                DisplayEntity(m_target.m_prefabEntities[i]);
            }
        }
                
        if (m_selectedPrefab != -1 && m_selectedClip != -1)
            DisplayAnimation();
    }

    void DisplayEntity(GPUAnimationData.PrefabEntity entity)
    {
        GUILayout.Label("Entity Index: " + entity.entityI.ToString());

        AnimationClip[] clips = entity.prefab.GetComponent<Animator>().runtimeAnimatorController.animationClips;
        for (int j = 0; j < clips.Length; j++)
        {
            if (m_selectedClip == j)
            {
                GUI.color = Color.green;
            }
            if (GUILayout.Button(j+" " + clips[j].name, EditorStyles.toolbarButton))
            {
                if (m_selectedClip == j)
                {
                    StopAnimation();
                    m_selectedClip = -1;
                }
                else
                {
                    m_selectedClip = j;
                    PlayAnimation();
                }
            }
            GUI.color = m_defaultColor;
        }
    }

    void DisplayAnimation()
    {
        float imageWidth = EditorGUIUtility.currentViewWidth - 40;
        float imageHeight = imageWidth * m_tarTextureHeight / m_tarTextureWidth;
        Rect rect = GUILayoutUtility.GetRect(imageWidth, imageHeight);
        GUI.DrawTexture(rect, m_GPUClipPlayer.tarText, ScaleMode.ScaleToFit);
    }

    void PlayAnimation()
    {
        if (m_selectedPrefab != -1 && m_selectedClip != -1)
        {
            m_GPUClipPlayer.Play(m_selectedPrefab, m_selectedClip);
        }
    }

    void StopAnimation()
    {
        m_GPUClipPlayer.Stop();
    }
}

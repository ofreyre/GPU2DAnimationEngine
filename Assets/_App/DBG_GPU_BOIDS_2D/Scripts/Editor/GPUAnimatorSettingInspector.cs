using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GPUAnimatorSetting))]
public class GPUAnimatorSettingInspector : Editor
{
    public enum BAKE_STATE
    {
        idle,
        baking,
        bakingComplete,
        saving
    }

    GPUAnimatorSetting m_target;
    BAKE_STATE m_bakeState = BAKE_STATE.idle;
    string m_savePath;
    GPUAnimatorBaker m_baker;

    private void OnEnable()
    {
        m_bakeState = BAKE_STATE.idle;
        m_target = (GPUAnimatorSetting)target;
    }

    private void OnDestroy()
    {
        Cancel();
    }

    public override void OnInspectorGUI()
    {
        if (m_bakeState == BAKE_STATE.idle)
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Bake"))
            {
                m_savePath = "";
                m_savePath = GetUserPathAndName();
                if (m_savePath != "" && m_savePath != null)
                {
                    Bake();
                }
            }
        }
        else if (m_bakeState == BAKE_STATE.baking) {
            if (m_baker.gpuAnimationData != null)
            {
                m_bakeState = BAKE_STATE.bakingComplete;
            }
            else
            {
                //GUILayout.BeginHorizontal();
                //GUILayout.FlexibleSpace();
                //GUILayout.EndHorizontal();
                if (GUILayout.Button("Cancel Bake"))
                {
                    Cancel();
                    m_bakeState = BAKE_STATE.idle;
                }
                Rect rect = GUILayoutUtility.GetLastRect();
                EditorGUI.ProgressBar(new Rect(rect.x, 40, Screen.width, 20), m_baker.progressPercent, "Progress = " + (m_baker.progressPercent * 100) + " %");
            }
        }
        else if(m_bakeState == BAKE_STATE.bakingComplete)
        {
            if (m_baker.gpuAnimationData != null && m_savePath != "")
            {
                Save(m_baker.gpuAnimationData, m_savePath);
            }
            m_bakeState = BAKE_STATE.idle;
        }
    }

    void SetTargetDirty()
    {
        EditorUtility.SetDirty(m_target);
    }

    void Bake()
    {
        m_bakeState = BAKE_STATE.baking;
        if (m_baker == null)
        {
            m_baker = new GPUAnimatorBaker();
        }
        m_baker.Bake(m_target, SetTargetDirty);
    }

    public void Cancel()
    {
        if(m_baker != null)
        {
            m_baker.Cancel();
        }
        m_bakeState = BAKE_STATE.idle;
    }

    string GetUserPathAndName()
    {
        //string path = EditorUtility.SaveFolderPanel("Save Animated Model", Application.dataPath, "GPUAnimationData");
        string path = EditorUtility.SaveFilePanel("Save Animated Model", Application.dataPath, "GPUAnimationData", "asset");

        if (path != "" && path != null)
        {
            int i = Application.dataPath.Length - "Assets".Length;
            //path = path.Substring(i) + "/";
            path = path.Substring(i);
            return AssetDatabase.GenerateUniqueAssetPath(path);
        }
        return "";
    }

    public void Save(ScriptableObject gpuprefabs, string path)
    {
        m_bakeState = BAKE_STATE.saving;
        AssetDatabase.CreateAsset(gpuprefabs, path);
        EditorUtility.SetDirty(gpuprefabs);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}

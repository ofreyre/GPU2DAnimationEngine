using UnityEngine;
using UnityEditor;
using static GPUAnimatorSettingInspector;
using Unity.VisualScripting;
using System;
using UnityEngine.Rendering;
using static UnityEngine.GraphicsBuffer;

[CustomEditor(typeof(TerrainSettings))]
public class TerrainSettingInspector : Editor
{
    TerrainSettings m_target;
    SerializedProperty sp_mapPower_2;
    SerializedProperty sp_limitsWidth;
    SerializedProperty sp_limitsRange;
    SerializedProperty sp_grassProb;
    SerializedProperty sp_bkgLength;
    SerializedProperty sp_projectilesCenter;
    SerializedProperty sp_projectilesRadius;
    SerializedProperty sp_projectilesMargin;
    SerializedProperty sp_projectilesFrequence;
    SerializedProperty sp_lifesCenter;
    SerializedProperty sp_lifesRadius;
    SerializedProperty sp_lifesMargin;
    SerializedProperty sp_lifesFrequence;
    SerializedProperty sp_speedsCenter;
    SerializedProperty sp_speedsRadius;
    SerializedProperty sp_speedsMargin;
    SerializedProperty sp_speedsFrequence;
    SerializedProperty sp_noTreeFactor;
    SerializedProperty sp_margin;
    SerializedProperty sp_density;
    SerializedProperty sp_tiles;
    SerializedProperty sp_coputeTerrain1;
    SerializedProperty sp_coputeTerrain2;
    SerializedProperty sp_computeSpanAreas;
    SerializedProperty sp_computeLimits;
    SerializedProperty sp_computeTiles;

    SerializedProperty sp_foldoutTerrain;
    SerializedProperty sp_foldoutCollectables;
    SerializedProperty sp_foldoutWoods;
    SerializedProperty sp_foldoutTiles;
    SerializedProperty sp_foldoutTerrainPreview;
    SerializedProperty sp_textureGenerated;

    SerializedProperty sp_rendertexture;

    ComputerTerrainGenerator m_generator;

    Sprite[] tiles;
    string[] tileNames;
    float tileSize = 64;
    float tileWithSpaceWidth = 64 + 3;
    bool generating;

    GUIStyle tileLabelStyle;

    RenderTexture m_tarText;
    ComputeShader textureShader;

    private void OnEnable()
    {
        m_target = (TerrainSettings)target;

        sp_rendertexture = serializedObject.FindProperty("m_rendertexture");

        sp_mapPower_2 = serializedObject.FindProperty("m_mapPower_2");
        sp_limitsWidth = serializedObject.FindProperty("m_limitsWidth");
        sp_limitsRange = serializedObject.FindProperty("m_limitsRange");
        sp_grassProb = serializedObject.FindProperty("m_grassProb");
        sp_bkgLength = serializedObject.FindProperty("m_bkgLength");

        sp_coputeTerrain1 = serializedObject.FindProperty("m_coputeTerrain1");
        sp_coputeTerrain2 = serializedObject.FindProperty("m_coputeTerrain2");
        sp_computeSpanAreas = serializedObject.FindProperty("m_computeSpanAreas");
        sp_computeLimits = serializedObject.FindProperty("m_computeLimits");
        sp_computeTiles = serializedObject.FindProperty("m_computeTiles");

        sp_projectilesCenter = serializedObject.FindProperty("m_projectilesCenter");
        sp_projectilesRadius = serializedObject.FindProperty("m_projectilesRadius");
        sp_projectilesMargin = serializedObject.FindProperty("m_projectilesMargin");
        sp_projectilesFrequence = serializedObject.FindProperty("m_projectilesFrequence");
        sp_lifesCenter = serializedObject.FindProperty("m_lifesCenter");
        sp_lifesRadius = serializedObject.FindProperty("m_lifesRadius");
        sp_lifesMargin = serializedObject.FindProperty("m_lifesMargin");
        sp_lifesFrequence = serializedObject.FindProperty("m_lifesFrequence");
        sp_speedsCenter = serializedObject.FindProperty("m_speedsCenter");
        sp_speedsRadius = serializedObject.FindProperty("m_speedsRadius");
        sp_speedsMargin = serializedObject.FindProperty("m_speedsMargin");
        sp_speedsFrequence = serializedObject.FindProperty("m_speedsFrequence");

        sp_noTreeFactor = serializedObject.FindProperty("m_noTreeFactor");
        sp_margin = serializedObject.FindProperty("m_margin");
        sp_density = serializedObject.FindProperty("m_density");
        sp_tiles = serializedObject.FindProperty("m_tiles");

        ComputeShader[] terrainShaders = Resources.LoadAll<ComputeShader>("Terrain/Shaders");
        for(int i=0;i< terrainShaders.Length;i++)
        {
            switch (terrainShaders[i].name)
            {
                case "ComputeInitTerrain1":
                    if(m_target.m_coputeTerrain1 == null)
                        sp_coputeTerrain1.objectReferenceValue = terrainShaders[i];
                    break;
                case "ComputeInitTerrain2":
                    if (m_target.m_coputeTerrain2 == null)
                        sp_coputeTerrain2.objectReferenceValue = terrainShaders[i];
                    break;
                case "ComputeLimits":
                    if (m_target.m_computeLimits == null)
                        sp_computeLimits.objectReferenceValue = terrainShaders[i];
                    break;
                case "ComputeSpanAreas":
                    if (m_target.m_computeSpanAreas == null)
                        sp_computeSpanAreas.objectReferenceValue = terrainShaders[i];
                    break;
                case "ComputeTerrainTiles":
                    if (m_target.m_computeTiles == null)
                        sp_computeTiles.objectReferenceValue = terrainShaders[i];
                    break;
            }
        }

        textureShader = Resources.Load<ComputeShader>("Shaders/Texture/TerrainToTexture");

        object[] loadedTiles = Resources.LoadAll("Terrain", typeof(Sprite));

        tiles = new Sprite[loadedTiles.Length];
        tiles[0b00000000] = (Sprite)(loadedTiles[5]);//no grass
        tiles[0b00001000] = (Sprite)(loadedTiles[13]);//top left
        tiles[0b00001100] = (Sprite)(loadedTiles[2]);//top
        tiles[0b00000100] = (Sprite)(loadedTiles[12]);//top right
        tiles[0b00001010] = (Sprite)(loadedTiles[4]);//left
        tiles[0b00000101] = (Sprite)(loadedTiles[6]);//right
        tiles[0b00000010] = (Sprite)(loadedTiles[11]);//botom left
        tiles[0b00000011] = (Sprite)(loadedTiles[8]);//botom
        tiles[0b00000001] = (Sprite)(loadedTiles[10]);//botom right
        tiles[0b00001111] = (Sprite)(loadedTiles[0]);//Surrounded
        tiles[0b00001110] = (Sprite)(loadedTiles[1]);//top-left
        tiles[0b00001101] = (Sprite)(loadedTiles[3]);//top-right
        tiles[0b00001011] = (Sprite)(loadedTiles[7]);//bottom-left
        tiles[0b00000111] = (Sprite)(loadedTiles[9]);//bottom-right
        tiles[0b00001001] = (Sprite)(loadedTiles[14]);//top-left, bottom-right
        tiles[0b00000110] = (Sprite)(loadedTiles[15]);//top-right bottom-left

        tileNames = new string[loadedTiles.Length];
        tileNames[0b00000000] = "Enpty";
        tileNames[0b00001000] = "tl";
        tileNames[0b00001100] = "top";
        tileNames[0b00000100] = "tr";
        tileNames[0b00001010] = "left";
        tileNames[0b00000101] = "right";
        tileNames[0b00000010] = "bl";
        tileNames[0b00000011] = "b";
        tileNames[0b00000001] = "br";
        tileNames[0b00001111] = "Full";
        tileNames[0b00001110] = "t-l";
        tileNames[0b00001101] = "t-r";
        tileNames[0b00001011] = "b-l";
        tileNames[0b00000111] = "b-r";
        tileNames[0b00001001] = "tl-br";
        tileNames[0b00000110] = "bl-tr";

        if(sp_tiles.arraySize < 16)
        {
            for(int i= sp_tiles.arraySize; i < 16; i++)
            {
                sp_tiles.InsertArrayElementAtIndex(i);
                SerializedProperty tile = sp_tiles.GetArrayElementAtIndex(i);
                tile.objectReferenceValue = tiles[i];
            }
        }

        sp_foldoutTerrain = serializedObject.FindProperty("m_foldoutTerrain");
        sp_foldoutCollectables = serializedObject.FindProperty("m_foldoutCollectables");
        sp_foldoutWoods = serializedObject.FindProperty("m_foldoutWoods");
        sp_foldoutTiles = serializedObject.FindProperty("m_foldoutTiles");
        sp_foldoutTerrainPreview = serializedObject.FindProperty("m_foldoutTerrainPreview");
        sp_textureGenerated = serializedObject.FindProperty("m_textureGenerated");
    }

    private void OnDestroy()
    {
        if(m_generator != null)
            m_generator.Release();

        if(m_tarText != null)
            m_tarText.Release();
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(sp_rendertexture, GUILayout.ExpandWidth(false));

        EditorGUIUtility.labelWidth = 150;

        sp_foldoutTerrain.boolValue = EditorGUILayout.Foldout(sp_foldoutTerrain.boolValue, "Terrain");
        if (sp_foldoutTerrain.boolValue)
        {
            EditorGUILayout.PropertyField(sp_mapPower_2, GUILayout.ExpandWidth(false));
            EditorGUILayout.PropertyField(sp_limitsWidth, GUILayout.ExpandWidth(false));
            EditorGUILayout.PropertyField(sp_limitsRange, GUILayout.ExpandWidth(false));
            EditorGUILayout.PropertyField(sp_grassProb, GUILayout.ExpandWidth(false));
            EditorGUILayout.PropertyField(sp_bkgLength, GUILayout.ExpandWidth(false));
            EditorGUILayout.PropertyField(sp_coputeTerrain1, GUILayout.ExpandWidth(false), GUILayout.Width(350));
            EditorGUILayout.PropertyField(sp_coputeTerrain2, GUILayout.ExpandWidth(false), GUILayout.Width(350));
            EditorGUILayout.PropertyField(sp_computeSpanAreas, GUILayout.ExpandWidth(false), GUILayout.Width(350));
            EditorGUILayout.PropertyField(sp_computeLimits, GUILayout.ExpandWidth(false), GUILayout.Width(350));
            EditorGUILayout.PropertyField(sp_computeTiles, GUILayout.ExpandWidth(false), GUILayout.Width(350));
        }

        GUILayout.Space(10);
        sp_foldoutCollectables.boolValue = EditorGUILayout.Foldout(sp_foldoutCollectables.boolValue, "Collectables");
        if (sp_foldoutCollectables.boolValue)
        {
            EditorGUILayout.PropertyField(sp_projectilesCenter, GUILayout.ExpandWidth(false), GUILayout.Width(300));
            EditorGUILayout.PropertyField(sp_projectilesRadius, GUILayout.ExpandWidth(false));
            EditorGUILayout.PropertyField(sp_projectilesMargin, GUILayout.ExpandWidth(false));
            EditorGUILayout.PropertyField(sp_projectilesFrequence, GUILayout.ExpandWidth(false));
            EditorGUILayout.PropertyField(sp_lifesCenter, GUILayout.ExpandWidth(false), GUILayout.Width(300));
            EditorGUILayout.PropertyField(sp_lifesRadius, GUILayout.ExpandWidth(false));
            EditorGUILayout.PropertyField(sp_lifesMargin, GUILayout.ExpandWidth(false));
            EditorGUILayout.PropertyField(sp_lifesFrequence, GUILayout.ExpandWidth(false));
            EditorGUILayout.PropertyField(sp_speedsCenter, GUILayout.ExpandWidth(false), GUILayout.Width(300));
            EditorGUILayout.PropertyField(sp_speedsRadius, GUILayout.ExpandWidth(false));
            EditorGUILayout.PropertyField(sp_speedsMargin, GUILayout.ExpandWidth(false));
            EditorGUILayout.PropertyField(sp_speedsFrequence, GUILayout.ExpandWidth(false));
        }

        GUILayout.Space(10);
        sp_foldoutWoods.boolValue = EditorGUILayout.Foldout(sp_foldoutWoods.boolValue, "Woods");
        if (sp_foldoutWoods.boolValue)
        {
            EditorGUILayout.PropertyField(sp_noTreeFactor, GUILayout.ExpandWidth(false));
            EditorGUILayout.PropertyField(sp_margin, GUILayout.ExpandWidth(false));
            EditorGUILayout.PropertyField(sp_density, GUILayout.ExpandWidth(false));
        }

        GUILayout.Space(10);
        sp_foldoutTiles.boolValue = EditorGUILayout.Foldout(sp_foldoutTiles.boolValue, "Tiles");
        bool tile_changed = false;
        if (sp_foldoutTiles.boolValue)
        {
            if (GUILayout.Button("Restore Tile References"))
            {
                for (int i = 0; i < 16; i++)
                {
                    sp_tiles.InsertArrayElementAtIndex(i);
                    SerializedProperty tile = sp_tiles.GetArrayElementAtIndex(i);
                    tile.objectReferenceValue = tiles[i];
                }
            }

            if (tileLabelStyle == null)
                tileLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            float width = EditorGUIUtility.currentViewWidth - EditorStyles.inspectorDefaultMargins.padding.left - EditorStyles.inspectorDefaultMargins.padding.right;
            int cols = (int)Mathf.Max(1, width / tileWithSpaceWidth);

            for (int i = 0; i < 16; i++)
            {
                if (i % cols == 0)
                {
                    if (i > 0)
                        EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal(GUILayout.Width(tileWithSpaceWidth * cols));
                }
                SerializedProperty tile = sp_tiles.GetArrayElementAtIndex(i); // get array element at x
                UnityEngine.Object tile_reference = tile.objectReferenceValue;

                EditorGUILayout.BeginVertical(GUILayout.Width(tileSize));
                EditorGUILayout.LabelField(tileNames[i], tileLabelStyle, GUILayout.Width(tileSize));
                var texture = AssetPreview.GetAssetPreview(tiles[i]);
                GUILayout.Label(texture, EditorStyles.helpBox, GUILayout.Width(tileSize), GUILayout.Height(tileSize));
                tile.objectReferenceValue = EditorGUILayout.ObjectField(tile.objectReferenceValue,
                    typeof(Sprite), false, GUILayout.Width(tileSize), GUILayout.Height(tileSize));
                //EditorGUILayout.LabelField(tileNames[i], GUILayout.Width(tileSize));

                if(tile.objectReferenceValue != tile_reference)
                {
                    tile_changed = true;
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        sp_foldoutTerrainPreview.boolValue = EditorGUILayout.Foldout(sp_foldoutTerrainPreview.boolValue, "Terrain Preview");
        if (sp_foldoutTerrainPreview.boolValue)
        {
            if (!generating)
            {
                if (GUILayout.Button("Generate Map"))
                {
                    GenerateMap();
                }
                DisplayTexture();
            }
            else
            {
                GUILayout.Label("Generating Map...");
            }
        }

        if (EditorGUI.EndChangeCheck())
        {   
            serializedObject.ApplyModifiedProperties();
            if (tile_changed)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }

    void DisplayTexture()
    {
        if (m_tarText == null)
        {
            if (sp_textureGenerated.boolValue)
            {
                GenerateMap();
            }
        }

        if(m_tarText != null)
        {
            float width = EditorGUIUtility.currentViewWidth - EditorStyles.inspectorDefaultMargins.padding.left - EditorStyles.inspectorDefaultMargins.padding.right;        
            Rect rect = GUILayoutUtility.GetRect(width, width);
            GUI.DrawTexture(rect, m_tarText, ScaleMode.ScaleToFit, false);
        }
    }

    void GenerateMap()
    {
        generating = true;
        if (m_generator == null)
        {
            m_generator = new ComputerTerrainGenerator();
        }
        m_generator.GenerateMap(m_target, MapReadyHandler);
    }

    void MapReadyHandler(float[] map)
    {
        if (m_tarText == null || m_tarText.width != m_target.SideLength)
        {
            GenerateTerrainTexture();
        }

        float min = float.MaxValue;
        float max = float.MinValue;
        for (int i = 0; i < map.Length; i++)
        {
            if(min > map[i]) min = map[i];
            if(max < map[i]) max = map[i];
        }

        int shaderKernelHandle = textureShader.FindKernel("CSMain");
        int[] computerArgs = UtilsComputeShader.GetThreadGroups(
                    textureShader,
                    shaderKernelHandle,
                    new Vector3Int(m_target.SideLength, m_target.SideLength, 0)
            );

        textureShader.SetBuffer(shaderKernelHandle, "map", m_generator.m_mapBuffer);
        textureShader.SetInt("sideLength", m_target.SideLength);
        textureShader.SetFloat("min", min);
        textureShader.SetFloat("max", max);
        textureShader.SetTexture(shaderKernelHandle, "Result", m_tarText);
        textureShader.Dispatch(shaderKernelHandle, computerArgs[0], computerArgs[1], computerArgs[2]);
        AsyncGPUReadback.Request(m_tarText, 0,TextureReadyCallback);
    }

    void TextureReadyCallback(AsyncGPUReadbackRequest readbackRequest)
    {
        if (!readbackRequest.hasError)
        {
            sp_textureGenerated.boolValue = true;
        }
        generating = false;
    }

    void GenerateTerrainTexture()
    {
        if (m_tarText == null || m_tarText.width != m_target.SideLength)
        {
            if (m_tarText != null)
                m_tarText.Release();

            m_tarText = new RenderTexture(m_target.SideLength, m_target.SideLength, 24, RenderTextureFormat.ARGB32)
            {
                enableRandomWrite = true
            };
            m_tarText.filterMode = FilterMode.Point;
            serializedObject.ApplyModifiedProperties();
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TerrainSettings", menuName = "DBG/New Terrain Settings")]
public class TerrainSettings : ScriptableObject
{
    public RenderTexture m_rendertexture;

    [Header("Terrain")]
    public int m_mapPower_2;
    public int m_limitsWidth = 4;
    public int m_limitsRange = 3;
    public Sprite[] m_tiles;
    public Material m_material;
    public float m_grassProb = 0.3f;
    public Sprite[] m_bkgSprites;
    public Material m_bkgMaterial;
    public int m_bkgLength = 8;
    public ComputeShader m_coputeTerrain1;
    public ComputeShader m_coputeTerrain2;
    public ComputeShader m_computeSpanAreas;
    public ComputeShader m_computeLimits;
    public ComputeShader m_computeTiles;

    [Header("Collectables")]
    public Vector3 m_projectilesCenter = new Vector3(500, 500, 0);
    public float m_projectilesRadius = 10;
    public float m_projectilesMargin = 2;
    public float m_projectilesFrequence = 20;
    public Vector3 m_lifesCenter = new Vector3(100, 100, 0);
    public float m_lifesRadius = 10;
    public float m_lifesMargin = 2;
    public float m_lifesFrequence = 20;
    public Vector3 m_speedsCenter = new Vector3(900, 700, 0);
    public float m_speedsRadius = 10;
    public float m_speedsMargin = 2;
    public float m_speedsFrequence = 20;

    [Header("Woods")]
    public float m_noTreeFactor = 100;
    public int m_margin = 5;
    public int m_density = 3;

    [HideInInspector] public int SideLength;
    [HideInInspector] public int BKGSideLength;
    [HideInInspector] public int TerrainBkgCount;
    [HideInInspector] public int MapTilesCount;
    [HideInInspector] public int TerrainCount;

    #region EDITOR VARIABLES
    [HideInInspector] public bool m_foldoutTerrain = true;
    [HideInInspector] public bool m_foldoutCollectables = true;
    [HideInInspector] public bool m_foldoutWoods = true;
    [HideInInspector] public bool m_foldoutTiles = true;
    [HideInInspector] public bool m_foldoutTerrainPreview = true;
    [HideInInspector] public bool m_textureGenerated = true;
    #endregion

    public void Init()
    {
        SideLength = (int)Mathf.Pow(2, m_mapPower_2) + 1;
        BKGSideLength = SideLength / m_bkgLength;
        TerrainBkgCount = BKGSideLength * BKGSideLength;
        MapTilesCount = SideLength * SideLength;
        TerrainCount = SideLength * SideLength;
    }

    public Vector4[] GetSpritesRects(Sprite[] sprites)
    {
        Vector4[] rects = new Vector4[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            rects[i] = new Vector4(sprite.uv[2].x,
                sprite.uv[2].y,
                sprite.uv[1].x,
                sprite.uv[1].y
                );
        }
        return rects;
    }

    public Vector4[] GetTilesUVs()
    {
        return GetSpritesRects(m_tiles);
    }

    public Vector4[] GetBkgTilesUVs()
    {
        return GetSpritesRects(m_bkgSprites);
    }

    public void GenerateMapMap(TerrainSettings settings, Action<float[]> mapReadyHandler)
    {
        ComputerTerrainGenerator generator = new ComputerTerrainGenerator();
        generator.GenerateMap(this, MapGenerationHandler);
    }

    void MapGenerationHandler(float[] map)
    {

    }
}

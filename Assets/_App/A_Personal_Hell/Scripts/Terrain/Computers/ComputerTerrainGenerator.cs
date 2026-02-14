using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using System.Collections;

using System.Threading.Tasks;
using Unity.VisualScripting;

public class ComputerTerrainGenerator
{
    public ComputeBuffer m_mapBuffer;
    public ComputeBuffer m_tilesBuffer;
    public ComputeBuffer m_spritesTilesBuffer;
    Action<float[]> m_mapReadyHandler;
    Action<int[]> m_mapTilesReadyHandler;
    TerrainSettings m_settings;

    float[] m_map;
    int[] m_mapTiles;

    ~ComputerTerrainGenerator()
    {
        if (m_mapBuffer != null)
        {
            m_mapBuffer.Dispose();
        }
    }

    public void GenerateBackground(TerrainSettings settings)
    {   
    }

    public void GenerateMap(TerrainSettings settings, Action<float[]> mapReadyHandler, Action<int[]> mapTilesReadyHandler)
    {
        m_mapTilesReadyHandler = mapTilesReadyHandler;
        GenerateMap(settings, mapReadyHandler);
    }

    public void GenerateMap(TerrainSettings settings, Action<float[]> mapReadyHandler)
    {
        m_settings = settings;
        if (settings.m_rendertexture != null)
        {
            Task<Color[]> task = Task.Run(() => GenerateTerrainGPU(settings));

            // Wait for the task to complete and get the result
            Color[] colors = task.Result;


            Texture2D texture2D = new Texture2D(settings.SideLength, settings.SideLength, TextureFormat.RGBA32, false);

            // Set the pixels of the Texture2D
            texture2D.SetPixels(colors);
            texture2D.Apply();

            // Set the active RenderTexture
            RenderTexture.active = settings.m_rendertexture;

            // Copy the Texture2D to the RenderTexture
            Graphics.Blit(texture2D, settings.m_rendertexture);

            // Release the active RenderTexture
            RenderTexture.active = null;
        }

        m_mapReadyHandler = mapReadyHandler;

        settings.Init();
        int mapCount = settings.SideLength * settings.SideLength;
        float[] map = new float[mapCount];
        map[0] = map[(settings.SideLength - 1) * settings.SideLength] = map[settings.SideLength - 1] =
              map[settings.SideLength - 1 + (settings.SideLength - 1) * settings.SideLength] = 0.8f;
        float variation = 1f;
        int randomLength = (settings.SideLength - 1) / (int)Mathf.Pow(2, 4);
        float randomRange = 1;
        float rndSeed = 357.923564f;

        int shaderKernelHandle1 = settings.m_coputeTerrain1.FindKernel("CSMain");
        int shaderKernelHandle2 = settings.m_coputeTerrain2.FindKernel("CSMain");
        int id_sideLength = Shader.PropertyToID("sideLength");
        int id_m_sideLength = Shader.PropertyToID("m_sideLength");
        int id_randomLength = Shader.PropertyToID("randomLength");
        int id_halfSide = Shader.PropertyToID("halfSide");
        int id_randomRange = Shader.PropertyToID("randomRange");
        int id_rndSeed = Shader.PropertyToID("rndSeed");
        int id_mapMin = Shader.PropertyToID("mapMin");
        int id_mapMax = Shader.PropertyToID("mapMax");
        int id_variation = Shader.PropertyToID("variation");
        int id_playerCenter = Shader.PropertyToID("playerCenter");
        int id_playerRadius = Shader.PropertyToID("playerRadius");

        settings.m_coputeTerrain1.SetInt(id_m_sideLength, settings.SideLength);
        settings.m_coputeTerrain2.SetInt(id_m_sideLength, settings.SideLength);
        settings.m_coputeTerrain1.SetInt(id_randomLength, randomLength);
        settings.m_coputeTerrain2.SetInt(id_randomLength, randomLength);
        settings.m_coputeTerrain1.SetFloat(id_randomRange, randomRange);
        settings.m_coputeTerrain2.SetFloat(id_randomRange, randomRange);
        settings.m_coputeTerrain1.SetFloat(id_rndSeed, rndSeed);
        settings.m_coputeTerrain2.SetFloat(id_rndSeed, rndSeed);
        settings.m_coputeTerrain1.SetVector(id_playerCenter, settings.m_projectilesCenter);
        settings.m_coputeTerrain2.SetFloat(id_playerRadius, settings.m_projectilesRadius);


        m_mapBuffer = new ComputeBuffer(map.Length, sizeof(float), ComputeBufferType.Structured);
        m_mapBuffer.SetData(map);
        settings.m_coputeTerrain1.SetBuffer(shaderKernelHandle1, "map", m_mapBuffer);
        settings.m_coputeTerrain2.SetBuffer(shaderKernelHandle2, "map", m_mapBuffer);
        int[] m_computerArgs;

        for (int sideLength = settings.SideLength - 1; sideLength >= 2; sideLength /= 2, variation *= 0.7f)
        {
            int halfSide = sideLength / 2;
            int n = (settings.SideLength - 1) / sideLength;

            m_computerArgs = UtilsComputeShader.GetThreadGroups(
                    settings.m_coputeTerrain1,
                    shaderKernelHandle1,
                    new Vector3Int(n, n, 0)
            );

            settings.m_coputeTerrain1.SetInt(id_sideLength, sideLength);
            settings.m_coputeTerrain1.SetInt(id_halfSide, halfSide);
            settings.m_coputeTerrain1.SetVector(id_mapMin, new Vector4(0, 0,0,0));
            settings.m_coputeTerrain1.SetVector(id_mapMax, new Vector4(n, n, 0, 0));
            settings.m_coputeTerrain1.SetFloat(id_variation, variation);
            settings.m_coputeTerrain1.Dispatch(shaderKernelHandle1, m_computerArgs[0], m_computerArgs[1], m_computerArgs[2]);

            n = (settings.SideLength - 1) / halfSide;
            int m = (settings.SideLength - 1) / sideLength;

            m_computerArgs = UtilsComputeShader.GetThreadGroups(
                    settings.m_coputeTerrain1,
                    shaderKernelHandle1,
                    new Vector3Int(n, m, 0)
            );

            settings.m_coputeTerrain2.SetInt(id_sideLength, sideLength);
            settings.m_coputeTerrain2.SetInt(id_halfSide, halfSide);
            settings.m_coputeTerrain2.SetVector(id_mapMin, new Vector4(0, 0, 0, 0));
            settings.m_coputeTerrain2.SetVector(id_mapMax, new Vector4(n, m, 0, 0));
            settings.m_coputeTerrain2.SetFloat(id_variation, variation);
            settings.m_coputeTerrain2.Dispatch(shaderKernelHandle1, m_computerArgs[0], m_computerArgs[1], m_computerArgs[2]);
            //AsyncGPUReadback.Request(m_mapBuffer, MapCallback);
        }

        //Spawn areas
        shaderKernelHandle1 = settings.m_computeSpanAreas.FindKernel("CSMain");
        settings.m_computeSpanAreas.SetBuffer(shaderKernelHandle1, "map", m_mapBuffer);
        m_computerArgs = UtilsComputeShader.GetThreadGroups(
                    settings.m_computeSpanAreas,
                    shaderKernelHandle1,
                    new Vector3Int(settings.SideLength, settings.SideLength, 0)
            );

        settings.m_computeSpanAreas.SetInt(id_m_sideLength, settings.SideLength);
        settings.m_computeSpanAreas.SetVector("projectilesCenter", settings.m_projectilesCenter);
        settings.m_computeSpanAreas.SetFloat("projectilesRadius", settings.m_projectilesRadius);
        settings.m_computeSpanAreas.SetFloat("projectilesMargin", settings.m_projectilesMargin);
        settings.m_computeSpanAreas.SetFloat("projectilesFrequence", settings.m_projectilesFrequence);

        settings.m_computeSpanAreas.SetVector("lifesCenter", settings.m_lifesCenter);
        settings.m_computeSpanAreas.SetFloat("lifesRadius", settings.m_lifesRadius);
        settings.m_computeSpanAreas.SetFloat("lifesMargin", settings.m_lifesMargin);
        settings.m_computeSpanAreas.SetFloat("lifesFrequence", settings.m_lifesFrequence);

        settings.m_computeSpanAreas.SetVector("speedsCenter", settings.m_speedsCenter);
        settings.m_computeSpanAreas.SetFloat("speedsRadius", settings.m_speedsRadius);
        settings.m_computeSpanAreas.SetFloat("speedsMargin", settings.m_speedsMargin);
        settings.m_computeSpanAreas.SetFloat("speedsFrequence", settings.m_speedsFrequence);

        settings.m_computeSpanAreas.Dispatch(shaderKernelHandle1, m_computerArgs[0], m_computerArgs[1], m_computerArgs[2]);
        //AsyncGPUReadback.Request(m_mapBuffer, MapCallback);

        //World Limits
        shaderKernelHandle1 = settings.m_computeLimits.FindKernel("CSMain");
        settings.m_computeLimits.SetBuffer(shaderKernelHandle1, "map", m_mapBuffer);
        m_computerArgs = UtilsComputeShader.GetThreadGroups(
                    settings.m_computeSpanAreas,
                    shaderKernelHandle1,
                    new Vector3Int(settings.SideLength, settings.SideLength, 0)
            );

        settings.m_computeLimits.SetInt(id_m_sideLength, settings.SideLength);
        settings.m_computeLimits.SetFloat("limitsWidth", settings.m_limitsWidth);
        settings.m_computeLimits.SetFloat("limitsRange", settings.m_limitsRange);
        settings.m_computeLimits.SetFloat(id_rndSeed, rndSeed);

        settings.m_computeLimits.Dispatch(shaderKernelHandle1, m_computerArgs[0], m_computerArgs[1], m_computerArgs[2]);

        AsyncGPUReadback.Request(m_mapBuffer, MapCallback);

        if (m_mapTilesReadyHandler != null)
        {
            //Terrain Tiles
            shaderKernelHandle1 = settings.m_computeTiles.FindKernel("CSMain");
            m_tilesBuffer = new ComputeBuffer(map.Length, sizeof(int), ComputeBufferType.Structured);
            int[] spritesTiles = GetSpritesTilesIndex(settings);
            m_spritesTilesBuffer = new ComputeBuffer(spritesTiles.Length, sizeof(int), ComputeBufferType.Structured);
            m_spritesTilesBuffer.SetData(spritesTiles);

            settings.m_computeTiles.SetBuffer(shaderKernelHandle1, "terrain", m_tilesBuffer);
            settings.m_computeTiles.SetBuffer(shaderKernelHandle1, "spritesTiles", m_spritesTilesBuffer);
            settings.m_computeTiles.SetBuffer(shaderKernelHandle1, "map", m_mapBuffer);

            m_computerArgs = UtilsComputeShader.GetThreadGroups(
                        settings.m_computeTiles,
                        shaderKernelHandle1,
                        new Vector3Int(settings.SideLength, settings.SideLength, 0)
                );

            settings.m_computeTiles.SetInt(id_m_sideLength, settings.SideLength);
            settings.m_computeTiles.SetFloat("grassProb", settings.m_grassProb);

            settings.m_computeTiles.Dispatch(shaderKernelHandle1, m_computerArgs[0], m_computerArgs[1], m_computerArgs[2]);

            AsyncGPUReadback.Request(m_tilesBuffer, MapTilesCallback);
        }

        //TestNoise2();
    }
    void MapCallback(AsyncGPUReadbackRequest readbackRequest)
    {
        if (!readbackRequest.hasError)
        {
        }
        else
        {
        }

        //if(m_step >= m_steps)
        {
            m_map = readbackRequest.GetData<float>().ToArray();
            m_mapReadyHandler(m_map);
            m_map = null;
            if (m_mapTilesReadyHandler != null)
            {
                m_mapBuffer.Release();
            }
        }
    }

    void MapTilesCallback(AsyncGPUReadbackRequest readbackRequest)
    {
        if (!readbackRequest.hasError)
        {  
        }
        else
        {
        }

        //if(m_step >= m_steps)
        {
            m_mapTiles = readbackRequest.GetData<int>().ToArray();
            m_mapTilesReadyHandler(m_mapTiles);
            m_mapTiles = null;
            m_tilesBuffer.Release();
            m_spritesTilesBuffer.Release();
        }
    }

    int[] GetSpritesTilesIndex(TerrainSettings settings)
    {
        Sprite[] sprites = settings.m_tiles;
        int[] tileIndices = new int[sprites.Length];
        int sideTiles = 4;
        int textureSide = sprites[0].texture.width;
        int tileSize = textureSide / sideTiles;
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            tileIndices[i] = (int)sprite.rect.x / tileSize + ((int)sprite.rect.y / tileSize) * sideTiles;
        }
        return tileIndices;
    }

    public void Release()
    {
        if(m_mapBuffer != null)
            m_mapBuffer.Release();
    }


    #region TEST
    void TestNoise2()
    {
        float[] floats = new float[513];
        for(int i = 0; i < 513; i++)
        {
            floats[i] = RandomRange2(0, 4, i, 357.923564f);
        }
    }

    float RND(float min, float max, System.Random random)
    {
        return min + (float)random.NextDouble() * (max - min);
    }

    float Noise(int seed1, int seed2, float seed3)
    {
        Vector3 v0 = new Vector3(seed1, seed2, seed3);
        Vector3 v1 = new Vector3(12.9898f, 78.233f, 45.164f);
        float dot = Vector3.Dot(v0, v1);
        float sin = Mathf.Sin(dot);
        return sin - Mathf.Floor(sin);
    }

    float Noise_Range(float min, float max, int seed1, int seed2, float seed3)
    {
        float r = Noise(seed1, seed2, seed3);
        return min + (max - min) * r;
    }

    float Noise2(int seed1, float seed2)
    {
        // Combine seeds into a single value
        float combinedSeed = Mathf.Abs(Mathf.Sin(Vector2.Dot(new Vector2(seed1, seed2), new Vector2(12.9898f, 78.233f))) * 43758.5453f);
        return combinedSeed % 1;
    }

    float RandomRange2(float min, float max, int seed1, float seed2)
    {
        float r = Noise2(seed1, seed2);
        return min + (max - min) * r;
    }

    Color[] GenerateTerrainGPU(TerrainSettings settings)
    {
        Debug.Log("GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG");
        System.Random random = new System.Random();
        float rndSeed = 357.923564f;
        int mapCount = settings.SideLength * settings.SideLength;
        float[] map = new float[mapCount];

        map[0] = map[(settings.SideLength - 1) * settings.SideLength] = map[settings.SideLength - 1] =
              map[settings.SideLength - 1 + (settings.SideLength - 1) * settings.SideLength] = 0.8f;

        float variation = 1f;

        int randomLength = (settings.SideLength - 1) / (int)math.pow(2, 4);
        float randomRange = 1;

        for (int sideLength = settings.SideLength - 1; sideLength >= 2; sideLength /= 2, variation *= 0.7f)
        {
            int halfSide = sideLength / 2;

            for (int x = 0; x < settings.SideLength - 1; x += sideLength)
            {
                for (int y = 0; y < settings.SideLength - 1; y += sideLength)
                {
                    if (sideLength > randomLength)
                    {
                        map[x + halfSide + (y + halfSide) * settings.SideLength] = Noise_Range(0, randomRange, x, y, rndSeed);
                    }
                    else
                    {
                        //x, y is upper left corner of square
                        //calculate average of existing corners
                        float avg = map[x + y * settings.SideLength] + //top left
                        map[x + sideLength + y * settings.SideLength] +//top right
                        map[x + (y + sideLength) * settings.SideLength] + //lower left
                        map[x + sideLength + (y + sideLength) * settings.SideLength];//lower right
                        avg /= 4.0f;

                        //center is average plus random offset
                        map[x + halfSide + (y + halfSide) * settings.SideLength] =
                        //We calculate random value in range of 2h
                        //and then subtract h so the end value is
                        //in the range (-h, +h)
                        avg + Noise_Range(-variation, variation, x, y, rndSeed);
                    }
                    
                }
            }

            for (int x = 0; x < settings.SideLength - 1; x += halfSide)
            {
                for (int y = (x + halfSide) % sideLength; y < settings.SideLength - 1; y += sideLength)
                {
                    if (sideLength > randomLength)
                    {
                        map[x + halfSide + (y + halfSide) * settings.SideLength] = Noise_Range(0, randomRange, x, y, rndSeed);
                        if (x == 0) map[settings.SideLength - 1 + y * settings.SideLength] = Noise_Range(0, randomRange, x, y, rndSeed);
                        if (y == 0) map[x + (settings.SideLength - 1) * settings.SideLength] = Noise_Range(0, randomRange, x, y, rndSeed);
                    }
                    else
                    {
                        //x, y is center of diamond
                        //note we must use mod  and add DATA_SIZE for subtraction 
                        //so that we can wrap around the array to find the corners
                        float avg =
                    map[((x - halfSide + settings.SideLength) % settings.SideLength) + y * settings.SideLength] + //left of center
                    map[((x + halfSide) % settings.SideLength) + y * settings.SideLength] + //right of center
                    map[x + ((y + halfSide) % settings.SideLength) * settings.SideLength] + //below center
                    map[x + ((y - halfSide + settings.SideLength) % settings.SideLength) * settings.SideLength]; //above center
                        avg /= 4.0f;

                        //new value = average plus random offset
                        //We calculate random value in range of 2h
                        //and then subtract h so the end value is
                        //in the range (-h, +h)
                        avg += Noise_Range(-variation, variation, x, y, rndSeed);
                        //update value for center of diamond
                        map[x + y * settings.SideLength] = avg;

                        //wrap values on the edges, remove
                        //this and adjust loop condition above
                        //for non-wrapping values.
                        if (x == 0) map[settings.SideLength - 1 + y * settings.SideLength] = avg;
                        if (y == 0) map[x + (settings.SideLength - 1) * settings.SideLength] = avg;
                    }
                }
            }
        }

        Color[] colors = new Color[mapCount];
        for(int i=0;i< mapCount; i++)
        {
            colors[i] = new Color(map[i], map[i], map[i], 1);
        }

        return colors;
    }
    #endregion TEST

}

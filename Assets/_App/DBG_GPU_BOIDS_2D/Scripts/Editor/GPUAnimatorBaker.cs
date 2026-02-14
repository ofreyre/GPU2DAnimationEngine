using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;
using PrefabEntitySettings = GPUAnimatorSetting.PrefabEntitySettings;
using PrefabEntity = GPUAnimationData.PrefabEntity;
using System.Text;
using System.Threading;
using System;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using System.Security.Cryptography;

public class GPUAnimatorBaker
{
    public class BindedCurve
    {
        public string propertyName;
        public AnimationCurve curve;
    }

    public class BindedKeyframe
    {
        public string propertyName;
        public ObjectReferenceKeyframe[] curve;
    }

    #region SHADER BUFFERS
    List<GPUSprite> m_gpusprites = new List<GPUSprite>();
    Entity[] m_entities;
    List<Node> m_nodes = new List<Node>();
    List<Clip> m_clips = new List<Clip>();
    List<Frame> m_frames = new List<Frame>();
    #endregion

    #region EDITOR GPU ANIMATION DATA
    List<PrefabEntity> m_prefabEntities = new List<PrefabEntity>();
    List<Vector4> m_frameRects = new List<Vector4>();
    List<Vector4> m_clipRects = new List<Vector4>();
    List<Vector4> m_entityRects = new List<Vector4>();
    Vector2Int m_textureSize;
    Vector2 m_UV2WS;
    List<Texture2D> m_textures = new List<Texture2D>();
    #endregion

    #region BAKER EDITOR
    float m_progress;
    float m_progressTotal;
    #endregion

    int m_entityI;
    int m_nodesStartI;

    // Accumulates sprites of all entitites
    List<Sprite> m_sprites = new List<Sprite>();

    // Starts clear for each entity
    List<string> m_bindPaths = new List<string>();                                          // Filled by ProcesTransform(Transform t) : AddLastPath(List<Transform> b)
    Dictionary<string, Sprite> m_mapPathSprite = new Dictionary<string, Sprite>();
    Dictionary<string, Transform> m_mapPathTransform = new Dictionary<string, Transform>();
    List<Transform> m_nodesTransforms = new List<Transform>();
    List<Node> m_entityNodes = new List<Node>();

    // Starts clear for each clip
    Dictionary<string, List<BindedCurve>> m_curves = new Dictionary<string, List<BindedCurve>>();
    Dictionary<string, List<BindedKeyframe>> m_keyFrames = new Dictionary<string, List<BindedKeyframe>>();

    //FOR INSPECTOR DEBUGING
    //List<Sprite> m_spritesOrder = new List<Sprite>();

    // Starts clear for each path
    StringBuilder m_stringBuilder = new StringBuilder();

    GPUAnimationData m_gpuAnimationData;

    Vector3[] m_spriteVertices = new Vector3[] { 
        new Vector3(0, 0, 1), new Vector3(1, 0, 1),
        new Vector3(0, 1, 1), new Vector3(1, 1, 1),
    };

    Vector3[] m_spriteVerticesTest = new Vector3[4];

    Action m_progressHandler;


    CancellationTokenSource m_cancellationTokenBake;
    CancellationTokenSource m_cancellationTokenInternal;

    /*
    public void Bake(PrefabEntitySettings[] prefabsEntity)
    {
        _Bake(prefabsEntity);
    }*/

    public GPUAnimationData gpuAnimationData { get { return m_gpuAnimationData; } }

    public float progress { 
        get { return Mathf.Min(m_progress, m_progressTotal); }
        private set
        {
            m_progress = value;
            if(m_progressHandler != null)
            {
                m_progressHandler.Invoke();
            }
        }
    }

    public float progressTotal { get { return m_progressTotal; } }
    public float progressPercent { get { return progress / m_progressTotal; } }

    void InitTotalProgress(PrefabEntitySettings[] prefabsEntity)
    {
        progress = 0;
        m_progressTotal = 0;
        for (int i=0;i< prefabsEntity.Length;i++)
        {
            Transform[] ts = prefabsEntity[i].prefab.transform.GetComponentsInChildren<Transform>();
            m_progressTotal += ts != null ? ts.Length : 0;

            AnimationClip[] clips = prefabsEntity[i].prefab.GetComponent<Animator>().runtimeAnimatorController.animationClips;
            for(int j=0;j<clips.Length;j++)
            {
                m_progressTotal += clips[j].averageDuration * clips[j].frameRate;
            }
        }
    }

    public async void Bake(GPUAnimatorSetting gpuAnimatorSetting, Action progressHandler)
    {
        

        m_progressHandler = progressHandler;
        PrefabEntitySettings[] prefabsEntity = gpuAnimatorSetting.prefabs;

        InitTotalProgress(prefabsEntity);

        m_cancellationTokenBake = new CancellationTokenSource();

        m_gpuAnimationData = null;

        m_gpusprites.Clear();
        m_entities = new Entity[prefabsEntity.Length];
        m_nodes.Clear();
        m_clips.Clear();
        m_frames.Clear();
        m_textures.Clear();


        m_frameRects.Clear();
        m_clipRects.Clear();
        m_entityRects.Clear();

        m_sprites.Clear();
        m_prefabEntities.Clear();

        //m_curves.Clear();
        //m_keyFrames.Clear();
        //m_spritesOrder.Clear();

        //SetSpacesInfo(prefabsEntity[0].prefab);

        for (m_entityI = 0; m_entityI < prefabsEntity.Length; m_entityI++)
        {
            m_bindPaths.Clear();
            m_mapPathSprite.Clear();
            m_mapPathTransform.Clear();

            Vector2 entityMin = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 entityMax = new Vector2(float.MinValue, float.MinValue);

            m_nodesStartI = m_nodes.Count;
            //ProcesTransform(prefabsEntity[m_entityI].prefab.transform, m_nodes.Count, m_nodes.Count, "");

            await ProcesTransform(prefabsEntity[m_entityI].prefab.transform);

            m_entities[m_entityI] = new Entity
            {
                nodesStartI = m_nodesStartI,
                nodesC = m_nodes.Count - m_nodesStartI,
                clipsStartI = m_clips.Count
            };

            m_prefabEntities.Add(
                new PrefabEntity
                {
                    prefab = prefabsEntity[m_entityI].prefab,
                    entityI = m_entityI
                }
            );

            AnimationClip[] clips = prefabsEntity[m_entityI].prefab.GetComponent<Animator>().runtimeAnimatorController.animationClips;
            
            for (int clipI = 0; clipI < clips.Length; clipI++)
            {
                SetClipBindings(clips[clipI]);
                await BakeClip(clips[clipI]);

                Vector4 clipRect = m_clipRects[m_clipRects.Count - 1];
                entityMin.x = entityMin.x < clipRect.x ? entityMin.x : clipRect.x;
                entityMin.y = entityMin.y < clipRect.y ? entityMin.y : clipRect.y;
                entityMax.x = entityMax.x > clipRect.x + clipRect.z ? entityMax.x : clipRect.x + clipRect.z;
                entityMax.y = entityMax.y > clipRect.y + clipRect.w ? entityMax.y : clipRect.y + clipRect.w;

            }
            m_entityRects.Add(new Vector4(entityMin.x, entityMin.y, entityMax.x - entityMin.x, entityMax.y - entityMin.y));
        }

        m_gpuAnimationData = ScriptableObject.CreateInstance<GPUAnimationData>();
        //m_gpuAnimationData.m_sprites = m_gpusprites.ToArray();
        m_gpuAnimationData.m_entities = m_entities;
        m_gpuAnimationData.m_nodes = m_nodes.ToArray();
        m_gpuAnimationData.m_clips = m_clips.ToArray();
        m_gpuAnimationData.m_frames = m_frames.ToArray();
        m_gpuAnimationData.m_prefabEntities = m_prefabEntities.ToArray();
        //m_gpuAnimationData.m_texture = m_textures;
        m_gpuAnimationData.m_frameRects = m_frameRects.ToArray();
        m_gpuAnimationData.m_clipRects = m_clipRects.ToArray();
        m_gpuAnimationData.m_entityRects = m_entityRects.ToArray();
        m_gpuAnimationData.m_computerSpriteAnimation = Resources.Load<ComputeShader>("SpritesAnimation");
        m_gpuAnimationData.m_spritesAnimationMaterial = Resources.Load<Material>("GPUSprites_SpritesAnimation");
        //m_gpuAnimationData.m_spritesOrder = m_spritesOrder;

        await BakeStaticSprites(gpuAnimatorSetting.sprites);

        var texArray = await SetTexturesArray();
        SaveTextureArray(texArray);

        Cancel();
        Debug.Log(m_prefabEntities.Count);
        Debug.Log("Bake : End");
    }

    async Task BakeStaticSprites(Sprite[] sprites)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            AddSprite(sprites[i]);
            if (i % 100 == 0)
                await Task.Yield();
        }
    }

    int GetTextturesSize2()
    {
        int size = 0;
        for (int i = 0; i < m_textures.Count; i++)
        {
            size = (int)MathF.Max(m_textures[i].height, MathF.Max(m_textures[i].width, size));
        }
        return UtilsComputeShader.NextPowerOf2(size);
    }

    async Task<Texture2DArray> SetTexturesArray()
    {
        Debug.Log("$$$$$$$$$$$$$$ SetTexturesArray m_textures.Count = " + m_textures.Count);

        int textureSize = GetTextturesSize2();

        var texArray = new Texture2DArray(textureSize, textureSize, m_textures.Count, TextureFormat.RGBA32, false);
        var tex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32,false);
        List<Vector2> m_textSizeArray = new List<Vector2>();

        for (int i = 0; i < m_textures.Count; i++)
        {
            for(int x = 0;x< textureSize; x++)
            {
                for (int y = 0; y < textureSize; y++)
                {
                    tex.SetPixel(x,y, new Color(0, 0, 0, 0));
                }
            }
            await Task.Yield();
            tex.SetPixels(0, 0, m_textures[i].width, m_textures[i].height, m_textures[i].GetPixels());

            texArray.SetPixels32(tex.GetPixels32(), i);
            m_textSizeArray.Add(new Vector2(m_textures[i].width, m_textures[i].height));
        }

        for(int i=0;i< m_gpusprites.Count;i++)
        {
            GPUSprite sprite = m_gpusprites[i];
            float kx = m_textures[sprite.textureI].width / (float)textureSize;
            float ky = m_textures[sprite.textureI].height / (float)textureSize;
            sprite.uv = new Vector2(sprite.uv.x * kx, sprite.uv.y * ky);
            sprite.sizeuv = new Vector2(sprite.sizeuv.x * kx, sprite.sizeuv.y * ky);
            m_gpusprites[i] = sprite;

            if(i%500 == 0)
                await Task.Yield();
        }

        m_gpuAnimationData.m_srcTextures = m_textures.ToArray();
        m_gpuAnimationData.m_sprites = m_gpusprites.ToArray();
        m_gpuAnimationData.m_texturesSize = textureSize;

        return texArray;
    }

    void SaveTextureArray(Texture2DArray texArray)
    {
        string gpuAnimationDataPath = AssetDatabase.GetAssetPath(m_gpuAnimationData);
        AssetDatabase.AddObjectToAsset(texArray, gpuAnimationDataPath);
        m_gpuAnimationData.m_textures = texArray;
        AssetDatabase.SaveAssets();
    }

    int TryAddTexture(Sprite sprite)
    {
        Texture2D tex = UnityEditor.Sprites.SpriteUtility.GetSpriteTexture(sprite, true);
        int i = m_textures.IndexOf(tex);
        if (i == -1)
        {
            m_textures.Add(tex);
            i = m_textures.Count - 1;
        }
        return i;
    }

    /*
    void SetSpacesInfo(GameObject prefab)
    {
        foreach(Transform t in prefab.transform)
        {
            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            if(sr != null )
            {
                Sprite sprite = sr.sprite;
                m_textures = UnityEditor.Sprites.SpriteUtility.GetSpriteTexture(sprite, true);
                m_textureSize = new Vector2Int(m_textures.width, m_textures.height);
                m_UV2WS = new Vector2(m_textures.width / sprite.pixelsPerUnit, m_textures.height / sprite.pixelsPerUnit);
            }
        }
    }
    */
    
    public async void ProcesTransforms(Transform t, int rootIndex, int parentIndex, string path)
    {
        Node node = new Node
        {
            entityI = m_entityI,
            parentI = parentIndex,
            order = m_nodes.Count - rootIndex
        };

        string newPath = path + "/" + t.name;
        m_bindPaths.Add(newPath);
        var renderer = t.GetComponent<SpriteRenderer>();
        m_mapPathSprite.Add(newPath, renderer!= null? renderer.sprite:null);
        if(renderer != null)
        {
            m_sprites.Add(renderer.sprite);
        }

        m_mapPathTransform.Add(newPath, t);

        m_nodes.Add(node);

        foreach(Transform tr in t)
        {
            ProcesTransforms(tr, rootIndex, parentIndex + 1, newPath);
            await Task.Yield();
        }
    }
    

    void AddLastPath(List<Transform> b)
    {
        m_stringBuilder.Clear();
        for(int i=0;i<b.Count;i++)
        {
            if(i > 0)
            {
                m_stringBuilder.Append('/');
            }
            m_stringBuilder.Append(b[i].name);
        }
        m_bindPaths.Add(m_stringBuilder.ToString());
    }

    void AddLastNode(List<Transform> b)
    {
        /*var parentIndex = -1;
        if(b.Count > 1)
        {
            parentIndex = m_nodesTransforms.IndexOf(b[b.Count - 2]);
        }
        */

        Node node = new Node
        {
            entityI = m_entityI,
            //parentOffset = m_nodes.Count - parentIndex,
            //order = m_nodes.Count - m_nodesStartI
        };
        m_entityNodes.Add(node);
        Transform t = b[b.Count - 1];
        m_nodesTransforms.Add(t);


        var renderer = t.GetComponent<SpriteRenderer>();
        m_mapPathSprite.Add(m_bindPaths[m_bindPaths.Count - 1], renderer != null ? renderer.sprite : null);
        if (renderer != null)
        {
            AddSprite(renderer.sprite);
            //m_sprites.Add(renderer.sprite);
        }

        m_mapPathTransform.Add(m_bindPaths[m_bindPaths.Count - 1], t);

        progress++;
    }

    public void Cancel()
    {
        if (m_cancellationTokenBake != null)
        {
            m_cancellationTokenBake.Cancel();
            m_cancellationTokenBake.Dispose();
            m_cancellationTokenBake = null;
        }

        CancelInternalToken();
    }

    void CancelInternalToken()
    {
        if (m_cancellationTokenInternal != null)
        {
            m_cancellationTokenInternal.Cancel();
            m_cancellationTokenInternal.Dispose();
            m_cancellationTokenInternal = null;
        }
    }

    public async Task ProcesTransform(Transform t)
    {
        m_nodesTransforms.Clear();
        m_entityNodes.Clear();
        m_cancellationTokenInternal = new CancellationTokenSource();


        List<Transform> a = new List<Transform>();
        List<Transform> b = new List<Transform>();
        a.Add(t.GetChild(0));
        while(a.Count > 0 || b.Count > 0)
        {
            if(a.Count > b.Count)
            {
                t = a[a.Count - 1];
                if (t != null)
                {
                    b.Add(t);
                    if(t.childCount > 0)
                    {
                        a[a.Count - 1] = null;
                        //a.RemoveAt(a.Count - 1);
                        var trans = t.GetChildren(); 
                        a.AddRange(trans);
                    }
                    else
                    {
                        a.RemoveAt(a.Count - 1);
                        AddLastPath(b);
                        AddLastNode(b);
                        b.RemoveAt(b.Count - 1);
                    }
                }
                else
                {
                    a.RemoveAt(a.Count - 1);
                    if (b.Count > 0)
                    {
                        AddLastPath(b);
                        AddLastNode(b);
                        b.RemoveAt(b.Count - 1);
                    }
                }
            }
            else
            {
                AddLastPath(b);
                AddLastNode(b);
                b.RemoveAt(b.Count - 1);
            }
            await Task.Yield();
        }

        //m_bindPaths.Reverse();
        //m_nodes.Reverse();
        //m_nodesTransforms.Reverse();

        for (int i = 0; i< m_entityNodes.Count;i++)
        {
            t = m_nodesTransforms[i];
            int parentI = m_nodesTransforms.IndexOf(t.parent);
            var node = m_entityNodes[i];
            m_entityNodes[i] = node;
        }

        //********************** Start Sort Nodes ***********************
        Dictionary<Transform, Node> mapTransformNode = new Dictionary<Transform, Node>();
        Dictionary<Transform, string> mapTransformPath = new Dictionary<Transform, string>();

        for (int i = 0; i < m_nodesTransforms.Count; i++)
        {
            mapTransformNode.Add(m_nodesTransforms[i], m_entityNodes[i]);
            mapTransformPath.Add(m_nodesTransforms[i], m_bindPaths[i]);
        }

        m_nodesTransforms.Sort((n0,n1) =>
        {
            SpriteRenderer r0 = n0.GetComponent<SpriteRenderer>();
            SpriteRenderer r1 = n1.GetComponent<SpriteRenderer>();
            if (r0.sortingOrder < r1.sortingOrder)
                return -1;
            else if (r0.sortingOrder > r1.sortingOrder)
                return 1;
            return 0;
        });


        m_bindPaths.Clear();
        for (int i = 0; i < m_nodesTransforms.Count; i++)
        {
            t = m_nodesTransforms[i];
            Node node = mapTransformNode[t];
            node.order = i;
            node.parentI = m_nodesTransforms.IndexOf(t.parent);
            m_nodes.Add(node);
            //m_spritesOrder.Add(t.GetComponent<SpriteRenderer>().sprite);
            AddSprite(t.GetComponent<SpriteRenderer>().sprite);
            m_bindPaths.Add(mapTransformPath[t]);
        }
        //********************** End Sort Nodes ***********************
        CancelInternalToken();

        Debug.Log("Bake.ProcesTransform end");
        Debug.Log(string.Join('\n', m_bindPaths));
    }

    public void SetClipBindings(AnimationClip animationClip)
    {
        Debug.Log("************** SetClipBindings");
        if (animationClip == null) return;

        //for (int i = 0; i < animationClips.Length; i++)
        {
            //var animationClip = animationClips[i];

            m_curves.Clear();
            foreach (var binding in AnimationUtility.GetCurveBindings(animationClip))
            {
                List<BindedCurve> list;
                if(!m_curves.TryGetValue(binding.path, out list))
                {
                    list = new List<BindedCurve>();
                    m_curves.Add(binding.path, list);
                }

                var curve = AnimationUtility.GetEditorCurve(animationClip, binding);
                list.Add(new BindedCurve{
                    propertyName = binding.propertyName,
                    curve = curve
                });
            }

            m_keyFrames.Clear();
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(animationClip))
            {
                Debug.Log(binding.path);
                List<BindedKeyframe> list;
                if (!m_keyFrames.TryGetValue(binding.path, out list))
                {
                    list = new List<BindedKeyframe>();
                    m_keyFrames.Add(binding.path, list);
                }
                list.Add(new BindedKeyframe
                { 
                    propertyName = binding.propertyName,
                    curve = AnimationUtility.GetObjectReferenceCurve(animationClip, binding) 
                });
            }
        }
    }

    ObjectReferenceKeyframe GetFrame(ObjectReferenceKeyframe[] frames, float t, int startI = 0)
    {
        for(int i= startI; i<frames.Length;i++)
        {
            if(t >= frames[i].time)
            {
                return frames[i];
            }
        }
        return frames[frames.Length - 1];
    }

    /* Sprite data:
     * uvs:         [left top, right top, left bottom, right bottom]
     *              from UnityEditor.Sprites.SpriteUtility.GetSpriteUVs(sprite, true);
     *              Relative to atlas texture.
     *              
     * vertices:    [left top, right top, left bottom, right bottom]
     *              from Sprite.vertices, in world units
     *              vertices * m_sprite.pixelsPerUnit == rect
     *              pivot == (0,0)
     *              
     * rect:        relative to original texture, in pixels, only use its width and height
     *              from Sprite.rect
     *              x,y = left botom vertex.
     * 
     * pivot        relative to the origin of the sprite rect, in pixels
     *              from Sprite.pivot
     * 
    */

    int AddSprite(Sprite sprite)
    {
        int i = m_sprites.IndexOf(sprite);
        if (i != -1) return i;

        m_sprites.Add(sprite);
        Vector2[] uvs = UnityEditor.Sprites.SpriteUtility.GetSpriteUVs(sprite, true);

        Vector2 sizeuv = new Vector2(Mathf.Abs(uvs[1].x - uvs[2].x), Mathf.Abs(uvs[1].y - uvs[2].y));
        m_gpusprites.Add(new GPUSprite
        {
            textureI = TryAddTexture(sprite),
            uv = uvs[2],
            sizeuv = sizeuv
        });

        /*
        Debug.Log("dedededede  spriteI = " + (m_sprites.Count - 1) + "   " +
            sprite.name + "\n"+ string.Join(",", uvs) + 
            "\nsizeuv = " + sizeuv + 
            "\ntextureSize = "+ m_texture.width + "  " + m_texture.height +
            "\ntextCoord = "+ uvs[2].x * m_texture.width + ","+ uvs[2].y * m_texture.height +
            "\nsizePixels = " + sizeuv.x * m_texture.width + "," + sizeuv.y * m_texture.height
            );
        */

        return m_sprites.Count - 1;
    }

    async Task BakeClip(AnimationClip animationClip)
    {
        m_cancellationTokenInternal = new CancellationTokenSource();

        float frameDuration = 1 / animationClip.frameRate;
        Clip clip = new Clip
        {
            frameStartI = m_frames.Count,
            frameDuration = frameDuration
        };

        Vector2 clipMin = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 clipMax = new Vector2(float.MinValue, float.MinValue);

        Debug.Log("************ BakeClip:  "+ animationClip.name + "   frameDuration = " + frameDuration+ "   m_bindPaths.Count = "+ m_bindPaths.Count);
        int framesCount = 0;
        Dictionary<string, SpriteFrame> spriteFrames = new Dictionary<string, SpriteFrame>();
        for (float time = 0; time < animationClip.length; time += frameDuration)
        {
            for (int i = 0; i < m_bindPaths.Count; i++)
            {
                //Frame frame = new Frame();
                string bindPath = m_bindPaths[i];
                var spriteFrame = new SpriteFrame(m_mapPathSprite[bindPath], m_mapPathTransform[bindPath]);
                List <BindedCurve> list;
                if (m_curves.TryGetValue(bindPath, out list))
                {
                    for (int j=0;j< list.Count; j++)
                    {
                        var curve = list[j];
                        spriteFrame.Set(curve.propertyName, curve.curve.Evaluate(time));
                    }
                }

                ObjectReferenceKeyframe referenceKeyFrame;
                List<BindedKeyframe> listFrames = new List<BindedKeyframe>();
                //Debug.Log(m_bindPaths[i] + "   " + m_keyFrames.ContainsKey(m_bindPaths[i]));
                if (m_keyFrames.TryGetValue(bindPath, out listFrames))
                {
                    Sprite sprite;
                    for (int j = 0; j < listFrames.Count; j++)
                    {
                        var referenceKeyFrames = listFrames[j].curve;
                        referenceKeyFrame = GetFrame(referenceKeyFrames, time);
                        sprite = (Sprite)referenceKeyFrame.value;
                        spriteFrame.spriteI = AddSprite(sprite);
                        spriteFrame.Sprite = sprite;
                        //frame.spriteI = AddSprite(sprite);
                        //frame.transformOS = spriteFrame.FullTransform;
                        //m_frames.Add(frame);
                    }
                }
                else
                {
                    spriteFrame.spriteI = AddSprite(m_mapPathSprite[bindPath]);
                    //frame.spriteI = AddSprite(m_mapPathSprite[bindPath]);
                    //frame.transformOS = spriteFrame.FullTransform;
                    //m_frames.Add(frame);
                }

                //frame.transformOS = spriteFrame.FullTransform;
                spriteFrames.Add(bindPath, spriteFrame);
            }

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < m_bindPaths.Count; i++)
            {

                string path = m_bindPaths[i];
                string originalPath = path;
                Frame frame = new Frame { 
                    spriteI = spriteFrames[path].spriteI
                };

                Matrix3x3 transformOS = spriteFrames[path].FullTransform;
                int k = 0;
                do
                {
                    k = path.LastIndexOf('/');
                    if (k > -1)
                    {
                        path = path.Substring(0, k);
                        //transformOS *= spriteFrames[path].NodeTransform;
                        transformOS = spriteFrames[path].NodeTransform * transformOS;
                    }
                } while (k > -1);

                frame.transformOS = transformOS;

                m_frames.Add(frame);
                for (int j = 0; j < 4; j++)
                {
                    Vector2 p = transformOS * m_spriteVertices[j];
                    min.x = min.x < p.x ? min.x:p.x;
                    min.y = min.y < p.y ? min.y:p.y;
                    max.x = max.x > p.x ? max.x : p.x;
                    max.y = max.y > p.y ? max.y : p.y;
                }
            }

            m_frameRects.Add(new Vector4(min.x, min.y, max.x - min.x, max.y - min.y));

            clipMin.x = clipMin.x < min.x ? clipMin.x : min.x;
            clipMin.y = clipMin.y < min.y ? clipMin.y : min.y;
            clipMax.x = clipMax.x > max.x ? clipMax.x : max.x;
            clipMax.y = clipMax.y > max.y ? clipMax.y : max.y;

            spriteFrames.Clear();

            await Task.Yield();
            progress++;
            framesCount++;
        }

        m_clipRects.Add(new Vector4(clipMin.x, clipMin.y, clipMax.x - clipMin.x, clipMax.y - clipMin.y));

        clip.framesC = framesCount;
        m_clips.Add(clip);

        CancelInternalToken();
    }
}

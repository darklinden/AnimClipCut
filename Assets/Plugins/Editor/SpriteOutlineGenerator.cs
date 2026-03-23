using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using System;
using System.Collections.Generic;
using System.Reflection;

public static class SpriteOutlineGenerator
{
    enum ActionType
    {
        ForceGenerate = 0,
        UpdateIfExisting = 1,
        GenerateIfMissing = 2
    }

    [MenuItem("Assets/Generate Sprites Custom Outlines In Folder/Force Generate")]
    public static void RegenerateOutlinesForSpritesInSelectedFolder_ForceGenerate()
    {
        RegenerateOutlinesForSpritesInSelectedFolder(ActionType.ForceGenerate);
    }

    [MenuItem("Assets/Generate Sprites Custom Outlines In Folder/Update If Existing")]
    public static void RegenerateOutlinesForSpritesInSelectedFolder_UpdateIfExisting()
    {
        RegenerateOutlinesForSpritesInSelectedFolder(ActionType.UpdateIfExisting);
    }

    [MenuItem("Assets/Generate Sprites Custom Outlines In Folder/Generate If Missing")]
    public static void RegenerateOutlinesForSpritesInSelectedFolder_GenerateIfMissing()
    {
        RegenerateOutlinesForSpritesInSelectedFolder(ActionType.GenerateIfMissing);
    }

    static void RegenerateOutlinesForSpritesInSelectedFolder(ActionType actionType)
    {
        var folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogError("Please select a valid folder.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        int processedCount = 0;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture != null)
            {
                GenerateOutlinesForTexture(texture, actionType);
                processedCount++;
            }
        }

        Debug.Log($"Processed {processedCount} Texture2D asset(s) in folder '{folderPath}'.");
    }

    /// <summary>
    /// Generates custom outlines for all sprites in the given texture 
    /// using Unity's internal GenerateOutline method via reflection.
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="actType">0: Force Generate, 1: Update If Existing 2: Generate If Missing</param>
    static void GenerateOutlinesForTexture(Texture2D texture, ActionType actType)
    {
        string path = AssetDatabase.GetAssetPath(texture);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("Could not get TextureImporter.");
            return;
        }

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        var outlineProvider = dataProvider.GetDataProvider<ISpriteOutlineDataProvider>();
        var textureProvider = dataProvider.GetDataProvider<ITextureDataProvider>();
        var spriteRects = dataProvider.GetSpriteRects();

        if (spriteRects == null || outlineProvider == null || textureProvider == null)
        {
            Debug.LogError("Could not get required data providers.");
            return;
        }

        // Get the internal GenerateOutline method via reflection
        var spriteUtilityType = typeof(UnityEditor.Editor).Assembly
            .GetType("UnityEditor.Sprites.SpriteUtility");

        if (spriteUtilityType == null)
        {
            // Try alternative type name
            spriteUtilityType = typeof(UnityEditor.Editor).Assembly
                .GetType("UnityEditor.SpriteUtility");
        }

        MethodInfo generateOutlineMethod = null;
        if (spriteUtilityType != null)
        {
            generateOutlineMethod = spriteUtilityType.GetMethod("GenerateOutline",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        if (generateOutlineMethod == null)
        {
            Debug.LogError("Could not find GenerateOutline method via reflection. Falling back to rect outlines.");
            GenerateRectOutlines(spriteRects, outlineProvider, dataProvider, importer);
            return;
        }

        var readableTexture = textureProvider.GetReadableTexture2D();
        if (readableTexture == null)
        {
            Debug.LogError("Could not get readable texture.");
            return;
        }

        textureProvider.GetTextureActualWidthAndHeight(out int actualWidth, out int actualHeight);
        float scaleX = (float)readableTexture.width / actualWidth;
        float scaleY = (float)readableTexture.height / actualHeight;

        float detail = 0f;
        byte alphaTolerance = 0;

        for (int i = 0; i < spriteRects.Length; i++)
        {
            var spriteRect = spriteRects[i];

            switch (actType)
            {
                case ActionType.UpdateIfExisting:
                    {
                        var existingOutlines = outlineProvider.GetOutlines(spriteRect.spriteID);
                        if (existingOutlines == null || existingOutlines.Count == 0)
                        {
                            Debug.Log($"Skipping sprite {i} as it has no existing outline.");
                            continue;
                        }
                    }
                    break;
                case ActionType.GenerateIfMissing:
                    {
                        var existingOutlines = outlineProvider.GetOutlines(spriteRect.spriteID);
                        if (existingOutlines != null && existingOutlines.Count > 0)
                        {
                            Debug.Log($"Skipping sprite {i} as it already has an outline.");
                            continue;
                        }
                    }
                    break;
                case ActionType.ForceGenerate:
                default:
                    // Always generate
                    break;
            }

            Rect scaledRect = spriteRect.rect;
            scaledRect.xMin *= scaleX;
            scaledRect.xMax *= scaleX;
            scaledRect.yMin *= scaleY;
            scaledRect.yMax *= scaleY;

            // Call: GenerateOutline(Texture2D, Rect, float, byte, bool, out Vector2[][])
            Vector2[][] paths = null;
            var parameters = new object[] { readableTexture, scaledRect, detail, alphaTolerance, true, null };

            try
            {
                generateOutlineMethod.Invoke(null, parameters);
                paths = (Vector2[][])parameters[5];
            }
            catch (Exception e)
            {
                Debug.LogWarning($"GenerateOutline failed for sprite {i}: {e.Message}");
            }

            var outlines = new List<Vector2[]>();
            Rect capRect = new Rect { size = spriteRect.rect.size, center = Vector2.zero };

            if (paths != null && paths.Length > 0)
            {
                foreach (var pp in paths)
                {
                    var points = new Vector2[pp.Length];
                    for (int p = 0; p < pp.Length; p++)
                    {
                        var v = new Vector2(pp[p].x / scaleX, pp[p].y / scaleY);
                        v.x = Mathf.Clamp(v.x, capRect.xMin, capRect.xMax);
                        v.y = Mathf.Clamp(v.y, capRect.yMin, capRect.yMax);
                        points[p] = v;
                    }
                    outlines.Add(points);
                }
            }
            else
            {
                // Fallback: rectangle outline
                Debug.LogWarning($"No outline generated for sprite {i}, using rectangle fallback.");
                Vector2 halfSize = spriteRect.rect.size * 0.5f;
                outlines.Add(new Vector2[]
                {
                    new(-halfSize.x, -halfSize.y),
                    new(-halfSize.x,  halfSize.y),
                    new( halfSize.x,  halfSize.y),
                    new( halfSize.x, -halfSize.y),
                });
            }

            outlineProvider.SetOutlines(spriteRect.spriteID, outlines);
            outlineProvider.SetTessellationDetail(spriteRect.spriteID, detail);
        }

        dataProvider.Apply();
        importer.SaveAndReimport();

        Debug.Log($"Generated custom outlines for {spriteRects.Length} sprite(s) in '{path}'.");
    }

    static void GenerateRectOutlines(SpriteRect[] spriteRects, ISpriteOutlineDataProvider outlineProvider,
        ISpriteEditorDataProvider dataProvider, TextureImporter importer)
    {
        foreach (var spriteRect in spriteRects)
        {
            Vector2 halfSize = spriteRect.rect.size * 0.5f;
            var outlines = new List<Vector2[]>
            {
                new Vector2[]
                {
                    new(-halfSize.x, -halfSize.y),
                    new(-halfSize.x,  halfSize.y),
                    new( halfSize.x,  halfSize.y),
                    new( halfSize.x, -halfSize.y),
                }
            };
            outlineProvider.SetOutlines(spriteRect.spriteID, outlines);
            outlineProvider.SetTessellationDetail(spriteRect.spriteID, 0);
        }
        dataProvider.Apply();
        importer.SaveAndReimport();
    }
}
using System.Collections.Generic;
using System.IO;
using UnityEditor;

public class SortFileAndFoldersUnderFolder
{
    [MenuItem("Assets/Sort Files and Folders")]
    public static void Sort()
    {
        var selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(selectedPath) || !AssetDatabase.IsValidFolder(selectedPath))
        {
            EditorUtility.DisplayDialog("Error", "Please select a valid folder in the Project window.", "OK");
            return;
        }
        SortAssets(selectedPath);
    }

    [MenuItem("Assets/Print File Asset Types")]
    public static void PrintFileAssetTypes()
    {
        var selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        var assetsAtPath = AssetDatabase.LoadAllAssetsAtPath(selectedPath);
        var assetTypes = new HashSet<System.Type>();
        foreach (var asset in assetsAtPath)
        {
            if (asset == null) continue;
            if (assetTypes.Add(asset.GetType()))
            {
                UnityEngine.Debug.Log($"asset {selectedPath} has type: {asset.GetType()}");
            }
        }
    }

    private static bool IsSprite(string assetPath)
    {
        var assetsAtPath = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (var asset in assetsAtPath)
        {
            if (asset is UnityEngine.Sprite || asset is UnityEngine.U2D.SpriteAtlas)
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsTexture(string assetPath)
    {
        var assetsAtPath = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (var asset in assetsAtPath)
        {
            if (asset is UnityEngine.Texture || asset is UnityEngine.Texture2D)
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsSpine(string assetPath, out List<string> spineAssets)
    {
        spineAssets = new List<string>();
        if (!assetPath.EndsWith(".atlas")
            && !assetPath.EndsWith(".atlas.txt")
            && !assetPath.EndsWith(".png")
            && !assetPath.EndsWith(".json")
            && !assetPath.EndsWith(".skel")
            && !assetPath.EndsWith(".skel.bytes"))
        {
            return false;
        }

        var assetName = Path.GetFileNameWithoutExtension(assetPath);
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>($"{Path.GetDirectoryName(assetPath)}/{assetName}.json") != null)
            spineAssets.Add($"{Path.GetDirectoryName(assetPath)}/{assetName}.json");
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>($"{Path.GetDirectoryName(assetPath)}/{assetName}.skel") != null)
            spineAssets.Add($"{Path.GetDirectoryName(assetPath)}/{assetName}.skel");
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>($"{Path.GetDirectoryName(assetPath)}/{assetName}.skel.bytes") != null)
            spineAssets.Add($"{Path.GetDirectoryName(assetPath)}/{assetName}.skel.bytes");
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>($"{Path.GetDirectoryName(assetPath)}/{assetName}.atlas") != null)
            spineAssets.Add($"{Path.GetDirectoryName(assetPath)}/{assetName}.atlas");
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>($"{Path.GetDirectoryName(assetPath)}/{assetName}.atlas.txt") != null)
            spineAssets.Add($"{Path.GetDirectoryName(assetPath)}/{assetName}.atlas.txt");
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>($"{Path.GetDirectoryName(assetPath)}/{assetName}.png") != null)
            spineAssets.Add($"{Path.GetDirectoryName(assetPath)}/{assetName}.png");
        if (spineAssets.Count > 1) return true;

        return false;
    }

    private static readonly Dictionary<System.Type, string> typeToFolderMap = new()
    {
        { typeof(UnityEngine.AudioClip), "Audio" },
        { typeof(UnityEngine.Material), "Materials" },
        { typeof(UnityEngine.Mesh), "Meshes" },

        { typeof(UnityEngine.AnimationClip), "Animations" },
        { typeof(UnityEngine.Animator), "Animations" },
        { typeof(UnityEditor.Animations.AnimatorStateTransition), "Animations" },
        { typeof(UnityEditor.Animations.AnimatorState), "Animations" },
        { typeof(UnityEditor.Animations.AnimatorStateMachine), "Animations" },
        { typeof(UnityEditor.Animations.AnimatorController), "Animations" },

        { typeof(UnityEngine.Font), "Fonts" },
        // Add more types and their corresponding folders as needed
    };

    private static readonly string prefabsFolder = "Prefabs";
    private static readonly string scenesFolder = "Scenes";
    private static readonly string spritesFolder = "Sprites";
    private static readonly string texturesFolder = "Textures";
    private static readonly string fontsFolder = "Fonts";
    private static readonly string spinesFolder = "Spines";
    private static readonly string defaultFolder = "Other";

    private static bool IsFolderEmpty(string folderPath)
    {
        string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });
        foreach (var guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!AssetDatabase.IsValidFolder(assetPath))
            {
                return false; // Found a file, folder is not empty
            }
        }
        return true; // Folder is empty 
    }

    private static void MakeDirRecursive(string dirPath)
    {
        if (!AssetDatabase.IsValidFolder(dirPath))
        {
            var parentDir = Path.GetDirectoryName(dirPath);
            if (!AssetDatabase.IsValidFolder(parentDir))
            {
                MakeDirRecursive(parentDir);
            }
            AssetDatabase.CreateFolder(parentDir, Path.GetFileName(dirPath));
        }
    }

    private static void MoveAssetToFolder(string assetPath, string targetFolderPath)
    {
        var destPath = $"{targetFolderPath}/{Path.GetFileName(assetPath)}";
        if (Path.GetFullPath(assetPath) == Path.GetFullPath(destPath))
        {
            UnityEngine.Debug.Log($"Asset {assetPath} is already in the correct folder {targetFolderPath}.");
            return; // Skip if already in the correct folder
        }

        MakeDirRecursive(targetFolderPath);

        int counter = 0;
        var assetName = Path.GetFileNameWithoutExtension(assetPath);
        string newAssetPath = $"{targetFolderPath}/{assetName}{Path.GetExtension(assetPath)}";
        while (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(newAssetPath) != null)
        {
            counter++;
            var lastAssetPath = newAssetPath;
            newAssetPath = $"{targetFolderPath}/{assetName}_{counter}{Path.GetExtension(assetPath)}";
            UnityEngine.Debug.LogWarning($"Asset already exists at {lastAssetPath}. Try to move to {newAssetPath}.");
        }

        var err = AssetDatabase.MoveAsset(assetPath, newAssetPath);
        if (!string.IsNullOrEmpty(err))
        {
            UnityEngine.Debug.LogError($"Failed to move asset from {assetPath} to {newAssetPath}: {err}");
        }
        else
        {
            UnityEngine.Debug.Log($"Successfully moved asset from {assetPath} to {newAssetPath}");
        }
    }

    private static void MoveFntAndDeps(string assetPath, string targetFolderPath)
    {
        var deps = AssetDatabase.GetDependencies(assetPath, false);
        var depsToMove = new List<string>();
        foreach (var dep in deps)
        {
            depsToMove.Add(dep);
        }
        if (!depsToMove.Contains(assetPath))
        {
            depsToMove.Add(assetPath);
        }

        if (depsToMove.Count > 1)
        {
            // to folder with the same name as the fnt file
            var fntFileName = Path.GetFileNameWithoutExtension(assetPath);
            var targetFolderPathWithName = $"{targetFolderPath}/{fntFileName}";

            if (Path.GetFullPath(assetPath) == Path.GetFullPath(targetFolderPathWithName))
            {
                UnityEngine.Debug.Log($"Asset {assetPath} is already in the correct folder {targetFolderPathWithName}.");
                return; // Skip if already in the correct folder
            }

            var count = 0;
            while (AssetDatabase.IsValidFolder(targetFolderPathWithName))
            {
                count++;
                targetFolderPathWithName = $"{targetFolderPath}/{fntFileName}_{count}";
            }
            MakeDirRecursive(targetFolderPathWithName);

            foreach (var dep in depsToMove)
            {
                MoveAssetToFolder(dep, targetFolderPathWithName);
            }
        }
    }

    private static void SortAssets(string folderPath)
    {
        string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });

        // move files in folder to their respective folders based on type
        foreach (var guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetDatabase.IsValidFolder(assetPath)) continue; // Skip folders

            if (assetPath.EndsWith(".prefab"))
            {
                string targetPath = $"{folderPath}/{prefabsFolder}";
                MakeDirRecursive(targetPath);
                MoveAssetToFolder(assetPath, targetPath);
                continue;
            }

            if (assetPath.EndsWith(".unity"))
            {
                string targetPath = $"{folderPath}/{scenesFolder}";
                MakeDirRecursive(targetPath);
                MoveAssetToFolder(assetPath, targetPath);
                continue;
            }

            if (IsSpine(assetPath, out var spineAssets))
            {
                string targetPath = $"{folderPath}/{spinesFolder}";

                // to folder with the same name as the fnt file
                var fileName = Path.GetFileNameWithoutExtension(assetPath);
                var targetFolderPath = $"{targetPath}/{fileName}";

                if (Path.GetFullPath(Path.GetDirectoryName(assetPath)) == Path.GetFullPath(targetFolderPath))
                {
                    UnityEngine.Debug.LogWarning($"Asset {assetPath} is already in the correct folder {targetFolderPath}.");
                    continue; // Skip if already in the correct folder
                }

                var count = 0;
                while (AssetDatabase.IsValidFolder(targetFolderPath))
                {
                    count++;
                    targetFolderPath = $"{targetPath}/{fileName}_{count}";
                }
                MakeDirRecursive(targetFolderPath);

                foreach (var dep in spineAssets)
                {
                    MoveAssetToFolder(dep, targetFolderPath);
                }
                continue;
            }

            if (assetPath.EndsWith(".spriteatlas") || assetPath.EndsWith(".spriteatlasv2") || IsSprite(assetPath))
            {
                string targetPath = $"{folderPath}/{spritesFolder}";
                MoveAssetToFolder(assetPath, targetPath);
                continue;
            }

            if (assetPath.EndsWith(".ttf") || assetPath.EndsWith(".otf") || assetPath.EndsWith(".fnt") || assetPath.EndsWith(".fontsettings"))
            {
                string targetPath = $"{folderPath}/{fontsFolder}";
                MoveFntAndDeps(assetPath, targetPath);
                continue;
            }

            if (IsTexture(assetPath))
            {
                string targetPath = $"{folderPath}/{texturesFolder}";
                MoveAssetToFolder(assetPath, targetPath);
                continue;
            }

            var assetsAtPath = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assetsAtPath.Length == 0) continue;

            foreach (var asset in assetsAtPath)
            {
                if (asset == null) continue;
                var assetType = asset.GetType();
                if (!typeToFolderMap.TryGetValue(assetType, out string targetFolder))
                {
                    targetFolder = defaultFolder;
                }
                string targetFolderPath = $"{folderPath}/{targetFolder}";

                if (assetPath == targetFolderPath)
                {
                    UnityEngine.Debug.Log($"Asset {assetPath} is already in the correct folder {targetFolderPath}.");
                    break; // Skip if already in the correct folder
                }
                MoveAssetToFolder(assetPath, targetFolderPath);

                break;
            }
        }

        // clean up empty folders
        var subFolders = AssetDatabase.GetSubFolders(folderPath);
        foreach (var subFolder in subFolders)
        {
            if (IsFolderEmpty(subFolder))
            {
                UnityEngine.Debug.Log($"Deleting empty folder: {subFolder}");
                AssetDatabase.DeleteAsset(subFolder);
            }
        }

        // Refresh the AssetDatabase to reflect changes
        AssetDatabase.Refresh();
    }
}
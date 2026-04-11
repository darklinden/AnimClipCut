using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class AnimClipExchangeWhiteSpace
{

	[MenuItem("Assets/AnimationClip Exchange White Space To Underline", false, 64)]
	public static void ExchangeClipPathWhiteSpaceToUnderline()
	{
		List<string> clipPaths = new();
		do
		{
			string[] assetGUIDArray = Selection.assetGUIDs;
			if (assetGUIDArray.Length <= 0)
				break;

			for (int i = 0; i < assetGUIDArray.Length; i++)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(assetGUIDArray[i]);

				if (".anim" == Path.GetExtension(assetPath))
				{
					clipPaths.Add(assetPath);
				}
			}

			clipPaths.Sort(delegate (string a, string b)
			{
				return string.Compare(a, b);
			});

		} while (false);

		if (clipPaths.Count <= 0)
		{
			EditorUtility.DisplayDialog("Error", "Please Select At Least One Animation Clips!", "Ok");
		}
		else
		{

			for (int i = 0; i < clipPaths.Count; i++)
			{
				var sourceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPaths[i]);

				var desClip = new AnimationClip
				{
					legacy = sourceClip.legacy,
					wrapMode = sourceClip.wrapMode,
					frameRate = sourceClip.frameRate
				};

				var bindings = AnimationUtility.GetCurveBindings(sourceClip);
				if (bindings != null && bindings.Length > 0)
				{
					for (int ii = 0; ii < bindings.Length; ++ii)
					{
						var binding = bindings[ii];
						var curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
						var path = binding.path;
						path = path.Replace("(", "_");
						path = path.Replace(")", "_");
						path = path.Replace(' ', '_');
						desClip.SetCurve(path, binding.type, binding.propertyName, curve);
					}
				}

				int stamp = (int)Time.realtimeSinceStartup;
				string savepath = Path.Combine(Path.GetDirectoryName(clipPaths[i]), Path.GetFileNameWithoutExtension(clipPaths[i]) + "___" + stamp + ".anim");

				AssetDatabase.CreateAsset(desClip, savepath);
			}
		}
	}

	[MenuItem("Assets/AnimationClip Exchange Underline To White Space", false, 64)]
	public static void ExchangeClipPathUnderlineToWhiteSpace()
	{
		List<string> clipPaths = new();
		do
		{
			string[] assetGUIDArray = Selection.assetGUIDs;
			if (assetGUIDArray.Length <= 0)
				break;

			for (int i = 0; i < assetGUIDArray.Length; i++)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(assetGUIDArray[i]);

				if (".anim" == Path.GetExtension(assetPath))
				{
					clipPaths.Add(assetPath);
				}
			}

			clipPaths.Sort(delegate (string a, string b)
			{
				return string.Compare(a, b);
			});

		} while (false);

		if (clipPaths.Count <= 0)
		{
			EditorUtility.DisplayDialog("Error", "Please Select At Least One Animation Clips!", "Ok");
		}
		else
		{
			for (int i = 0; i < clipPaths.Count; i++)
			{
				var sourceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPaths[i]);

				var desClip = new AnimationClip
				{
					legacy = sourceClip.legacy,
					wrapMode = sourceClip.wrapMode,
					frameRate = sourceClip.frameRate
				};

				var bindings = AnimationUtility.GetCurveBindings(sourceClip);
				if (bindings != null && bindings.Length > 0)
				{
					for (int ii = 0; ii < bindings.Length; ++ii)
					{
						var binding = bindings[ii];
						var curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
						var path = binding.path;

						var pathSegments = path.Split('/');
						for (int iii = 0; iii < pathSegments.Length; iii++)
						{
							if (pathSegments[iii].ToLower().StartsWith("bip"))
							{
								pathSegments[iii] = pathSegments[iii].Replace("_", " ");
							}
							else if (pathSegments[iii].ToLower().Contains("_mirrored_"))
							{
								var index = pathSegments[iii].ToLower().IndexOf("_mirrored_");
								var mirroredStart = index;
								var mirroredEnd = index + "_mirrored_".Length - 1;
								var newSegment = pathSegments[iii][..mirroredStart] + "(" + pathSegments[iii][(mirroredStart + 1)..mirroredEnd] + ")" + pathSegments[iii][(mirroredEnd + 1)..];
								Debug.Log($"{pathSegments[iii]} => {newSegment}");
								pathSegments[iii] = newSegment;
							}
						}
						path = string.Join("/", pathSegments);

						desClip.SetCurve(path, binding.type, binding.propertyName, curve);
					}
				}

				int stamp = (int)Time.realtimeSinceStartup;
				string savepath = Path.Combine(Path.GetDirectoryName(clipPaths[i]), Path.GetFileNameWithoutExtension(clipPaths[i]) + "   " + stamp + ".anim");

				AssetDatabase.CreateAsset(desClip, savepath);
			}
		}
	}
}
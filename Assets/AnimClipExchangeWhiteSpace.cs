using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class AnimClipExchangeWhiteSpace {

	static string _clipPath = null;
	static float _clipLen = float.MaxValue;
	static float _trimTimeStart = 0;
	static float _trimTimeEnd = float.MaxValue;

	[MenuItem("Assets/AnimationClip Exchange White Space", false, 64)]
	public static void ExchangeClipPathWhiteSpace() {

		List<string> clipPaths = new List<string>();

		do {
			string[] assetGUIDArray = Selection.assetGUIDs;
			if (assetGUIDArray.Length <= 0)
				break;

			for (int i = 0; i < assetGUIDArray.Length; i++) {
				string assetPath = AssetDatabase.GUIDToAssetPath(assetGUIDArray[i]);

				if (".anim" == Path.GetExtension(assetPath)) {
					clipPaths.Add(assetPath);
				}
			}

			clipPaths.Sort(delegate(string a, string b) {
				return string.Compare(a, b);
			});

		} while (false);

		if (clipPaths.Count <= 0) {
			EditorUtility.DisplayDialog("Error", "Please Select At Least One Animation Clips!", "Ok");
		} else {

			for (int i = 0; i < clipPaths.Count; i++) {
				var sourceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPaths[i]);

				var desClip = new AnimationClip();
				desClip.legacy = sourceClip.legacy;
				desClip.wrapMode = sourceClip.wrapMode;
				desClip.frameRate = sourceClip.frameRate;

				var bindings = AnimationUtility.GetCurveBindings(sourceClip);
				if (bindings != null && bindings.Length > 0) {
					for (int ii = 0; ii < bindings.Length; ++ii) {
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
				string savepath = Path.Combine(Path.GetDirectoryName(clipPaths[i]), Path.GetFileNameWithoutExtension(clipPaths[i]) + "_Exchanged_" + stamp + ".anim");

				AssetDatabase.CreateAsset(desClip, savepath);
			}
		}
	}
}
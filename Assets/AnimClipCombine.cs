using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Runtime.CompilerServices;

public class AnimClipCombine {

	class CurveKey {
		public string path;
		public System.Type type;
		public string propertyName;
	}

	struct CurveKeyComparer : IEqualityComparer<CurveKey> {
		public bool Equals(CurveKey x, CurveKey y) {
			return string.Equals(x.path, y.path) && string.Equals(x.propertyName, y.propertyName) && System.Type.Equals(x.type, y.type);
		}

		public int GetHashCode(CurveKey obj) {
			return (obj.path + " --- " + obj.propertyName).GetHashCode();
		}
	}

	[MenuItem("Assets/AnimationClip Combine", false, 64)]
	public static void CombineClipByTimeRange() {

		List<string> clipPaths = new List<string>();

		do {
			string[] assetGUIDArray = Selection.assetGUIDs;
			if (assetGUIDArray.Length <= 1)
				break;

			for (int i = 0; i < assetGUIDArray.Length; i++) {
				string assetPath = AssetDatabase.GUIDToAssetPath(assetGUIDArray[i]);

				if (".anim" == Path.GetExtension(assetPath)) {
					clipPaths.Add(assetPath);
				}
			}

		} while (false);

		if (clipPaths.Count <= 1) {
			EditorUtility.DisplayDialog("Error", "Please Select At Least Two Animation Clips!", "Ok");
		} else {

			AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPaths[0]);

			var desClip = new AnimationClip();
			desClip.legacy = clip.legacy;
			desClip.wrapMode = clip.wrapMode;
			desClip.frameRate = clip.frameRate;

			string desInfo = "";

			Dictionary<CurveKey, List<Keyframe>> dict = new Dictionary<CurveKey, List<Keyframe>>(default(CurveKeyComparer));

			float timeOffset = (float)1 / clip.frameRate;
			for (int i = 0; i < clipPaths.Count; i++) {
				var sourceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPaths[i]);

				desInfo += clipPaths[i] + ", " + timeOffset + ", " + (timeOffset + sourceClip.length) + "\n";

				var bindings = AnimationUtility.GetCurveBindings(sourceClip);
				if (bindings != null && bindings.Length > 0) {
					for (int ii = 0; ii < bindings.Length; ++ii) {
						var binding = bindings[ii];
						var curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
						if (curve == null || curve.keys == null) {
							// Debug.LogWarning(string.Format("AnimationClipCurveData {0} don't have curve; Animation name {1} ", curveDate, animationPath));
							continue;
						}

						var curveKey = new CurveKey {
							path = binding.path,
							type = binding.type,
							propertyName = binding.propertyName
						};

						List<Keyframe> frames;
						if (!dict.TryGetValue(curveKey, out frames)) {
							frames = new List<Keyframe>();
						}

						for (int iii = 0; iii < curve.keys.Length; iii++) {
							var key = curve.keys[iii];
							key.time += timeOffset;
							frames.Add(OptimizeKey(key));
						}

						dict[curveKey] = frames;
					}
				}

				timeOffset += sourceClip.length;
				timeOffset += (float)1 / clip.frameRate;
			}

			foreach (var pair in dict) {
				var curveKey = pair.Key;
				var frames = pair.Value;
				desClip.SetCurve(curveKey.path, curveKey.type, curveKey.propertyName, new AnimationCurve(frames.ToArray()));
			}

			int stamp = (int)Time.realtimeSinceStartup;
			string savepath = clipPaths[0] + "_Combined_" + clipPaths.Count + "_" + stamp + ".anim";
			string infopath = clipPaths[0] + "_Combined_" + clipPaths.Count + "_" + stamp + ".txt";

			StreamWriter writer = new StreamWriter(infopath, true);
			writer.WriteLine(desInfo);
			writer.Close();

			AssetDatabase.CreateAsset(desClip, savepath);
			AssetDatabase.ImportAsset(infopath);
		}
	}

	static Keyframe OptimizeKey(Keyframe key) {
		// Floating point precision compressed to f3
		const string FloatFormatOptimize = "f3";

		var ret = new Keyframe();
		ret.time = key.time;
		ret.value = float.Parse(key.value.ToString(FloatFormatOptimize));
		ret.inTangent = float.Parse(key.inTangent.ToString(FloatFormatOptimize));
		ret.outTangent = float.Parse(key.outTangent.ToString(FloatFormatOptimize));
		ret.inWeight = key.inWeight;
		ret.outWeight = key.outWeight;
		ret.weightedMode = key.weightedMode;
		return ret;
	}
}
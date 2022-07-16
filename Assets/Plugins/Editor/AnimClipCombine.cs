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

			clipPaths.Sort(delegate(string a, string b) {
				return string.Compare(a, b);
			});

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

			float timeOffset = 0;
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
							frames.Add(key);
						}

						dict[curveKey] = frames;
					}
				}

				timeOffset += sourceClip.length;
				timeOffset += 1.0f / clip.frameRate;
			}

			foreach (var pair in dict) {
				var curveKey = pair.Key;
				var frames = pair.Value;
				desClip.SetCurve(curveKey.path, curveKey.type, curveKey.propertyName, new AnimationCurve(frames.ToArray()));
			}

			int stamp = (int)Time.realtimeSinceStartup;

			string remoteClipPath = Path.Combine(Path.GetDirectoryName(clipPaths[0]), Path.GetFileNameWithoutExtension(clipPaths[0]));

			string savepath = remoteClipPath + "_Combined_" + clipPaths.Count + "_" + stamp + ".anim";
			string infopath = remoteClipPath + "_Combined_" + clipPaths.Count + "_" + stamp + ".txt";

			StreamWriter writer = new StreamWriter(infopath, true);
			writer.WriteLine(desInfo);
			writer.Close();

			AssetDatabase.CreateAsset(desClip, savepath);
			AssetDatabase.ImportAsset(infopath);
		}
	}
}
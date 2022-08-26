using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using Newtonsoft.Json;

public class AnimClipCutFbxOfCreator {

	class JSON {
		public class Root {
			public UserData userData { get; set; }
		}

		public class UserData {
			public List<AnimationImportSetting> animationImportSettings { get; set; }
		}

		public class AnimationImportSetting {
			public double duration { get; set; }
			public int fps { get; set; }
			public List<Split> splits { get; set; }
		}

		public class Split {
			public string name { get; set; }
			public double from { get; set; }
			public double to { get; set; }
			public int wrapMode { get; set; }
		}
	}

	[MenuItem("Assets/Fbx Cut AnimationClips", false, 66)]
	public static void CutClipByTimeRange() {

		string clipPath = null;

		do {
			string[] assetGUIDArray = Selection.assetGUIDs;
			if (assetGUIDArray.Length != 1)
				break;

			string assetPath = AssetDatabase.GUIDToAssetPath(assetGUIDArray[0]);

			if (".anim" != Path.GetExtension(assetPath))
				break;

			clipPath = assetPath;
		} while (false);

		if (string.IsNullOrEmpty(clipPath)) {
			EditorUtility.DisplayDialog("Error", "Please Select One Single Animation Clip!", "Ok");
		} else {

			string jsonPath = EditorUtility.OpenFilePanel("Please Select The Meta File Of Creator Fbx:", "", "");
			jsonPath = jsonPath.Replace("\\", "/");

			string json = File.ReadAllText(jsonPath);
			var data = JsonConvert.DeserializeObject<JSON.Root>(json);

			AnimationClip sourceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
			var dir = Path.GetDirectoryName(clipPath);
			var listSettings = data?.userData?.animationImportSettings;
			if (listSettings != null && listSettings.Count > 0) {
				var settings = listSettings[0]?.splits;
				if (settings != null && settings.Count > 0) {
					for (int i = 0; i < settings.Count; i++) {
						var s = settings[i];
						string savepath = Path.Combine(dir, s.name + ".anim");
						CutTheClip(sourceClip, (float)s.from, (float)s.to, savepath);
					}
				}
			}

			Debug.Log("Done");
		}
	}

	static Keyframe ProcessValue(Keyframe before, Keyframe after, float time) {
		var len = after.time - before.time;
		var pass = time - before.time;
		var value = (after.value - before.value) * pass / len + before.value;
		return new Keyframe(time, value);
	}

	static void CutTheClip(AnimationClip sourceClip, float trimTimeStart, float trimTimeEnd, string savepath) {

		if (sourceClip == null) {
			EditorUtility.DisplayDialog("Error", "Open Animation Clip Failed!", "Ok");
			return;
		}

		var desClip = new AnimationClip();
		desClip.legacy = sourceClip.legacy;
		desClip.wrapMode = sourceClip.wrapMode;
		desClip.frameRate = sourceClip.frameRate;

		var bindings = AnimationUtility.GetCurveBindings(sourceClip);
		if (bindings != null && bindings.Length > 0) {

			Dictionary<AnimClipCurveKey, List<Keyframe>> dict = new Dictionary<AnimClipCurveKey, List<Keyframe>>(default(AnimClipCurveKey.Comparer));

			for (int ii = 0; ii < bindings.Length; ++ii) {
				var binding = bindings[ii];
				var curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
				if (curve == null || curve.keys == null) {
					// Debug.LogWarning(string.Format("AnimationClipCurveData {0} don't have curve; Animation name {1} ", curveDate, animationPath));
					continue;
				}

				var curveKey = new AnimClipCurveKey {
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
					frames.Add(key);
				}

				dict[curveKey] = frames;
			}

			foreach (var pair in dict) {
				var curveKey = pair.Key;
				var frames = pair.Value;
				frames.Sort(delegate(Keyframe a, Keyframe b) {
					return a.time - b.time < 0 ? -1 : 1;
				});

				var desframes = new List<Keyframe>();
				Keyframe? last = null;

				if (frames[0].time != 0) {
					last = new Keyframe(0, frames[0].value);
					desframes.Add(last.Value);
				}

				for (int i = 0; i < frames.Count; i++) {
					var key = frames[i];

					if (key.time < trimTimeStart) {
						last = key;
						continue;
					}

					if (key.time >= trimTimeStart) {
						if (desframes.Count == 0) {
							// use last to create the start key
							if (last == null)
								last = key;
							var startKey = ProcessValue(last.Value, key, trimTimeStart);
							desframes.Add(startKey);
						}
					}

					if (desframes.Count > 0) {
						if (key.time >= trimTimeStart && key.time < trimTimeEnd) {
							last = key;
							desframes.Add(key);
							continue;
						}
					}

					if (key.time >= trimTimeEnd) {
						if (last == null)
							last = key;
						var endKey = ProcessValue(last.Value, key, trimTimeEnd);
						desframes.Add(endKey);
						break;
					}
				}

				var timeEnd = Mathf.Min(sourceClip.length, trimTimeEnd);
				if (frames[frames.Count - 1].time != timeEnd) {
					last = new Keyframe(timeEnd, frames[frames.Count - 1].value);
					desframes.Add(last.Value);
				}

				desClip.SetCurve(curveKey.path, curveKey.type, curveKey.propertyName, new AnimationCurve(desframes.ToArray()));
			}

			AssetDatabase.CreateAsset(desClip, savepath);
			EditorUtility.ClearProgressBar();
		}
	}
}
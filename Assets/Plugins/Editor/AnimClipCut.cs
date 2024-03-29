using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class AnimClipCut {

	public class AlertSetTimeRange : EditorWindow {

		void OnGUI() {

			EditorGUI.LabelField(new Rect(10, 10, 480, 20), "AnimationClip: " + _clipPath);

			_trimTimeStart = EditorGUI.FloatField(new Rect(10, 60, 480, 20), "Time Start:(Min: 0)", _trimTimeStart);
			_trimTimeEnd = EditorGUI.FloatField(new Rect(10, 110, 480, 20), "Time End:(Max:" + _clipLen + ")", _trimTimeEnd);

			if (GUI.Button(new Rect(10, 160, 480, 30), "Cut The Clip!")) {

				if (_trimTimeStart >= 0 && _trimTimeEnd <= _clipLen && _trimTimeStart < _trimTimeEnd) {
					AnimClipCut.CutTheClip();

					this.Close();
				} else {
					EditorUtility.DisplayDialog("Error", "Please Select Correct Time Range!", "Ok");
				}
			}
		}
	}

	static string _clipPath = null;
	static AnimationClip _clip = null;
	static float _clipLen = float.MaxValue;
	static float _trimTimeStart = 0;
	static float _trimTimeEnd = float.MaxValue;

	[MenuItem("Assets/AnimationClip Cut By TimeRange", false, 64)]
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

			_clipPath = clipPath;
			_clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
			_trimTimeStart = 0;
			_clipLen = _clip.length;
			_trimTimeEnd = _clipLen;

			// show alert
			AlertSetTimeRange window = EditorWindow.GetWindow<AlertSetTimeRange>();
			window.titleContent = new GUIContent("Cut Animation Clip");
			window.position = new Rect(Screen.width / 2, Screen.height / 2, 500, 300);
			window.ShowModalUtility();
		}
	}

	static Keyframe ProcessValue(Keyframe before, Keyframe after, float time) {
		var len = after.time - before.time;
		var pass = time - before.time;
		var value = (after.value - before.value) * pass / len + before.value;
		return new Keyframe(time, value);
	}

	static void CutTheClip() {
		string savepath = Path.Combine(Path.GetDirectoryName(_clipPath), Path.GetFileNameWithoutExtension(_clipPath) + "_" + _trimTimeStart + "_" + _trimTimeEnd + ".anim");

		CutTheClip(_clip,
		           _trimTimeStart,
		           _trimTimeEnd,
		           savepath);
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

				var timeRangeFrames = new List<Keyframe>();
				Keyframe? last = null;

				if (frames[0].time != 0) {
					last = new Keyframe(0, frames[0].value);
					frames.Insert(0, last.Value);
				}

				for (int i = 0; i < frames.Count; i++) {
					var key = frames[i];

					if (key.time < trimTimeStart) {
						last = key;
						continue;
					}

					if (key.time >= trimTimeStart) {
						if (timeRangeFrames.Count == 0) {
							// use last to create the start key
							if (last == null)
								last = key;
							var startKey = ProcessValue(last.Value, key, trimTimeStart);
							timeRangeFrames.Add(startKey);
						}
					}

					if (timeRangeFrames.Count > 0) {
						if (key.time >= trimTimeStart && key.time < trimTimeEnd) {
							last = key;
							timeRangeFrames.Add(key);
							continue;
						}
					}

					if (key.time >= trimTimeEnd) {
						if (last == null)
							last = key;
						var endKey = ProcessValue(last.Value, key, trimTimeEnd);
						timeRangeFrames.Add(endKey);
						break;
					}
				}

				var timeEnd = Mathf.Min(sourceClip.length, trimTimeEnd);
				if (frames[frames.Count - 1].time != timeEnd) {
					last = new Keyframe(timeEnd, frames[frames.Count - 1].value);
					timeRangeFrames.Add(last.Value);
				}

				var desframes = new List<Keyframe>();
				for (var i = 0; i < timeRangeFrames.Count; i++) {
					var f = timeRangeFrames[i];
					f.time = Mathf.Clamp(f.time - trimTimeStart, 0, trimTimeEnd - trimTimeStart);
					desframes.Add(f);
				}

				desClip.SetCurve(curveKey.path, curveKey.type, curveKey.propertyName, new AnimationCurve(desframes.ToArray()));
			}

			AssetDatabase.CreateAsset(desClip, savepath);
			EditorUtility.ClearProgressBar();
		}
	}
}
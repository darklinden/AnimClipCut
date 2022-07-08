using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class AnimClipReverse {

	[MenuItem("Assets/AnimationClip Reverse", false, 64)]
	public static void ReverseClip() {

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

				int stamp = (int)Time.realtimeSinceStartup;
				string copiedFilePath = Path.Combine(Path.GetDirectoryName(clipPaths[i]), Path.GetFileNameWithoutExtension(clipPaths[i]) + "_Reversed_" + stamp + ".anim");

				AssetDatabase.CopyAsset(clipPaths[i], copiedFilePath);

				var clip = (AnimationClip)AssetDatabase.LoadAssetAtPath(copiedFilePath, typeof(AnimationClip));

				if (clip == null)
					return;
				float clipLength = clip.length;

				EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);

				AnimationClipCurveData[] curves = new AnimationClipCurveData[bindings.Length];

				for (int j = 0; j < curves.Length; j++) {
					curves[j] = new AnimationClipCurveData(bindings[j]);
					curves[j].curve = AnimationUtility.GetEditorCurve(clip, bindings[j]);
				}

				clip.ClearCurves();
				foreach (AnimationClipCurveData curve in curves) {
					var keys = curve.curve.keys;
					int keyCount = keys.Length;
					var postWrapmode = curve.curve.postWrapMode;
					curve.curve.postWrapMode = curve.curve.preWrapMode;
					curve.curve.preWrapMode = postWrapmode;
					for (int j = 0; j < keyCount; j++) {
						Keyframe K = keys[j];
						K.time = clipLength - K.time;
						var tmp = -K.inTangent;
						K.inTangent = -K.outTangent;
						K.outTangent = tmp;
						keys[j] = K;
					}
					curve.curve.keys = keys;
					clip.SetCurve(curve.path, curve.type, curve.propertyName, curve.curve);
				}
				var events = AnimationUtility.GetAnimationEvents(clip);
				if (events.Length > 0) {
					for (int j = 0; j < events.Length; j++) {
						events[j].time = clipLength - events[j].time;
					}
					AnimationUtility.SetAnimationEvents(clip, events);
				}
			}
		}
	}
}

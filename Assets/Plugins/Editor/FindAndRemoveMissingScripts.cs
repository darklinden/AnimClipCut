using UnityEditor;
using UnityEngine;

public static class FindAndRemoveMissingScripts {

	private static int _goCount;
	private static int _missingCount;

	[MenuItem("GameObject/Find And Remove Missing Scripts", false, 160)]
	private static void RemoveMissingScripts() {
		GameObject[] go = UnityEngine.Object.FindObjectsOfType<GameObject>();
		_goCount = 0;
		_missingCount = 0;
		foreach (var g in go) {
			FindInGo(g);
		}

		Debug.Log($"Searched {_goCount} GameObjects, removed {_missingCount} missing scripts");

		AssetDatabase.SaveAssets();
	}

	private static void FindInGo(GameObject g) {
		_goCount++;

		var missing = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g);
		if (missing > 0) {
			var s = g.name;
			var t = g.transform;
			while (t.parent != null) {
				s = t.parent.name + "/" + s;
				t = t.parent;
			}

			Debug.Log($"{s} removed {missing} missing scripts");
			_missingCount += missing;
		}

		foreach (Transform childT in g.transform) {
			FindInGo(childT.gameObject);
		}
	}
}
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

using System.IO;

public class BatchImportAssetPackages {

	[MenuItem("Assets/Import Packages In Folder", false, 80)]
	public static void CreateWizard() {
		string packagePath = EditorUtility.OpenFolderPanel("Export subsprites into what folder?", "", "");
		packagePath = packagePath.Replace("\\", "/") + "/";

		string[] allFilePaths = Directory.GetFiles(Path.GetDirectoryName(packagePath));

		try {
			foreach (string curPath in allFilePaths) {
				string fileToImport = curPath.Replace("\\", "/");
				if (Path.GetExtension(fileToImport).ToLower() == ".unitypackage") {
					Debug.Log("Importing: " + fileToImport);
					AssetDatabase.ImportPackage(fileToImport, false);
				}
			}
		} catch (System.Exception ex) {
			Debug.Log("Error: " + ex.Message);
		}
	}
}
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ClipShowPath))]
public class ClipShowPathEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // draw default inspector
        DrawDefaultInspector();
        // draw button
        ClipShowPath myScript = (ClipShowPath)target;
        if (GUILayout.Button("Refresh Path"))
        {
            myScript.RefreshPath();
        }
    }
}

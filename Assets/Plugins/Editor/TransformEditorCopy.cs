using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(Transform))]
public class TransformEditorCopy : Editor
{
    const float PrefixLabelWidth = 60f;
    static public Editor instance;
    readonly List<Transform> m_TransformList = new();

    private bool extensionBool;

    string axisName = "Local";
    bool isLocal = false;

    private void OnEnable()
    {
        instance = this;

        var editorType = Assembly.GetAssembly(typeof(Editor)).GetTypes().FirstOrDefault(m => m.Name == "TransformInspector");
        instance = CreateEditor(targets, editorType);

        isLocal = EditorPrefs.GetBool("TransformEditorCopy.isLocal");
        extensionBool = EditorPrefs.GetBool("TransformEditorCopy.extensionBool");

        m_TransformList.Clear();
        foreach (var item in targets)
        {
            m_TransformList.Add(item as Transform);
        }
    }

    private void OnDisable()
    {
        EditorPrefs.SetBool("TransformEditorCopy.isLocal", isLocal);
        EditorPrefs.SetBool("TransformEditorCopy.extensionBool", extensionBool);
    }

    public override void OnInspectorGUI()
    {
        instance.OnInspectorGUI();

        // begin detect change
        EditorGUI.BeginChangeCheck();
        var extensionExpand = EditorGUILayout.Foldout(extensionBool, "Copy & Paste");
        if (EditorGUI.EndChangeCheck())
        {
            extensionBool = extensionExpand;
            EditorPrefs.SetBool("TransformEditorCopy.extensionBool", extensionBool);
        }

        if (extensionBool)
        {
            OnTopGUI();

            OnTransformGUI();

            OnPositionGUI();

            OnRotationGUI();

            OnScaleGUI();
        }
    }

    void OnTopGUI()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Name", GUILayout.Width(PrefixLabelWidth));

        // begin detect change
        EditorGUI.BeginChangeCheck();
        var l = GUILayout.Toggle(isLocal, "Use Local");
        if (EditorGUI.EndChangeCheck())
        {
            isLocal = l;
            EditorPrefs.SetBool("TransformEditorCopy.isLocal", isLocal);
        }

        axisName = isLocal ? "Local" : "Global";
        GUILayout.Label("Current Space: " + axisName);
        EditorGUILayout.EndHorizontal();
    }

    void OnTransformGUI()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Transform", GUILayout.Width(PrefixLabelWidth));
        if (GUILayout.Button("Copy"))
        {
            var select = Selection.activeGameObject;
            if (select == null)
                return;
            var tm = select.transform;
            StringBuilder s = new();
            s.Append("Transform_");

            if (isLocal)
            {
                s.Append(FormatVe3(tm.localPosition) + "_");
                s.Append(FormatVe3(tm.localRotation.eulerAngles) + "_");
                s.Append(FormatVe3(tm.localScale));
            }
            else
            {
                s.Append(FormatVe3(tm.position) + "_");
                s.Append(FormatVe3(tm.rotation.eulerAngles) + "_");
                s.Append(FormatVe3(tm.localScale));
            }
            UnityEngine.GUIUtility.systemCopyBuffer = s.ToString();
        }

        if (GUILayout.Button("Paste"))
        {
            string s = UnityEngine.GUIUtility.systemCopyBuffer;
            string[] sArr = s.Split('_');
            if (sArr[0] != "Transform" || s == "")
            {
                Debug.LogError("Data is not Transform Data!");
                return;
            }
            try
            {
                if (isLocal)
                {
                    foreach (var item in m_TransformList)
                    {
                        item.SetLocalPositionAndRotation(ParseV3(sArr[1]), Quaternion.Euler(ParseV3(sArr[2])));
                        item.localScale = ParseV3(sArr[3]);
                    }
                }
                else
                {
                    foreach (var item in m_TransformList)
                    {
                        item.SetPositionAndRotation(ParseV3(sArr[1]), Quaternion.Euler(ParseV3(sArr[2])));
                        item.localScale = ParseV3(sArr[3]);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
                return;
            }
        }
        if (GUILayout.Button("Reset"))
        {
            if (isLocal)
            {
                foreach (var item in m_TransformList)
                {
                    item.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    item.localScale = Vector3.one;
                }
            }
            else
            {
                foreach (var item in m_TransformList)
                {
                    item.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    item.localScale = Vector3.one;
                }
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    void OnPositionGUI()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Position", GUILayout.Width(PrefixLabelWidth));
        if (GUILayout.Button("Copy"))
        {
            var select = Selection.activeGameObject;
            if (select == null)
                return;
            var tm = select.transform;
            StringBuilder s = new();
            if (isLocal)
            {
                s.Append(FormatVe3(tm.localPosition));
            }
            else
            {
                s.Append(FormatVe3(tm.position));
            }
            UnityEngine.GUIUtility.systemCopyBuffer = s.ToString();
            // Debug.Log(s);
        }
        if (GUILayout.Button("Paste"))
        {
            string s = UnityEngine.GUIUtility.systemCopyBuffer;
            if (s == "")
            {
                Debug.LogError("Data is not Position Data!");
                return;
            }
            try
            {
                if (isLocal)
                {
                    foreach (var item in m_TransformList)
                    {
                        item.localPosition = ParseV3(s);
                    }
                }
                else
                {
                    foreach (var item in m_TransformList)
                    {
                        item.position = ParseV3(s);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
                return;
            }
        }
        if (GUILayout.Button("Reset"))
        {
            if (isLocal)
            {
                foreach (var item in m_TransformList)
                {
                    item.localPosition = Vector3.zero;
                }
            }
            else
            {
                foreach (var item in m_TransformList)
                {
                    item.position = Vector3.zero;
                }
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    void OnRotationGUI()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Rotation", GUILayout.Width(PrefixLabelWidth));
        if (GUILayout.Button("Copy"))
        {
            var select = Selection.activeGameObject;
            if (select == null)
                return;
            var tm = select.transform;
            StringBuilder s = new();
            if (isLocal)
            {
                s.Append(FormatVe3(tm.localRotation.eulerAngles));
            }
            else
            {
                s.Append(FormatVe3(tm.rotation.eulerAngles));
            }
            UnityEngine.GUIUtility.systemCopyBuffer = s.ToString();
        }
        if (GUILayout.Button("Paste"))
        {
            string s = UnityEngine.GUIUtility.systemCopyBuffer;
            if (s == "")
            {
                Debug.LogError("Data is not Rotation Data!");
                return;
            }
            try
            {
                if (isLocal)
                {
                    foreach (var item in m_TransformList)
                    {
                        item.localRotation = Quaternion.Euler(ParseV3(s));
                    }
                }
                else
                {
                    foreach (var item in m_TransformList)
                    {
                        item.rotation = Quaternion.Euler(ParseV3(s));
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
                return;
            }
        }
        if (GUILayout.Button("Reset"))
        {
            if (isLocal)
            {
                foreach (var item in m_TransformList)
                {
                    item.localRotation = Quaternion.identity;
                }
            }
            else
            {
                foreach (var item in m_TransformList)
                {
                    item.rotation = Quaternion.identity;
                }
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    void OnScaleGUI()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Scale", GUILayout.Width(PrefixLabelWidth));
        if (GUILayout.Button("Copy"))
        {
            var select = Selection.activeGameObject;
            if (select == null)
                return;
            var tm = select.transform;
            StringBuilder s = new();
            s.Append(FormatVe3(tm.localScale));
            UnityEngine.GUIUtility.systemCopyBuffer = s.ToString();
        }
        if (GUILayout.Button("Paste"))
        {
            string s = UnityEngine.GUIUtility.systemCopyBuffer;
            if (s == "")
            {
                Debug.LogError("Data is not Scale Data!");
                return;
            }
            try
            {
                foreach (var item in m_TransformList)
                {
                    item.localScale = ParseV3(s);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
                return;
            }
        }
        if (GUILayout.Button("Reset"))
        {
            foreach (var item in m_TransformList)
            {
                item.localScale = Vector3.one;
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    // x,y,z
    Vector3 ParseV3(string strVector3)
    {
        string[] s = strVector3.Split(',');
        return new Vector3(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
    }

    // x,y,z
    string FormatVe3(Vector3 ve3)
    {
        return ve3.x + "," + ve3.y + "," + ve3.z;
    }
}

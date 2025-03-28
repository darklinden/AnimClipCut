using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(RectTransform))]
public class RectTransformEditorCopy : Editor
{
    const float PrefixLabelWidth = 90f;
    static public Editor instance;
    readonly List<RectTransform> m_TransformList = new();

    private bool extensionBool;

    string axisName = "Local";
    bool isLocal = false;

    private void OnEnable()
    {
        instance = this;

        var editorType = Assembly.GetAssembly(typeof(Editor)).GetType("UnityEditor.RectTransformEditor");
        if (editorType == null)
        {
            Debug.LogError("Can't find RectTransformEditor");
            return;
        }

        instance = CreateEditor(targets, editorType);
        isLocal = EditorPrefs.GetBool("RectTransformEditorCopy.isLocal");
        extensionBool = EditorPrefs.GetBool("RectTransformEditorCopy.extensionBool");

        m_TransformList.Clear();
        foreach (var item in targets)
        {
            m_TransformList.Add(item as RectTransform);
        }
    }

    private void OnDisable()
    {
        EditorPrefs.SetBool("RectTransformEditorCopy.isLocal", isLocal);
        EditorPrefs.SetBool("RectTransformEditorCopy.extensionBool", extensionBool);
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
            EditorPrefs.SetBool("RectTransformEditorCopy.extensionBool", extensionBool);
        }

        if (extensionBool)
        {
            OnTopGUI();

            OnTransformGUI();

            OnAnchorGUI();

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
            EditorPrefs.SetBool("RectTransformEditorCopy.isLocal", isLocal);
        }

        axisName = isLocal ? "Local" : "Global";
        GUILayout.Label("Current Space: " + axisName);
        EditorGUILayout.EndHorizontal();
    }

    void OnTransformGUI()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("RectTransform", GUILayout.Width(PrefixLabelWidth));
        if (GUILayout.Button("Copy"))
        {
            var select = Selection.activeGameObject;
            if (select == null)
                return;
            var rt = select.GetComponent<RectTransform>();
            StringBuilder s = new();

            s.Append("RectTransform_");
            s.Append(FormatVe2(rt.anchorMin));
            s.Append("|");
            s.Append(FormatVe2(rt.anchorMax));
            s.Append("|");
            s.Append(FormatVe2(rt.pivot));
            s.Append("|");
            s.Append(FormatVe2(rt.sizeDelta));
            s.Append("_");
            if (isLocal)
            {
                s.Append(FormatVe3(rt.localPosition) + "_");
                s.Append(FormatVe3(rt.localRotation.eulerAngles) + "_");
                s.Append(FormatVe3(rt.localScale));
            }
            else
            {
                s.Append(FormatVe3(rt.position) + "_");
                s.Append(FormatVe3(rt.rotation.eulerAngles) + "_");
                s.Append(FormatVe3(rt.localScale));
            }
            UnityEngine.GUIUtility.systemCopyBuffer = s.ToString();
        }
        if (GUILayout.Button("Paste"))
        {
            string s = UnityEngine.GUIUtility.systemCopyBuffer;
            string[] sArr = s.Split('_');
            if (sArr[0] != "RectTransform" || s == "")
            {
                Debug.LogError("Data is not RectTransform!");
                return;
            }
            try
            {
                foreach (var item in m_TransformList)
                {
                    var anchor = sArr[1].Split('|');
                    item.anchorMin = ParseV2(anchor[0]);
                    item.anchorMax = ParseV2(anchor[1]);
                    item.pivot = ParseV2(anchor[2]);
                    item.sizeDelta = ParseV2(anchor[3]);

                    if (isLocal)
                    {
                        item.localPosition = ParseV3(sArr[2]);
                        item.localRotation = Quaternion.Euler(ParseV3(sArr[3]));
                        item.localScale = ParseV3(sArr[4]);
                    }
                    else
                    {
                        item.position = ParseV3(sArr[2]);
                        item.rotation = Quaternion.Euler(ParseV3(sArr[3]));
                        item.localScale = ParseV3(sArr[4]);
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
            foreach (var item in m_TransformList)
            {
                item.anchorMin = new Vector2(0.5f, 0.5f);
                item.anchorMax = new Vector2(0.5f, 0.5f);
                item.pivot = new Vector2(0.5f, 0.5f);
                item.sizeDelta = Vector2.one * 100;
                if (isLocal)
                {
                    item.localPosition = Vector3.zero;
                    item.localRotation = Quaternion.identity;
                    item.localScale = Vector3.one;
                }
                else
                {
                    item.position = Vector3.zero;
                    item.rotation = Quaternion.identity;
                    item.localScale = Vector3.one;
                }
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    void OnAnchorGUI()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Anchor", GUILayout.Width(PrefixLabelWidth));
        if (GUILayout.Button("Copy"))
        {
            var select = Selection.activeGameObject;
            if (select == null)
                return;
            var rt = select.GetComponent<RectTransform>();
            StringBuilder s = new();
            s.Append(FormatVe2(rt.anchorMin));
            s.Append("|");
            s.Append(FormatVe2(rt.anchorMax));
            s.Append("|");
            s.Append(FormatVe2(rt.pivot));
            s.Append("|");
            s.Append(FormatVe2(rt.sizeDelta));
            UnityEngine.GUIUtility.systemCopyBuffer = s.ToString();
            Debug.Log(s);
        }

        if (GUILayout.Button("Paste"))
        {
            string s = UnityEngine.GUIUtility.systemCopyBuffer;
            if (s == "")
            {
                Debug.LogError("Data is not Anchor!");
                return;
            }
            try
            {
                string[] sArr = s.Split('|');
                foreach (var item in m_TransformList)
                {
                    item.anchorMin = ParseV2(sArr[0]);
                    item.anchorMax = ParseV2(sArr[1]);
                    item.pivot = ParseV2(sArr[2]);
                    item.sizeDelta = ParseV2(sArr[3]);
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
                item.anchorMin = new Vector2(0.5f, 0.5f);
                item.anchorMax = new Vector2(0.5f, 0.5f);
                item.pivot = new Vector2(0.5f, 0.5f);
                item.sizeDelta = Vector2.one * 100;
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
            var rt = select.GetComponent<RectTransform>();
            StringBuilder s = new();
            if (isLocal)
            {
                s.Append(FormatVe3(rt.localPosition));
            }
            else
            {
                s.Append(FormatVe3(rt.position));
            }
            UnityEngine.GUIUtility.systemCopyBuffer = s.ToString();
            Debug.Log(s);
        }
        if (GUILayout.Button("Paste"))
        {
            string s = UnityEngine.GUIUtility.systemCopyBuffer;
            if (s == "")
            {
                Debug.LogError("Data is not Position!");
                return;
            }
            try
            {
                foreach (var item in m_TransformList)
                {
                    if (isLocal)
                    {
                        item.localPosition = ParseV3(s);
                    }
                    else
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
            foreach (var item in m_TransformList)
            {
                if (isLocal)
                    item.localPosition = Vector3.zero;
                else
                    item.position = Vector3.zero;
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
            var rt = select.GetComponent<RectTransform>();
            StringBuilder s = new();
            if (isLocal)
            {
                s.Append(FormatVe3(rt.localRotation.eulerAngles));
            }
            else
            {
                s.Append(FormatVe3(rt.rotation.eulerAngles));
            }
            UnityEngine.GUIUtility.systemCopyBuffer = s.ToString();
        }
        if (GUILayout.Button("Paste"))
        {
            string s = UnityEngine.GUIUtility.systemCopyBuffer;
            if (s == "")
            {
                Debug.LogError("Data is not Rotation!");
                return;
            }
            try
            {
                foreach (var item in m_TransformList)
                {
                    if (isLocal)
                    {
                        item.localRotation = Quaternion.Euler(ParseV3(s));
                    }
                    else
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
            foreach (var item in m_TransformList)
            {
                if (isLocal)
                    item.localRotation = Quaternion.identity;
                else
                    item.rotation = Quaternion.identity;
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
            var rt = select.GetComponent<RectTransform>();
            StringBuilder s = new();
            s.Append(FormatVe3(rt.localScale));
            UnityEngine.GUIUtility.systemCopyBuffer = s.ToString();
        }
        if (GUILayout.Button("Paste"))
        {
            string s = UnityEngine.GUIUtility.systemCopyBuffer;
            if (s == "")
            {
                Debug.LogError("Data is not Scale!");
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

    Vector2 ParseV2(string strVector2)
    {
        string[] s = strVector2.Split(',');
        return new Vector2(float.Parse(s[0]), float.Parse(s[1]));
    }

    string FormatVe2(Vector2 ve2)
    {
        return ve2.x + "," + ve2.y;
    }
}

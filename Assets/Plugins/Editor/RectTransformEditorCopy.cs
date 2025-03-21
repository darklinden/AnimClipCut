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
    RectTransform m_Transform;

    private bool extensionBool;

    string axisName = "Local";
    bool isLocal = false;

    private void OnEnable()
    {
        instance = this;

        var editorType = Assembly.GetAssembly(typeof(Editor)).GetType("UnityEditor.RectTransformEditor");
        if (editorType == null)
        {
            Debug.LogError("找不到RectTransformInspector类！");
            return;
        }

        instance = CreateEditor(targets, editorType);
        isLocal = EditorPrefs.GetBool("RectTransformEditorCopy.isLocal");
        extensionBool = EditorPrefs.GetBool("RectTransformEditorCopy.extensionBool");

        m_Transform = this.target as RectTransform;

        if (this)
        {
            try
            {
                var so = serializedObject;
            }
            catch { }
        }
    }

    private void OnDisable()
    {
        EditorPrefs.SetBool("isLocal", isLocal);
        EditorPrefs.SetBool("extensionBool", extensionBool);
    }

    public override void OnInspectorGUI()
    {
        instance.OnInspectorGUI();

        // begin detect change
        EditorGUI.BeginChangeCheck();
        var extensionExpand = EditorGUILayout.Foldout(extensionBool, "拓展功能");
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
        GUILayout.Label("当前坐标轴：" + axisName);
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
            StringBuilder s = new StringBuilder();
            s.Append("RectTransform_");
            s.Append(FormatVe2(m_Transform.anchorMin));
            s.Append("|");
            s.Append(FormatVe2(m_Transform.anchorMax));
            s.Append("|");
            s.Append(FormatVe2(m_Transform.pivot));
            s.Append("|");
            s.Append(FormatVe2(m_Transform.sizeDelta));
            s.Append("_");
            if (isLocal)
            {
                s.Append(FormatVe3(m_Transform.localPosition) + "_");
                s.Append(FormatVe3(m_Transform.localRotation.eulerAngles) + "_");
                s.Append(FormatVe3(m_Transform.localScale));
            }
            else
            {
                s.Append(FormatVe3(m_Transform.position) + "_");
                s.Append(FormatVe3(m_Transform.rotation.eulerAngles) + "_");
                s.Append(FormatVe3(m_Transform.localScale));
            }
            UnityEngine.GUIUtility.systemCopyBuffer = s.ToString();
        }
        if (GUILayout.Button("Paste"))
        {
            string s = UnityEngine.GUIUtility.systemCopyBuffer;
            string[] sArr = s.Split('_');
            if (sArr[0] != "RectTransform" || s == "")
            {
                Debug.LogError("未复制RectTransform组件内容！");
                return;
            }
            try
            {
                var anchor = sArr[1].Split('|');
                m_Transform.anchorMin = ParseV2(anchor[0]);
                m_Transform.anchorMax = ParseV2(anchor[1]);
                m_Transform.pivot = ParseV2(anchor[2]);
                m_Transform.sizeDelta = ParseV2(anchor[3]);

                if (isLocal)
                {
                    m_Transform.localPosition = ParseV3(sArr[2]);
                    m_Transform.localRotation = Quaternion.Euler(ParseV3(sArr[3]));
                    m_Transform.localScale = ParseV3(sArr[4]);
                }
                else
                {
                    m_Transform.position = ParseV3(sArr[2]);
                    m_Transform.rotation = Quaternion.Euler(ParseV3(sArr[3]));
                    m_Transform.localScale = ParseV3(sArr[4]);
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
                m_Transform.localPosition = Vector3.zero;
                m_Transform.localRotation = Quaternion.identity;
                m_Transform.localScale = Vector3.one;
            }
            else
            {
                m_Transform.position = Vector3.zero;
                m_Transform.rotation = Quaternion.identity;
                m_Transform.localScale = Vector3.one;
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
            StringBuilder s = new StringBuilder();
            s.Append(FormatVe2(m_Transform.anchorMin));
            s.Append("|");
            s.Append(FormatVe2(m_Transform.anchorMax));
            s.Append("|");
            s.Append(FormatVe2(m_Transform.pivot));
            s.Append("|");
            s.Append(FormatVe2(m_Transform.sizeDelta));
            UnityEngine.GUIUtility.systemCopyBuffer = s.ToString();
            Debug.Log(s);
        }

        if (GUILayout.Button("Paste"))
        {
            string s = UnityEngine.GUIUtility.systemCopyBuffer;
            if (s == "")
            {
                Debug.LogError("未复制Anchor内容！");
                return;
            }
            try
            {
                string[] sArr = s.Split('|');
                m_Transform.anchorMin = ParseV2(sArr[0]);
                m_Transform.anchorMax = ParseV2(sArr[1]);
                m_Transform.pivot = ParseV2(sArr[2]);
                m_Transform.sizeDelta = ParseV2(sArr[3]);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
                return;
            }
        }
        if (GUILayout.Button("Reset"))
        {
            m_Transform.anchorMin = new Vector2(0.5f, 0.5f);
            m_Transform.anchorMax = new Vector2(0.5f, 0.5f);
            m_Transform.pivot = new Vector2(0.5f, 0.5f);
            m_Transform.sizeDelta = Vector2.one * 100;
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
            StringBuilder s = new StringBuilder();
            if (isLocal)
            {
                s.Append(FormatVe3(m_Transform.localPosition));
            }
            else
            {
                s.Append(FormatVe3(m_Transform.position));
            }
            UnityEngine.GUIUtility.systemCopyBuffer = s.ToString();
            Debug.Log(s);
        }
        if (GUILayout.Button("Paste"))
        {
            string s = UnityEngine.GUIUtility.systemCopyBuffer;
            if (s == "")
            {
                Debug.LogError("未复制Position内容！");
                return;
            }
            try
            {
                if (isLocal)
                {
                    m_Transform.localPosition = ParseV3(s);
                }
                else
                {
                    m_Transform.position = ParseV3(s);
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
                m_Transform.localPosition = Vector3.zero;
            else
                m_Transform.position = Vector3.zero;
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
            StringBuilder s = new StringBuilder();
            if (isLocal)
            {
                s.Append(FormatVe3(m_Transform.localRotation.eulerAngles));
            }
            else
            {
                s.Append(FormatVe3(m_Transform.rotation.eulerAngles));
            }
            UnityEngine.GUIUtility.systemCopyBuffer = s.ToString();
        }
        if (GUILayout.Button("Paste"))
        {
            string s = UnityEngine.GUIUtility.systemCopyBuffer;
            if (s == "")
            {
                Debug.LogError("未复制Rotation内容！");
                return;
            }
            try
            {
                if (isLocal)
                {
                    m_Transform.localRotation = Quaternion.Euler(ParseV3(s));
                }
                else
                {
                    m_Transform.rotation = Quaternion.Euler(ParseV3(s));
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
                m_Transform.localRotation = Quaternion.identity;
            else
                m_Transform.rotation = Quaternion.identity;
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
            StringBuilder s = new StringBuilder();
            s.Append(FormatVe3(m_Transform.localScale));
            UnityEngine.GUIUtility.systemCopyBuffer = s.ToString();
        }
        if (GUILayout.Button("Paste"))
        {
            string s = UnityEngine.GUIUtility.systemCopyBuffer;
            if (s == "")
            {
                Debug.LogError("未复制Scale内容！");
                return;
            }
            try
            {
                m_Transform.localScale = ParseV3(s);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
                return;
            }
        }
        if (GUILayout.Button("Reset"))
        {
            m_Transform.localScale = Vector3.one;
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

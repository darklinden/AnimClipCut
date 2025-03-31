using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class AnimationPreviewer : EditorWindow
{
    [MenuItem("Window/Animation/Animation Previewer", false)]
    public static void ShowWindow()
    {
        AnimationPreviewer animationClipViewer = GetWindow<AnimationPreviewer>("Animation Previewer");
        animationClipViewer.position = new Rect(300, 200, 800, 600);
    }

    public List<GameObject> AnimGameObjects = new();

    private readonly Dictionary<GameObject, AnimationClip[]> ClipsDict = new();
    private readonly Dictionary<GameObject, int> ClipsIndexDict = new();
    private readonly Dictionary<GameObject, bool> ClipPlay = new();
    private readonly Dictionary<GameObject, bool> ClipLoop = new();

    private int m_FrameSlider = 0;
    private int maxFrameCount = 0;
    private int frameRate = -1;
    private float speed = 1f;

    private bool loop = false;
    private bool isPlaying = false;
    private bool autoPlay = false;
    private float startTime = 0;
    private float length = 0f;
    private Vector2 scrollPosition = Vector2.zero;

    void OnGUI()
    {
        if (GUILayout.Button("Reload Animations", new[] { GUILayout.Height(20), GUILayout.Width(200) }))
        {
            maxFrameCount = 0;
            m_FrameSlider = 0;
            length = 0f;
            AnimGameObjects.Clear();
            ClipsDict.Clear();
            ClipsIndexDict.Clear();
            ClipPlay.Clear();
            ClipLoop.Clear();
            if (Selection.gameObjects != null && Selection.gameObjects.Length > 0)
            {
                GameObject[] gos = Selection.gameObjects;
                foreach (var go in gos)
                {
                    Animation[] animations = go.GetComponentsInChildren<Animation>(true);
                    if (animations != null && animations.Length > 0)
                    {
                        foreach (var animation in animations)
                        {
                            List<AnimationClip> clips = new List<AnimationClip>();
                            foreach (AnimationState _state in animation)
                            {
                                clips.Add(animation.GetClip(_state.name));
                            }
                            AnimGameObjects.Add(animation.gameObject);
                            ClipsDict.Add(animation.gameObject, clips.ToArray());
                            ClipsIndexDict.Add(animation.gameObject, 0);
                        }
                    }
                    Animator[] animators = go.GetComponentsInChildren<Animator>(true);
                    if (animators != null && animators.Length > 0)
                    {
                        foreach (var animator in animators)
                        {
                            AnimatorController controller = (AnimatorController)animator.runtimeAnimatorController;
                            AnimGameObjects.Add(animator.gameObject);
                            ClipsDict.Add(animator.gameObject, controller.animationClips);
                            ClipsIndexDict.Add(animator.gameObject, 0);
                        }
                    }
                }
            }
        }

        if (ClipsDict.Count == 0) return;

        EditorGUI.BeginChangeCheck();
        // scroll rect
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        // Your GUI elements here

        string preAnimName = string.Empty;
        foreach (var kvp in ClipsDict)
        {
            int selectIndex = ClipsIndexDict[kvp.Key];

            EditorGUILayout.BeginHorizontal();

            var play = !ClipPlay.ContainsKey(kvp.Key) || ClipPlay[kvp.Key];
            play = GUILayout.Toggle(play, "Play", new[] { GUILayout.Height(20), GUILayout.Width(100) });
            ClipPlay[kvp.Key] = play;

            if (play)
            {
                var loop = ClipLoop.ContainsKey(kvp.Key) && ClipLoop[kvp.Key];
                loop = GUILayout.Toggle(loop, "Loop", new[] { GUILayout.Height(20), GUILayout.Width(100) });
                ClipLoop[kvp.Key] = loop;

                EditorGUILayout.LabelField(kvp.Key.name, new[] { GUILayout.Height(20), GUILayout.Width(200) });
                var newSelectIndex = EditorGUILayout.Popup("Clip:", selectIndex, kvp.Value.Select(pkg => pkg.name).ToArray(), new[] { GUILayout.Height(20), GUILayout.Width(400) });
                if (newSelectIndex != selectIndex)
                {
                    selectIndex = newSelectIndex;
                    maxFrameCount = 0;
                    m_FrameSlider = 0;
                }
                AnimationClip clip = kvp.Value[selectIndex];
                var frame = m_FrameSlider;
                var frameTotal = Mathf.RoundToInt(clip.length * clip.frameRate);

                // 取最长的动画长度
                if (frameTotal > maxFrameCount)
                    maxFrameCount = frameTotal;

                if (frameRate == -1)
                {
                    frameRate = (int)clip.frameRate;
                    preAnimName = clip.name;
                }
                else if (frameRate != (int)clip.frameRate)
                {
                    Debug.LogError($"Anim frameRate not same! {preAnimName} {clip.name}");
                    break;
                }
                var desFrame = loop ? frame % frameTotal : frame > frameTotal ? frameTotal : frame;

                EditorGUILayout.LabelField($"{frame}:{desFrame}/{frameTotal}", new[] { GUILayout.Height(20), GUILayout.Width(100) });
                ClipsIndexDict[kvp.Key] = selectIndex;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        m_FrameSlider = EditorGUILayout.IntSlider("Frame", m_FrameSlider, 0, maxFrameCount);
        EditorGUILayout.LabelField($"Frame Rate:{frameRate} 帧:{m_FrameSlider} 秒:{m_FrameSlider / (float)frameRate}", new[] { GUILayout.Height(20), GUILayout.Width(200) });

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        speed = EditorGUILayout.Slider("Speed", speed, 0f, 10f, new[] { GUILayout.Height(20), GUILayout.Width(400) });
        if (GUILayout.Button("Reset", new[] { GUILayout.Height(20), GUILayout.Width(80) }))
        {
            speed = 1f;
        }
        EditorGUILayout.EndHorizontal();
        if (EditorGUI.EndChangeCheck())
        {
            Debug.Log("EditorGUI.EndChangeCheck");
            // autoPlay = false;
            foreach (var kvp in ClipsDict)
            {
                int selectIndex = ClipsIndexDict[kvp.Key];
                AnimationClip clip = kvp.Value[selectIndex];
                float time = m_FrameSlider / clip.frameRate;
                clip.SampleAnimation(kvp.Key, time);
            }
        }
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        loop = GUILayout.Toggle(loop, "Loop", new[] { GUILayout.Height(20), GUILayout.Width(200) });
        if (isPlaying)
        {
            if (GUILayout.Button("Stop", new[] { GUILayout.Height(20), GUILayout.Width(100) }))
            {
                autoPlay = false;
                isPlaying = false;
            }
        }
        else
        {
            if (GUILayout.Button("Play", new[] { GUILayout.Height(20), GUILayout.Width(100) }))
            {
                m_FrameSlider = 0;
                isPlaying = true;
                autoPlay = true;
                startTime = Time.realtimeSinceStartup;
                // Set length to the longest clip length
                length = maxFrameCount / (float)frameRate;
                // Debug.Log("Play startTime:" + startTime);
            }
        }
        EditorGUILayout.EndHorizontal();
        if (autoPlay)
        {
            float diff = (Time.realtimeSinceStartup - startTime) * speed;
            // Debug.Log("diff to " + diff);
            var frame = Mathf.RoundToInt(diff * frameRate);
            if (m_FrameSlider != frame)
            {
                // Debug.Log("frame to " + frame);
                m_FrameSlider = frame;
            }
            foreach (var kvp in ClipsDict)
            {
                int selectIndex = ClipsIndexDict[kvp.Key];
                AnimationClip clip = kvp.Value[selectIndex];
                clip.SampleAnimation(kvp.Key, diff);
                // Debug.Log("autoPlay Sample " + diff);
            }
            if (diff >= length)
            {
                startTime = Time.realtimeSinceStartup;
                if (!loop)
                {
                    autoPlay = false;
                    isPlaying = false;
                }
            }
            Repaint();
        }
    }
}

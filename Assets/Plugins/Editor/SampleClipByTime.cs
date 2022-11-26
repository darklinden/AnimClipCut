using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Common.Tools.Editor
{
    public class SampleClipByTime : EditorWindow
    {
        [UnityEditor.MenuItem("Tools/SampleClipByTime", false)]
        public static void ShowWindow()
        {
            SampleClipByTime animationClipViewer = EditorWindow.GetWindow<SampleClipByTime>("SampleClipByTime");
            animationClipViewer.position = new Rect(300, 200, 800, 600);
        }

        public List<GameObject> AnimGameObjects = new List<GameObject>();

        private Dictionary<GameObject, AnimationClip[]> ClipsDict = new Dictionary<GameObject, AnimationClip[]>();
        private Dictionary<GameObject, int> ClipsIndexDict = new Dictionary<GameObject, int>();
        private Dictionary<GameObject, bool> ClipPlay = new Dictionary<GameObject, bool>();
        private Dictionary<GameObject, bool> ClipLoop = new Dictionary<GameObject, bool>();

        private float m_SliderValue = 0f;
        private float maxLength = 0f;

        void OnGUI()
        {
            if (GUILayout.Button("Use Selected GameObject", EditorStyles.miniButtonLeft, new[] { GUILayout.Height(20), GUILayout.Width(200) }))
            {
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

            if (AnimGameObjects.Count == 0) return;
            if (ClipsIndexDict.Count == 0) return;

            for (var i = 0; i < AnimGameObjects.Count; i++)
            {
                var animObj = AnimGameObjects[i];
                int selectIndex = ClipsIndexDict[animObj];

                EditorGUILayout.BeginHorizontal();

                var play = ClipPlay.ContainsKey(animObj) ? ClipPlay[animObj] : true;
                play = GUILayout.Toggle(play, "Enable");
                ClipPlay[animObj] = play;

                if (play)
                {
                    var loop = ClipLoop.ContainsKey(animObj) ? ClipLoop[animObj] : false;
                    loop = GUILayout.Toggle(loop, "Loop");
                    ClipLoop[animObj] = loop;

                    EditorGUILayout.LabelField(animObj.name, new GUIStyle { alignment = TextAnchor.MiddleLeft }, new[] { GUILayout.Height(20), GUILayout.Width(200) });
                    selectIndex = EditorGUILayout.Popup(selectIndex, ClipsDict[animObj].Select(pkg => pkg.name).ToArray(), new[] { GUILayout.Height(20), GUILayout.Width(400) });
                    AnimationClip clip = ClipsDict[animObj][selectIndex];
                    float time = m_SliderValue;

                    // 取最长的动画长度
                    if (clip.length > maxLength)
                        maxLength = clip.length;
                    var frame = Mathf.RoundToInt(time * clip.frameRate);
                    var frameTotal = Mathf.RoundToInt(clip.length * clip.frameRate);
                    var desFrame = loop ? frame % frameTotal : frame > frameTotal ? frameTotal : frame;

                    EditorGUILayout.LabelField($"{frame} : {desFrame} / {frameTotal}");
                    ClipsIndexDict[animObj] = selectIndex;
                }
                EditorGUILayout.EndHorizontal();
            }

            m_SliderValue = EditorGUILayout.Slider(m_SliderValue, 0f, maxLength);

            EditorGUILayout.Space();

            for (var i = 0; i < AnimGameObjects.Count; i++)
            {
                var animObj = AnimGameObjects[i];

                var play = ClipPlay.ContainsKey(animObj) ? ClipPlay[animObj] : true;
                if (play)
                {
                    int selectIndex = ClipsIndexDict[animObj];
                    AnimationClip clip = ClipsDict[animObj][selectIndex];
                    float time = m_SliderValue;
                    var loop = ClipLoop.ContainsKey(animObj) ? ClipLoop[animObj] : false;
                    time = loop ? time % clip.length : time > clip.length ? clip.length : time;
                    clip.SampleAnimation(animObj, time);
                }
            }
        }
    }
}
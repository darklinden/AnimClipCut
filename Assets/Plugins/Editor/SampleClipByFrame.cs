using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.Animations;

public class SampleClipByFrame : EditorWindow
{
    public GameObject SelectionGo;
    public List<GameObject> AnimGameObjects = new List<GameObject>();
    public List<string> AnimGameObjectNames = new List<string>();
    private Dictionary<GameObject, AnimationClip[]> AnimGameObjectsClips = new Dictionary<GameObject, AnimationClip[]>();
    private List<string> SelectedAnimClipNames = new List<string>();
    private int SelectedAnimGoIndex = 0;
    private int LastSelectedAnimGoIndex = -1;
    private int SelectedAnimClipIndex = 0;
    private int LastSelectedAnimClipIndex = -1;
    private GameObject SelectedAnimGo;
    private AnimationClip SelectedAnimClip;
    private int CurrentFrame = 0;

    [MenuItem("Tools/SampleClipByFrame", false, 2000)]
    static void ShowWindow()
    {
        var window = CreateInstance<SampleClipByFrame>();
        window.Show();
    }

    public void OnGUI()
    {
        var selectionGo = Selection.activeGameObject;
        if (selectionGo == null && SelectionGo == null)
        {
            EditorGUILayout.HelpBox("Please select a GO", MessageType.Info);
            return;
        }

        GUILayout.BeginVertical();

        var btnclicked = (GUILayout.Button("Use Selected GameObject", EditorStyles.miniButtonLeft, new[] { GUILayout.Height(20), GUILayout.Width(200) }));
        var selectedGo = (GameObject)EditorGUILayout.ObjectField(SelectionGo, typeof(GameObject), true);

        var shouldRefresh = btnclicked || (selectionGo != null && selectedGo != SelectionGo);
        if (selectionGo != null && selectedGo != SelectionGo) selectionGo = selectedGo;

        if (shouldRefresh)
        {
            AnimGameObjects.Clear();
            AnimGameObjectNames.Clear();
            AnimGameObjectsClips.Clear();
            SelectedAnimClipNames.Clear();
            SelectedAnimGoIndex = 0;
            SelectedAnimClipIndex = 0;
            CurrentFrame = 0;
            LastSelectedAnimGoIndex = -1;
            LastSelectedAnimClipIndex = -1;

            SelectionGo = selectionGo;

            Animation[] animations = selectionGo.GetComponentsInChildren<Animation>(true);
            if (animations != null && animations.Length > 0)
            {
                foreach (var animation in animations)
                {
                    List<AnimationClip> clips = new List<AnimationClip>();
                    foreach (AnimationState _state in animation)
                    {
                        clips.Add(animation.GetClip(_state.name));
                        Debug.Log(_state.name);
                    }
                    AnimGameObjects.Add(animation.gameObject);
                    AnimGameObjectNames.Add(animation.gameObject.name);
                    AnimGameObjectsClips.Add(animation.gameObject, clips.ToArray());
                }
            }
            Animator[] animators = selectionGo.GetComponentsInChildren<Animator>(true);
            if (animators != null && animators.Length > 0)
            {
                foreach (var animator in animators)
                {
                    var controller = (AnimatorController)animator.runtimeAnimatorController;
                    AnimGameObjects.Add(animator.gameObject);
                    AnimGameObjectNames.Add(animator.gameObject.name);
                    AnimGameObjectsClips.Add(animator.gameObject, controller.animationClips);
                }
            }
        }

        if (SelectionGo != null)
        {
            SelectedAnimGoIndex = EditorGUILayout.Popup("GameObject", SelectedAnimGoIndex, AnimGameObjectNames.ToArray(), new[] { GUILayout.Height(20), GUILayout.Width(400) });

            if (SelectedAnimGoIndex != LastSelectedAnimGoIndex)
            {
                LastSelectedAnimGoIndex = SelectedAnimGoIndex;
                SelectedAnimGo = AnimGameObjects[SelectedAnimGoIndex];
                SelectedAnimClipNames.Clear();
                foreach (var clip in AnimGameObjectsClips[SelectedAnimGo])
                {
                    SelectedAnimClipNames.Add(clip.name);
                }
                SelectedAnimClipIndex = 0;
                LastSelectedAnimClipIndex = -1;
            }

            SelectedAnimClipIndex = EditorGUILayout.Popup("Animations", SelectedAnimClipIndex, SelectedAnimClipNames.ToArray(), new[] { GUILayout.Height(20), GUILayout.Width(400) });

            if (SelectedAnimClipIndex != LastSelectedAnimClipIndex)
            {
                LastSelectedAnimClipIndex = SelectedAnimClipIndex;
                SelectedAnimClip = AnimGameObjectsClips[AnimGameObjects[SelectedAnimGoIndex]][SelectedAnimClipIndex];
                CurrentFrame = 0;
            }

            if (SelectedAnimClip != null)
            {
                int startFrame = 0;
                int stopFrame = Mathf.RoundToInt(SelectedAnimClip.length * SelectedAnimClip.frameRate);
                CurrentFrame = Mathf.RoundToInt(EditorGUILayout.Slider(CurrentFrame, startFrame, stopFrame));

                SelectedAnimClip.SampleAnimation(SelectedAnimGo, CurrentFrame / SelectedAnimClip.frameRate);
            }
        }


        EditorGUILayout.EndVertical();
    }
}
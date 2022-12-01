#define DEBUG_DRAW

using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class ClipShowPath : MonoBehaviour
{
    public Animation ParentAnimation;
    public Animator ParentAnimator;
    public AnimationClip clip;

    public List<Vector3> path = new List<Vector3>();

    public void RefreshPath()
    {
        ParentAnimation = gameObject.GetComponentInParent<Animation>();
        ParentAnimator = gameObject.GetComponentInParent<Animator>();
        if (ParentAnimation == null && ParentAnimator == null)
        {
            Debug.LogError("No Animation or Animator component found in parent");
            return;
        }

        if (ParentAnimation != null)
        {
            clip = ParentAnimation.clip;
        }
        else
        {
            clip = ParentAnimator.runtimeAnimatorController.animationClips[0];
        }

        if (clip == null)
        {
            Debug.LogError("No clip found");
            return;
        }

        path.Clear();

        var mePath = AnimationUtility.CalculateTransformPath(transform, ParentAnimation.transform);

        var bindings = AnimationUtility.GetCurveBindings(clip);
        AnimationCurve xcurve = null;
        AnimationCurve ycurve = null;
        AnimationCurve zcurve = null;
        if (bindings != null && bindings.Length > 0)
        {
            for (int ii = 0; ii < bindings.Length; ++ii)
            {
                var binding = bindings[ii];
                if (binding.path == mePath)
                {
                    if (binding.propertyName == "m_LocalPosition.x")
                    {
                        xcurve = AnimationUtility.GetEditorCurve(clip, binding);
                    }
                    else if (binding.propertyName == "m_LocalPosition.y")
                    {
                        ycurve = AnimationUtility.GetEditorCurve(clip, binding);
                    }
                    else if (binding.propertyName == "m_LocalPosition.z")
                    {
                        zcurve = AnimationUtility.GetEditorCurve(clip, binding);
                    }
                }
            }
        }

        for (float t = 0; t < clip.length; t += 0.03f)
        {
            var x = xcurve.Evaluate(t);
            var y = ycurve.Evaluate(t);
            var z = zcurve.Evaluate(t);
            var pos = new Vector3(x, y, z);

            path.Add(transform.parent.localToWorldMatrix.MultiplyPoint3x4(pos));
        }
    }

    private void OnDrawGizmos()
    {
        // draw clip move path

        if (path.Count > 0)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < path.Count - 1; i++)
            {
                Gizmos.DrawLine(path[i], path[i + 1]);
            }
        }
    }
}
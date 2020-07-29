using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages multiple GrindSplines, generally on a single surface or object, e.g.a ledge
/// </summary>
public class GrindSurface : MonoBehaviour
{
    public List<GrindSpline> Splines = new List<GrindSpline>();
    public GrindSpline.SurfaceTypes SurfaceType;
    public bool IsRound;

    public ColliderGenerationSettings ColliderGenerationSettings = new ColliderGenerationSettings();

    private void OnValidate()
    {
        if (Splines.Count == 0)
        {
            Splines.AddRange(GetComponentsInChildren<GrindSpline>());
        }

        for (var index = Splines.Count - 1; index >= 0; index--)
        {
            var s = Splines[index];

            if (s == null)
            {
                Splines.RemoveAt(index);
            }
        }
    }

    public void DestroySplines()
    {
        foreach (var s in Splines)
        {
            if (s != null)
            {
                foreach (var c in s.GeneratedColliders)
                {
                    if (c != null) 
                        DestroyImmediate(c.gameObject);
                }

                DestroyImmediate(s.gameObject);
            }
        }

        Splines.Clear();
    }
}
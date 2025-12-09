using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SceneDiffResult
{
    public List<long> added = new();
    public List<long> removed = new();
    public List<long> modified = new();
}

public static class SceneDiffEngine
{
    public static SceneDiffResult Compute(ParsedScene a, ParsedScene b)
    {
        SceneDiffResult result = new();

        var idsA = new HashSet<long>(a.objectNames.Keys);
        var idsB = new HashSet<long>(b.objectNames.Keys);

        // Added
        foreach (var id in idsB)
            if (!idsA.Contains(id))
                result.added.Add(id);

        // Removed
        foreach (var id in idsA)
            if (!idsB.Contains(id))
                result.removed.Add(id);

        // Modified
        Debug.Log($"Checking {idsA.Intersect(idsB).Count()} common objects for modifications");
        Debug.Log($"Scene A has {(a.transforms != null ? a.transforms.Count : 0)} transforms, Scene B has {(b.transforms != null ? b.transforms.Count : 0)} transforms");
        
        if (a.objectNames != null)
            Debug.Log("GameObject names in Scene A: " + string.Join(", ", a.objectNames));
        if (a.transforms != null)
            Debug.Log("Transform IDs in Scene A: " + string.Join(", ", a.transforms.Keys));
        if (b.objectNames != null)
            Debug.Log("GameObject names in Scene B: " + string.Join(", ", b.objectNames));
        if (b.transforms != null)
            Debug.Log("Transform IDs in Scene B: " + string.Join(", ", b.transforms.Keys));
        
        foreach (var id in idsA.Intersect(idsB))
        {
            var nameA = a.objectNames[id];
            var nameB = b.objectNames[id];

            bool nameChanged = nameA != nameB;
            bool transformChanged = HasTransformChanged(a, b, id);

            if (nameChanged)
                Debug.Log($"Name changed for ID {id}: {nameA} -> {nameB}");

            if (nameChanged || transformChanged)
                result.modified.Add(id);
        }

        return result;
    }

    private static long FindParent(ParsedScene scene, long child)
    {
        foreach (var kvp in scene.children)
        {
            if (kvp.Value.Contains(child))
                return kvp.Key; // parent ID
        }
        return -1; // means root
    }

    private static bool HasTransformChanged(ParsedScene a, ParsedScene b, long id)
    {
        if (a.transforms == null || b.transforms == null)
        {
            Debug.LogWarning($"Transform dictionary is null. A: {a.transforms == null}, B: {b.transforms == null}");
            return false;
        }

        if (!a.transforms.ContainsKey(id) || !b.transforms.ContainsKey(id))
        {
            Debug.LogWarning($"Transform data missing for ID {id}. A: {a.transforms.ContainsKey(id)}, B: {b.transforms.ContainsKey(id)}");
            return false;
        }

        var ta = a.transforms[id];
        var tb = b.transforms[id];

        if (ta == null || tb == null)
        {
            Debug.LogWarning($"Transform data is null for ID {id}. A: {ta == null}, B: {tb == null}");
            return false;
        }

        bool posChanged = ta.localPosition != tb.localPosition;
        bool rotChanged = ta.localRotation != tb.localRotation;
        bool scaleChanged = ta.localScale != tb.localScale;

        if (posChanged || rotChanged || scaleChanged)
        {
            Debug.Log($"Transform changed for ID {id}:");
            if (posChanged) Debug.Log($"  Position: {ta.localPosition} -> {tb.localPosition}");
            if (rotChanged) Debug.Log($"  Rotation: {ta.localRotation} -> {tb.localRotation}");
            if (scaleChanged) Debug.Log($"  Scale: {ta.localScale} -> {tb.localScale}");
        }

        if (posChanged) return true;
        if (rotChanged) return true;
        if (scaleChanged) return true;

        return false;
    }
}

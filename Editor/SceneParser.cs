using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class TransformData
{
    public Vector3 localPosition;
    public Quaternion localRotation;
    public Vector3 localScale;
}

public class ParsedScene
{
    public Dictionary<long, string> objectNames = new();
    public Dictionary<long, List<long>> children = new();
    public Dictionary<long, TransformData> transforms = new();
    public List<long> roots = new();
}

public static class SceneParser
{
    private static readonly Regex GameObjectRegex =
        new(@"--- !u!1 &(?<id>\d+).*?m_Name:\s(?<name>.+?)\n", RegexOptions.Singleline);

    private static readonly Regex TransformToGameObjectRegex =
        new(@"--- !u!4 &(?<transformId>\d+)[\s\S]*?m_GameObject:\s*\{fileID:\s*(?<goId>\d+)\}", RegexOptions.Multiline);

    private static readonly Regex TransformRegex =
        new(@"--- !u!4 &(?<id>\d+)\s+Transform:.*?$[\s\S]*?m_LocalRotation:\s*\{x:\s*(?<rx>[-\d.e]+),\s*y:\s*(?<ry>[-\d.e]+),\s*z:\s*(?<rz>[-\d.e]+),\s*w:\s*(?<rw>[-\d.e]+)\}[\s\S]*?m_LocalPosition:\s*\{x:\s*(?<px>[-\d.e]+),\s*y:\s*(?<py>[-\d.e]+),\s*z:\s*(?<pz>[-\d.e]+)\}[\s\S]*?m_LocalScale:\s*\{x:\s*(?<sx>[-\d.e]+),\s*y:\s*(?<sy>[-\d.e]+),\s*z:\s*(?<sz>[-\d.e]+)\}", RegexOptions.Multiline);

    private static readonly Regex ChildrenRegex =
        new(@"m_Children:\s*(?<block>(?:\s+-\s\{fileID:.*\})+)", RegexOptions.Singleline);

    private static readonly Regex ChildEntryRegex =
        new(@"\{fileID:\s(?<id>\d+)\}");

    public static ParsedScene Parse(string yaml)
    {
        var result = new ParsedScene();

        var goMatches = GameObjectRegex.Matches(yaml);
        foreach (Match m in goMatches)
        {
            long id = long.Parse(m.Groups["id"].Value);
            string name = m.Groups["name"].Value.Trim();

            result.objectNames[id] = name;
            result.children[id] = new();
        }

        // Build Transform ID -> GameObject ID mapping (from Transform's m_GameObject reference)
        var transformToGo = new Dictionary<long, long>();
        var transformRefMatches = TransformToGameObjectRegex.Matches(yaml);
        Debug.Log($"Found {transformRefMatches.Count} Transform->GameObject mappings");
        foreach (Match m in transformRefMatches)
        {
            long transformId = long.Parse(m.Groups["transformId"].Value);
            long goId = long.Parse(m.Groups["goId"].Value);
            transformToGo[transformId] = goId;
            Debug.Log($"Transform {transformId} -> GameObject {goId}");
        }

        // Parse transform data (indexed by Transform ID)
        var transformDataById = new Dictionary<long, TransformData>();
        var transformMatches = TransformRegex.Matches(yaml);
        Debug.Log($"TransformRegex matched {transformMatches.Count} transforms");
        foreach (Match m in transformMatches)
        {
            long transformId = long.Parse(m.Groups["id"].Value);
            Debug.Log($"Parsed Transform data for ID: {transformId}");
            
            var transformData = new TransformData
            {
                localPosition = new Vector3(
                    float.Parse(m.Groups["px"].Value),
                    float.Parse(m.Groups["py"].Value),
                    float.Parse(m.Groups["pz"].Value)
                ),
                localRotation = new Quaternion(
                    float.Parse(m.Groups["rx"].Value),
                    float.Parse(m.Groups["ry"].Value),
                    float.Parse(m.Groups["rz"].Value),
                    float.Parse(m.Groups["rw"].Value)
                ),
                localScale = new Vector3(
                    float.Parse(m.Groups["sx"].Value),
                    float.Parse(m.Groups["sy"].Value),
                    float.Parse(m.Groups["sz"].Value)
                )
            };
            
            transformDataById[transformId] = transformData;
        }

        // Remap transforms to use GameObject IDs
        foreach (var kvp in transformDataById)
        {
            long transformId = kvp.Key;
            TransformData transformData = kvp.Value;
            
            if (transformToGo.ContainsKey(transformId))
            {
                long goId = transformToGo[transformId];
                result.transforms[goId] = transformData;
            }
        }

        Debug.Log("Parsed Transforms: " + result.transforms.Count);

        // Find children blocks (per GameObject)
        foreach (Match parent in goMatches)
        {
            long id = long.Parse(parent.Groups["id"].Value);
            int startIndex = parent.Index + parent.Length;

            var childBlockMatch = ChildrenRegex.Match(yaml, startIndex);
            if (!childBlockMatch.Success) continue;

            var childMatches = ChildEntryRegex.Matches(childBlockMatch.Groups["block"].Value);
            foreach (Match c in childMatches)
            {
                long childID = long.Parse(c.Groups["id"].Value);
                result.children[id].Add(childID);
            }
        }

        // Determine root objects (objects that are not children of any other)
        HashSet<long> allChildren = new();
        foreach (var kvp in result.children)
            foreach (var child in kvp.Value)
                allChildren.Add(child);

        foreach (var id in result.objectNames.Keys)
            if (!allChildren.Contains(id))
                result.roots.Add(id);

        return result;
    }
}

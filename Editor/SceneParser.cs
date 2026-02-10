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
    // Single-line regex patterns for state machine parsing
    private static readonly Regex GameObjectHeaderRegex =
        new(@"^--- !u!1 &(?<id>\d+)");
    
    private static readonly Regex NameRegex =
        new(@"^\s*m_Name:\s+(?<name>.+)$");
    
    private static readonly Regex TransformHeaderRegex =
        new(@"^--- !u!4 &(?<id>\d+)");
    
    private static readonly Regex GameObjectRefRegex =
        new(@"^\s*m_GameObject:\s*\{fileID:\s*(?<id>\d+)\}");
    
    private static readonly Regex LocalRotationRegex =
        new(@"^\s*m_LocalRotation:\s*\{x:\s*(?<x>[-\d.e]+),\s*y:\s*(?<y>[-\d.e]+),\s*z:\s*(?<z>[-\d.e]+),\s*w:\s*(?<w>[-\d.e]+)\}");
    
    private static readonly Regex LocalPositionRegex =
        new(@"^\s*m_LocalPosition:\s*\{x:\s*(?<x>[-\d.e]+),\s*y:\s*(?<y>[-\d.e]+),\s*z:\s*(?<z>[-\d.e]+)\}");
    
    private static readonly Regex LocalScaleRegex =
        new(@"^\s*m_LocalScale:\s*\{x:\s*(?<x>[-\d.e]+),\s*y:\s*(?<y>[-\d.e]+),\s*z:\s*(?<z>[-\d.e]+)\}");
    
    private static readonly Regex ChildrenHeaderRegex =
        new(@"^\s*m_Children:");
    
    private static readonly Regex ChildEntryRegex =
        new(@"^\s*-\s*\{fileID:\s*(?<id>\d+)\}");
    
    // Prefab instance regex patterns
    private static readonly Regex PrefabInstanceHeaderRegex =
        new(@"^--- !u!1001 &(?<id>\d+)");
    
    private static readonly Regex ModificationTargetRegex =
        new(@"^\s*-\s*target:\s*\{fileID:\s*(?<targetId>\d+)");
    
    private static readonly Regex PropertyPathRegex =
        new(@"^\s*propertyPath:\s*(?<path>.+)$");
    
    private static readonly Regex ValueRegex =
        new(@"^\s*value:\s*(?<value>.*)$");

    public static ParsedScene Parse(string yaml)
    {
        var result = new ParsedScene();
        var transformToGo = new Dictionary<long, long>(); // Transform ID -> GameObject ID
        var transformDataById = new Dictionary<long, TransformData>(); // Temp storage by Transform ID
        
        var lines = yaml.Split('\n');
        
        // State machine variables
        long currentGameObjectId = -1;
        long currentTransformId = -1;
        TransformData currentTransformData = null;
        bool inChildrenBlock = false;
        
        // Prefab instance tracking
        long currentPrefabInstanceId = -1;
        bool inPrefabModifications = false;
        long currentModificationTarget = -1;
        string currentPropertyPath = null;
        
        // Prefab data storage
        var prefabNames = new Dictionary<long, string>();
        var prefabPosX = new Dictionary<long, float>();
        var prefabPosY = new Dictionary<long, float>();
        var prefabPosZ = new Dictionary<long, float>();
        var prefabRotX = new Dictionary<long, float>();
        var prefabRotY = new Dictionary<long, float>();
        var prefabRotZ = new Dictionary<long, float>();
        var prefabRotW = new Dictionary<long, float>();
        var prefabScaleX = new Dictionary<long, float>();
        var prefabScaleY = new Dictionary<long, float>();
        var prefabScaleZ = new Dictionary<long, float>();
        
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            
            var prefabHeaderMatch = PrefabInstanceHeaderRegex.Match(line);
            if (prefabHeaderMatch.Success)
            {
                currentPrefabInstanceId = long.Parse(prefabHeaderMatch.Groups["id"].Value);
                inPrefabModifications = false;
                currentGameObjectId = -1;
                currentTransformId = -1;
                inChildrenBlock = false;
                continue;
            }
            
            // Check if entering modifications block
            if (currentPrefabInstanceId != -1 && line.Trim() == "m_Modifications:")
            {
                inPrefabModifications = true;
                continue;
            }
            
            // Parse prefab modifications
            if (inPrefabModifications && currentPrefabInstanceId != -1)
            {
                // Check for modification target
                var targetMatch = ModificationTargetRegex.Match(line);
                if (targetMatch.Success)
                {
                    currentModificationTarget = long.Parse(targetMatch.Groups["targetId"].Value);
                    currentPropertyPath = null;
                    continue;
                }
                
                // Check for propertyPath
                var propPathMatch = PropertyPathRegex.Match(line);
                if (propPathMatch.Success)
                {
                    currentPropertyPath = propPathMatch.Groups["path"].Value.Trim();
                    continue;
                }
                
                // Check for value
                var valueMatch = ValueRegex.Match(line);
                if (valueMatch.Success && currentPropertyPath != null)
                {
                    string value = valueMatch.Groups["value"].Value.Trim();
                    
                    // Extract name
                    if (currentPropertyPath == "m_Name" && !string.IsNullOrEmpty(value))
                    {
                        prefabNames[currentPrefabInstanceId] = value;
                    }
                    // Extract position components
                    else if (currentPropertyPath == "m_LocalPosition.x")
                    {
                        if (float.TryParse(value, out float val))
                            prefabPosX[currentPrefabInstanceId] = val;
                    }
                    else if (currentPropertyPath == "m_LocalPosition.y")
                    {
                        if (float.TryParse(value, out float val))
                            prefabPosY[currentPrefabInstanceId] = val;
                    }
                    else if (currentPropertyPath == "m_LocalPosition.z")
                    {
                        if (float.TryParse(value, out float val))
                            prefabPosZ[currentPrefabInstanceId] = val;
                    }
                    // Extract rotation components
                    else if (currentPropertyPath == "m_LocalRotation.x")
                    {
                        if (float.TryParse(value, out float val))
                            prefabRotX[currentPrefabInstanceId] = val;
                    }
                    else if (currentPropertyPath == "m_LocalRotation.y")
                    {
                        if (float.TryParse(value, out float val))
                            prefabRotY[currentPrefabInstanceId] = val;
                    }
                    else if (currentPropertyPath == "m_LocalRotation.z")
                    {
                        if (float.TryParse(value, out float val))
                            prefabRotZ[currentPrefabInstanceId] = val;
                    }
                    else if (currentPropertyPath == "m_LocalRotation.w")
                    {
                        if (float.TryParse(value, out float val))
                            prefabRotW[currentPrefabInstanceId] = val;
                    }
                    // Extract scale components
                    else if (currentPropertyPath == "m_LocalScale.x")
                    {
                        if (float.TryParse(value, out float val))
                            prefabScaleX[currentPrefabInstanceId] = val;
                    }
                    else if (currentPropertyPath == "m_LocalScale.y")
                    {
                        if (float.TryParse(value, out float val))
                            prefabScaleY[currentPrefabInstanceId] = val;
                    }
                    else if (currentPropertyPath == "m_LocalScale.z")
                    {
                        if (float.TryParse(value, out float val))
                            prefabScaleZ[currentPrefabInstanceId] = val;
                    }
                    
                    currentPropertyPath = null;
                    continue;
                }
                
                // End of modifications block when we hit something that's not indented for modifications
                if (!line.StartsWith("    ") && !line.StartsWith("  -") && line.Trim().Length > 0 && !line.Trim().StartsWith("m_"))
                {
                    inPrefabModifications = false;
                    currentPrefabInstanceId = -1;
                }
            }
        
            // Check for GameObject header
            var goHeaderMatch = GameObjectHeaderRegex.Match(line);
            if (goHeaderMatch.Success)
            {
                currentGameObjectId = long.Parse(goHeaderMatch.Groups["id"].Value);
                result.children[currentGameObjectId] = new List<long>();
                currentTransformId = -1;
                inChildrenBlock = false;
                continue;
            }
            
            // Add prefab instances to the result as if they were regular GameObjects
            foreach (var prefabId in prefabNames.Keys)
            {
                // Add the name
                result.objectNames[prefabId] = prefabNames[prefabId];
                result.children[prefabId] = new List<long>(); // Prefabs typically don't have children tracked this way
                
                // Build transform data from collected components
                var transformData = new TransformData
                {
                    localPosition = new Vector3(
                        prefabPosX.ContainsKey(prefabId) ? prefabPosX[prefabId] : 0f,
                        prefabPosY.ContainsKey(prefabId) ? prefabPosY[prefabId] : 0f,
                        prefabPosZ.ContainsKey(prefabId) ? prefabPosZ[prefabId] : 0f
                    ),
                    localRotation = new Quaternion(
                        prefabRotX.ContainsKey(prefabId) ? prefabRotX[prefabId] : 0f,
                        prefabRotY.ContainsKey(prefabId) ? prefabRotY[prefabId] : 0f,
                        prefabRotZ.ContainsKey(prefabId) ? prefabRotZ[prefabId] : 0f,
                        prefabRotW.ContainsKey(prefabId) ? prefabRotW[prefabId] : 1f
                    ),
                    localScale = new Vector3(
                        prefabScaleX.ContainsKey(prefabId) ? prefabScaleX[prefabId] : 1f,
                        prefabScaleY.ContainsKey(prefabId) ? prefabScaleY[prefabId] : 1f,
                        prefabScaleZ.ContainsKey(prefabId) ? prefabScaleZ[prefabId] : 1f
                    )
                };
                
                result.transforms[prefabId] = transformData;
            }
        
            // Check for GameObject name (only if we're in a GameObject block)
            if (currentGameObjectId != -1 && currentTransformId == -1)
            {
                var nameMatch = NameRegex.Match(line);
                if (nameMatch.Success)
                {
                    result.objectNames[currentGameObjectId] = nameMatch.Groups["name"].Value.Trim();
                    continue;
                }
            }
            
            // Check for Transform header
            var transformHeaderMatch = TransformHeaderRegex.Match(line);
            if (transformHeaderMatch.Success)
            {
                currentTransformId = long.Parse(transformHeaderMatch.Groups["id"].Value);
                currentTransformData = new TransformData();
                currentGameObjectId = -1;
                inChildrenBlock = false;
                continue;
            }
            
            // Check for GameObject reference in Transform (links Transform to GameObject)
            if (currentTransformId != -1)
            {
                var goRefMatch = GameObjectRefRegex.Match(line);
                if (goRefMatch.Success)
                {
                    long goId = long.Parse(goRefMatch.Groups["id"].Value);
                    transformToGo[currentTransformId] = goId;
                    continue;
                }
                
                // Parse transform properties
                var rotationMatch = LocalRotationRegex.Match(line);
                if (rotationMatch.Success)
                {
                    currentTransformData.localRotation = new Quaternion(
                        float.Parse(rotationMatch.Groups["x"].Value),
                        float.Parse(rotationMatch.Groups["y"].Value),
                        float.Parse(rotationMatch.Groups["z"].Value),
                        float.Parse(rotationMatch.Groups["w"].Value)
                    );
                    continue;
                }
                
                var positionMatch = LocalPositionRegex.Match(line);
                if (positionMatch.Success)
                {
                    currentTransformData.localPosition = new Vector3(
                        float.Parse(positionMatch.Groups["x"].Value),
                        float.Parse(positionMatch.Groups["y"].Value),
                        float.Parse(positionMatch.Groups["z"].Value)
                    );
                    continue;
                }
                
                var scaleMatch = LocalScaleRegex.Match(line);
                if (scaleMatch.Success)
                {
                    currentTransformData.localScale = new Vector3(
                        float.Parse(scaleMatch.Groups["x"].Value),
                        float.Parse(scaleMatch.Groups["y"].Value),
                        float.Parse(scaleMatch.Groups["z"].Value)
                    );
                    transformDataById[currentTransformId] = currentTransformData;
                    continue;
                }
            }
            
            // Check for children header
            var childrenHeaderMatch = ChildrenHeaderRegex.Match(line);
            if (childrenHeaderMatch.Success)
            {
                inChildrenBlock = true;
                continue;
            }
            
            // Parse child entries
            if (inChildrenBlock)
            {
                var childMatch = ChildEntryRegex.Match(line);
                if (childMatch.Success)
                {
                    long childTransformId = long.Parse(childMatch.Groups["id"].Value);
                    
                    // Find the current parent GameObject (look backwards for the most recent Transform)
                    // The children block is part of a Transform, and we need to find which GameObject owns this Transform
                    for (int j = i - 1; j >= 0; j--)
                    {
                        var pastTransformMatch = TransformHeaderRegex.Match(lines[j]);
                        if (pastTransformMatch.Success)
                        {
                            long parentTransformId = long.Parse(pastTransformMatch.Groups["id"].Value);
                            if (transformToGo.ContainsKey(parentTransformId))
                            {
                                long parentGoId = transformToGo[parentTransformId];
                                if (!result.children.ContainsKey(parentGoId))
                                    result.children[parentGoId] = new List<long>();
                                
                                // Children are referenced by Transform ID, need to convert to GameObject ID
                                result.children[parentGoId].Add(childTransformId);
                            }
                            break;
                        }
                    }
                    continue;
                }
                else if (!line.Trim().StartsWith("-"))
                {
                    // End of children block
                    inChildrenBlock = false;
                }
            }
        }
        
        // Add prefab instances to the result as if they were regular GameObjects
        foreach (var prefabId in prefabNames.Keys)
        {
            // Add the name
            result.objectNames[prefabId] = prefabNames[prefabId];
            result.children[prefabId] = new List<long>(); // Prefabs typically don't have children tracked this way
            
            // Build transform data from collected components
            var transformData = new TransformData
            {
                localPosition = new Vector3(
                    prefabPosX.ContainsKey(prefabId) ? prefabPosX[prefabId] : 0f,
                    prefabPosY.ContainsKey(prefabId) ? prefabPosY[prefabId] : 0f,
                    prefabPosZ.ContainsKey(prefabId) ? prefabPosZ[prefabId] : 0f
                ),
                localRotation = new Quaternion(
                    prefabRotX.ContainsKey(prefabId) ? prefabRotX[prefabId] : 0f,
                    prefabRotY.ContainsKey(prefabId) ? prefabRotY[prefabId] : 0f,
                    prefabRotZ.ContainsKey(prefabId) ? prefabRotZ[prefabId] : 0f,
                    prefabRotW.ContainsKey(prefabId) ? prefabRotW[prefabId] : 1f
                ),
                localScale = new Vector3(
                    prefabScaleX.ContainsKey(prefabId) ? prefabScaleX[prefabId] : 1f,
                    prefabScaleY.ContainsKey(prefabId) ? prefabScaleY[prefabId] : 1f,
                    prefabScaleZ.ContainsKey(prefabId) ? prefabScaleZ[prefabId] : 1f
                )
            };
            
            result.transforms[prefabId] = transformData;
        }
        
        // Remap transforms from Transform IDs to GameObject IDs
        foreach (var kvp in transformDataById)
        {
            long transformId = kvp.Key;
            if (transformToGo.ContainsKey(transformId))
            {
                long goId = transformToGo[transformId];
                result.transforms[goId] = kvp.Value;
            }
        }
        
        // Convert children Transform IDs to GameObject IDs
        var childrenCopy = new Dictionary<long, List<long>>(result.children);
        result.children.Clear();
        
        foreach (var kvp in childrenCopy)
        {
            long parentGoId = kvp.Key;
            result.children[parentGoId] = new List<long>();
            
            foreach (long childTransformId in kvp.Value)
            {
                if (transformToGo.ContainsKey(childTransformId))
                {
                    long childGoId = transformToGo[childTransformId];
                    result.children[parentGoId].Add(childGoId);
                }
            }
        }
        
        // Determine root objects
        HashSet<long> allChildren = new HashSet<long>();
        foreach (var kvp in result.children)
            foreach (var child in kvp.Value)
                allChildren.Add(child);
        
        foreach (var id in result.objectNames.Keys)
            if (!allChildren.Contains(id))
                result.roots.Add(id);
        
        return result;
    }
}

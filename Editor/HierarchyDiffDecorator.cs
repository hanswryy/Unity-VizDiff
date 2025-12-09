// using UnityEditor;
// using UnityEngine;


// namespace SceneDiff
// {
//     [InitializeOnLoad]
//     public static class HierarchyDiffDecorator
//     {
//         static HierarchyDiffDecorator()
//         {
//             EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
//         }


//         private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
//         {
//             GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
//             if (go == null) return;


//             var type = DiffModel.GetChangeType(go);
//             if (type == ChangeType.None) return;


//             // small square indicator on right side
//             Rect r = new Rect(selectionRect);
//             r.x = r.xMax - 18; // position near right edge
//             r.width = 14;
//             r.height = 14;
//             r.y += 2;


//             Color iconColor = Color.white;
//             switch (type)
//             {
//                 case ChangeType.Added: iconColor = Color.green; break;
//                 case ChangeType.Modified: iconColor = Color.yellow; break;
//                 case ChangeType.Removed: iconColor = Color.red; break;
//             }


//             EditorGUI.DrawRect(r, iconColor);
//         }
//     }
// }
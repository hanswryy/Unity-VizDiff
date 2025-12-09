// using System.Collections.Generic;
// using UnityEngine;


// namespace SceneDiff
// {
//     public enum ChangeType
//     {
//         None,
//         Added,
//         Modified,
//         Removed
//     }


//     /// <summary>
//     /// Lightweight shared model for MVP.
//     /// Replace or extend with real diff results later.
//     /// Keys are GameObject names for now (simple demo).
//     /// </summary>
//     public static class DiffModel
//     {
//         private static Dictionary<string, ChangeType> _changes = new Dictionary<string, ChangeType>
//         {
//             { "Player", ChangeType.Modified },
//             { "Enemy", ChangeType.Added },
//             { "Tree", ChangeType.Removed }
//         };


//         public static ChangeType GetChangeType(GameObject go)
//         {
//             if (go == null) return ChangeType.None;
//             if (_changes.TryGetValue(go.name, out var t)) return t;
//             return ChangeType.None;
//         }


//         public static void SetChanges(Dictionary<string, ChangeType> newChanges)
//         {
//             _changes = newChanges ?? new Dictionary<string, ChangeType>();
//         }


//         public static void Clear()
//         {
//             _changes.Clear();
//         }
//     }
// }
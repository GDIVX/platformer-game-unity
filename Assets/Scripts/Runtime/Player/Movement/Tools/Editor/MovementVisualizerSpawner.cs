#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace Runtime.Player.Movement.Tools.Editor
{
    [InitializeOnLoad]
    public static class MovementVisualizerSpawner
    {
        private const string DefaultStatsPath = "Assets/Gameplay Data/PlayerMovementStats.asset";

        static MovementVisualizerSpawner()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            Handles.BeginGUI();

            GUILayout.BeginArea(new Rect(10, 10, 200, 30));
            GUI.backgroundColor = new Color(0.3f, 0.6f, 1f, 0.9f);

            if (GUILayout.Button("➕ Add Movement Visualizer", GUILayout.Height(24)))
            {
                CreateVisualizerAtSceneView(sceneView);
            }

            GUILayout.EndArea();
            GUI.backgroundColor = Color.white;

            Handles.EndGUI();
        }

        private static void CreateVisualizerAtSceneView(SceneView view)
        {
            // Determine spawn position from mouse
            Event currentEvent = Event.current;
            Ray ray = HandleUtility.GUIPointToWorldRay(
                currentEvent?.mousePosition ?? new Vector2(Screen.width / 2f, Screen.height / 2f)
            );

            Vector3 spawnPosition = ray.origin + ray.direction * 5f;

            // Create GameObject
            GameObject go = new GameObject("Movement Visualizer");
            Undo.RegisterCreatedObjectUndo(go, "Create Movement Visualizer");
            go.transform.position = spawnPosition;

            // Attach the visualizer component
            var visualizer = go.AddComponent<MovementVisualizer>();

            // Try to auto-assign PlayerMovementStats
            PlayerMovementStats stats = Object.FindFirstObjectByType<PlayerMovementStats>();
            if (stats == null)
            {
                stats = AssetDatabase.LoadAssetAtPath<PlayerMovementStats>(DefaultStatsPath);

                if (stats == null)
                {
                    Debug.LogWarning(
                        $"⚠ Could not find PlayerMovementStats in scene or at path '{DefaultStatsPath}'.\n" +
                        "Please assign it manually in the Movement Visualizer.");
                }
            }

            if (stats != null)
            {
                typeof(MovementVisualizer)
                    .GetField("_movementStats",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(visualizer, stats);
            }

            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }
    }

    [CustomEditor(typeof(MovementVisualizer))]
    public class MovementVisualizerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(10);
            GUI.backgroundColor = new Color(0.6f, 0.9f, 1f);
            if (GUILayout.Button("➕ Duplicate Here", GUILayout.Height(22)))
            {
                DuplicateHere((MovementVisualizer)target);
            }
            GUI.backgroundColor = Color.white;
        }

        private static void DuplicateHere(MovementVisualizer visualizer)
        {
            GameObject original = visualizer.gameObject;
            GameObject clone = Object.Instantiate(original,
                original.transform.position + Vector3.right * 1.5f, Quaternion.identity);
            clone.name = original.name + " (Copy)";
            Undo.RegisterCreatedObjectUndo(clone, "Duplicate Movement Visualizer");
            Selection.activeGameObject = clone;
            EditorGUIUtility.PingObject(clone);
        }
    }
}
#endif

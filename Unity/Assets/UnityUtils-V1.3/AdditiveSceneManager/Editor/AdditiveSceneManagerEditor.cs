using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AdditiveSceneManager))]
public class AdditiveSceneManagerEditor : Editor
{
    AdditiveSceneManager manager;

    public void OnEnable() {
        manager = (AdditiveSceneManager)target;
    }

    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        // Don't do anything if not playing
        if (!Application.isPlaying) return;

        // Render controls for each scene
        EditorGUILayout.LabelField("=== Scene Controls ===", EditorStyles.boldLabel);
        foreach(string s in manager.scene_names) SceneButtons(s);
    }

    public void SceneButtons(string s) {

        // Query if scene is active
        bool sceneActive = manager.QuerySceneLoaded(s);

        // Start a horizontal group for the buttons
        GUILayout.BeginHorizontal();

        GUI.enabled = !sceneActive;
        if (GUILayout.Button($"Load \"{s}\"")) manager.LoadScene(s);
        GUI.enabled = sceneActive;
        GUILayout.Space(5);
        if (GUILayout.Button($"Unload \"{s}\"")) manager.UnloadScene(s);
        GUI.enabled = true;

        // End the horizontal group
        GUILayout.EndHorizontal();
    }

}

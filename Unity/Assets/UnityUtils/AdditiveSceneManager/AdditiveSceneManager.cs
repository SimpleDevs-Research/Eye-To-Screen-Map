using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class AdditiveSceneManager : MonoBehaviour
{

    [System.Serializable]
    public class Ref
    {
        public string key;
        public GameObject reference;
    }

    // Global instance
    public static AdditiveSceneManager Instance;
    
    [Header("=== Scene Lists ===")]
    [Tooltip("List of additive scenes. These scenes must be added to \"Build Settings\" too.")]
    #if UNITY_EDITOR
    public Object[] scenes;
    #endif
    [HideInInspector] 
    public List<string> scene_names;
    private Dictionary<string, Object> sceneDict;
    private List<string> activeScenes = new List<string>();

    [Header("=== Reference Management ===")]
    public List<Ref> references = new List<Ref>();
    private Dictionary<string, GameObject> refDict;

    [Header("=== Callbacks ===")]
    public UnityEvent onSceneLoadedCallback;
    public UnityEvent onSceneUnloadedCallback;

    #if UNITY_EDITOR
    private void OnValidate()
    {
        scene_names.Clear();
        foreach (Object s in scenes) {
            if (s != null) scene_names.Add(s.name);
        }
    }
    #endif

    // Awake
    private void Awake() {
        Instance = this;
        InitializeScenes();
        InitializeRefs();
    }

    private void InitializeScenes() {
        // Load listeners
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        // Initialize arrays and dictionary
        activeScenes = new List<string>();
    }

    private void InitializeRefs()
    {
        refDict = new Dictionary<string, GameObject>();
        foreach(Ref r in references) refDict.Add(r.key, r.reference);
    }

    // Add Scene from `_scenes` to scene
    public void LoadScene(string query, LoadSceneMode mode) {
        if(!scene_names.Contains(query)) {
            Debug.LogError($"Query scene \"{query}\" doesn't exist in this scene manager!");
            return;
        }
        if (activeScenes.Contains(query)) {
            Debug.LogError($"Query scene \"{query}\" is already loaded additively. Will not add scene again.");
            return;
        }
        SceneManager.LoadScene(query, mode);
    }
    public void LoadScene(string query) {
        LoadScene(query, LoadSceneMode.Additive);
    }

    public void UnloadScene(string query) {
        if (!scene_names.Contains(query)) {
            Debug.LogError($"Query scene \"{query}\" doesn't exist in this scene manager!");
            return;
        }
        if (!activeScenes.Contains(query)) {
            Debug.LogError($"Query scene \"{query}\" is not loaded.");
            return;
        }
        SceneManager.UnloadSceneAsync(query);
    }

    public void ToggleScene(string query)
    {
        if (activeScenes.Contains(query)) UnloadScene(query);
        else LoadScene(query, LoadSceneMode.Additive);
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        SceneManager.SetActiveScene(scene);
        if (!activeScenes.Contains(scene.name)) activeScenes.Add(scene.name);
        Debug.Log($"Scene \"{scene.name}\" loaded!");
        onSceneLoadedCallback?.Invoke();
    }

    public void OnSceneUnloaded(Scene scene) {
        if (activeScenes.Contains(scene.name)) activeScenes.Remove(scene.name);
        Debug.Log($"Scene \"scene.name\" unloaded!");
        onSceneUnloadedCallback?.Invoke();
    }

    public bool QuerySceneLoaded(string query) {
        return activeScenes.Contains(query);
    }

    public bool TryGetRef(string query, out GameObject g)
    {
        bool found = refDict.ContainsKey(query);
        g = found ? refDict[query] : null;
        return found;
    }

    void OnDestroy() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }






}

# UnityUtils

_A collection of helpful Unity scripts and editor tools, purely for convenience._

---

## ReadOnly

_**Source:** [FuzzyLogic's answer in the Unity forums](https://discussions.unity.com/t/how-to-make-a-readonly-property-in-inspector/75448/7)_

To convert any field into a ReadOnly column:

````csharp
[ReadOnlyInsp] public float example;
````

## Help Box

_**Source:** [unity-inspector-help](https://github.com/johnearnshaw/unity-inspector-help/tree/master)_

Add a help text box info into your inspector.

By default, you can just use:

````csharp
[Help("This is some help text!")]
public float example = 1000f;
````

You can add an optional parameter that changes the icon that appears with the help box:

* `UnityEditor.MessageType.Info` : The default icon.
* `UnityEditor.MessageType.Error` : A red error icon.
* `UnityEditor.MessageType.Warning` : A yellow warning icon.
* `UnityEditor.MessageType.None` : Removes the icon, leaving only the text.

````csharp
#if UNITY_EDITOR
[Help("This is some help text!", UnityEditor.MessageType.None)]
#endif
public float example = 1000f;
````

## Serializables

Extension Functions for various Unity-specific class types such as `Vector2` and `Vector3`. These scripts focus on creating `SVector2`, `SVector3`, and `SQuaternion` equivalents of primitive Unity `Vector2`, `Vector3`, and `Quaternion` classes for use in CSV-writing and such, as the primitive classes are not serializable for JSON or CSV reading/writing.

The existing list of serialized equivalents of primitive Unity classes are:

* `Vector2` => `SVector2`
* `Vector3` => `SVector3`
* `Vector4` => `SVector4`
* `Quaternion` => `SQuaternion`
* `Color32` => `SColor32`
* `RaycastHit` => `SRaycastHit`

To use these serialized equivalents, you must import the namespace `using SerializableTypes`.

## Frame Count

The `FrameCount.cs` component helps you gain access to the following details about your game:

- `frame_count`: What's the current frame since the start of the game?
- `fps`: What's the raw FPS of the game?
- `smoothed_fps`: What's the smoothed FPS of the game?

The smoothed FPS is a weighted FPS value set between the raw FPS of that frame and the previous FPS value. This prevents shocks in FPS such as frame spikes from severely affecting the FPS value. 

### Usage

You can access these values publicly as long as `FrameCount.cs` is present in your scene. Note that this component is a **singleton** and only one is needed in your current scene; you can reference it anywhere in your scene via `FrameCount.Instance`.

If you want to print the FPS on a `TextMeshProUGUI` textbox (i.e. debugging), this component can also handle that. The component comes with a `textboxes` list that lets you print out your desired variable in the textbox you reference it to.


## Additive Scene Manager

Included in this package is an `AdditiveSceneManager.cs` component. This component is a special helper to enable ADDITIVE scene management. I.E. allows you to load and unload scenes on top of another existing, single scene. Helpful for situations where you may want singleton logic at a scene level, by containing major components in the base scene and only switching out certain content dynamically. Note that this component is a **singleton** and only one is needed in your base scene; you can reference it anywhere in either your base or additive scenes via `AdditiveSceneManager.Instance`.

### Installation

Installation is really simple: just add `AdditiveSceneManager.cs` to any GameObject in your base scene, and populate the `Scenes` list with all the additive scenes.

### Usage 

You can think of the implementation as having a "Base" or "Core" scene where you place the `AdditiveSceneManager.cs` in, and then you have "Additive" scenes that you add on top of or remove from that base scene. The `AdditiveSceneManager.cs` component has two major ways of interaction:

1. `Scenes`: You must add any scenes you want to additively load here. All these scenes must also be check-marked in your "Build Settings".
2. `References`: If any of your additive scenes require references to objects in the base scene, you can add references to them here. Then you can query this list to get `GameObject` references.

During runtime, if you are in the editor, you can debug how additive scenes load in via the Inspector; you can load and unload your additive scenes via some UI buttons we've added. Alternatively, if you are playing the game directly, you can instead call the following functions to control loading and unloading:

|Function|Parameters|Returns|Description|
|:-|:-|:-|:-|
|`LoadScene`|`string scene_name`, `LoadSceneMode mode = LoadSceneMode.Additive`|_null_|Loads a scene based on a string query. You CAN technically use this to load single scenes via the 2nd parameter, but this is not the intended function of this manager. **Only loads in scenes added to its `Scenes` list; doesn't control any scenes NOT added to that list.|
|`UnloadScene`|`string scene_name`|_null_|Unloads a scene based on a string query. Only unloads a scene if 1) it's been added to the `Scenes` list of this manager, and 2) if it's loaded already. Doesn't do anything if the queried scene is unloaded already.|
|`ToggleScene`|`string scene_name`|_null_|Rather than `LoadScene` or `UnloadScene`, you toggle the queried scene on or off.|
|`QuerySceneLoaded`|`string scene_name`|`bool`|You can check if a scene has been additively loaded from this manager using this function.|
|`TryGetRe`|`string query`, `out GameObject g`|`bool`|Try and get a reference saved by the `AdditiveSceneManager.cs` component. If no reference is found, returns `false`. if it finds the reference, it returns `true` and lets you access the GameObject via its `out` parameter.|

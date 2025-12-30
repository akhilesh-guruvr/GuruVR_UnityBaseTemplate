using UnityEngine;

[CreateAssetMenu(fileName = "SceneConfig", menuName = "Scene Loading/Scene Config")]
public class SceneConfig : ScriptableObject
{
    public SceneEntry[] scenes;

    [System.Serializable]
    public struct SceneEntry
    {
        public string key;         // Unique ID ("game", "menu", "lab")
        public string sceneName;   // Real scene name or Addressable key

        public bool useAddressable;

        public string contentUrl;  // Folder (optional if local)
        public string catalogURL;  // Full URL or leave empty for auto
    }

    public SceneEntry? GetSceneByKey(string key)
    {
        foreach (var entry in scenes)
            if (entry.key == key)
                return entry;

        return null;
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneButtonLoader : MonoBehaviour
{
    public string sceneName;
    public bool useAddressable = false;
    public string catalogURL;

    public void LoadScene()
    {
        SceneLoadData.targetScene = sceneName;
        SceneLoadData.useAddressable = useAddressable;
        SceneLoadData.catalogURL = catalogURL;

        // Load the loading scene
        SceneManager.LoadScene("LoadingScene"); // Make sure you have a scene called "LoadingScene"
    }
}

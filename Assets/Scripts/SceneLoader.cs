using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using System.IO;
using UnityEngine.AddressableAssets.ResourceLocators;

public class SceneLoader : MonoBehaviour
{
    [System.Serializable]
    public struct SceneButton
    {
        public Button button; // UI Button
        public string sceneName; // Addressable scene key to load
        public string catalogURL; // Catalog URL for Addressables
        public string contentUrl; // Content URL from concept (if provided, used as catalog base path for addressable)
    }
    public SceneButton[] sceneButtons;
    void Start()
    {
        Debug.Log($"[SceneLoader] Initializing in scene: {SceneManager.GetActiveScene().name} on GameObject: {gameObject.name} (InstanceID: {gameObject.GetInstanceID()})");
        // Register button listeners
        foreach (var sceneButton in sceneButtons)
        {
            if (sceneButton.button != null && !string.IsNullOrEmpty(sceneButton.sceneName))
            {
                sceneButton.button.onClick.RemoveAllListeners(); // Clear existing listeners
                sceneButton.button.onClick.AddListener(() => SceneLoaderUtility.LoadScene(sceneButton));
                Debug.Log($"[SceneLoader] Registered button '{sceneButton.button.name}' for scene: {sceneButton.sceneName} on GameObject: {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[SceneLoader] Invalid button or scene name in SceneLoader on {gameObject.name}. Button: {sceneButton.button}, Scene: {sceneButton.sceneName}");
            }
        }
    }
    void OnDestroy()
    {
        foreach (var sceneButton in sceneButtons)
        {
            if (sceneButton.button != null)
            {
                sceneButton.button.onClick.RemoveAllListeners();
            }
        }
        Debug.Log($"[SceneLoader] Destroyed in scene: {SceneManager.GetActiveScene().name} on GameObject: {gameObject.name}");
    }
}

public static class SceneLoaderUtility
{
    private static bool isLoading = false; // Prevent multiple simultaneous loads
    private static readonly string[] multiplayerScenes = { "EntryLobby_Final" }; // Replace with your multiplayer scene names
    private static readonly string CATALOG_SUFFIX = "/catalog_0.1.0.json"; // Suffix for remote catalog
    private const string LOADING_SCENE = "LoadingScene"; // Transition scene name
    // Static variables to pass data to LoadingScene
    public static string TargetSceneName { get; private set; } = "";
    public static bool TargetUseAddressable { get; private set; } = true;
    public static string TargetCatalogURL { get; private set; } = "";
    // Public method to force reset isLoading (for emergency recovery)
    public static void ResetLoadingFlag()
    {
        Debug.LogWarning("[SceneLoaderUtility] Force resetting isLoading flag to false!");
        isLoading = false;
    }
    public static void LoadScene(SceneLoader.SceneButton sceneButton)
    {
        isLoading = false;
        Debug.Log($"[SceneLoaderUtility] === LoadScene ENTRY === Called for target from button: {sceneButton.button?.name ?? "Unknown"}. SceneButton details - SceneName: '{sceneButton.sceneName}', ContentUrl: '{sceneButton.contentUrl}', CatalogURL: '{sceneButton.catalogURL}'");
        // Determine load key and catalog URL based on contentUrl
        string loadKey = sceneButton.sceneName; // Always use sceneName as the addressable scene key
        string catalogURL = sceneButton.catalogURL;
        if (!string.IsNullOrEmpty(sceneButton.contentUrl) && sceneButton.contentUrl != "string")
        {
            // Use content_url as base for catalog
            catalogURL = sceneButton.contentUrl.EndsWith("/") ? sceneButton.contentUrl + "catalog_0.1.0.json" : sceneButton.contentUrl + "/catalog_0.1.0.json";
            Debug.Log($"[SceneLoaderUtility] Using contentUrl as catalog base: {catalogURL} for addressable loadKey: {loadKey}");
        }
        else if (string.IsNullOrEmpty(catalogURL))
        {
            Debug.LogError($"[SceneLoaderUtility] No valid catalog URL or contentUrl provided for addressable load. Aborting.");
            return;
        }
        Debug.Log($"[SceneLoaderUtility] Final load config: LoadKey='{loadKey}', CatalogURL='{catalogURL}' from current scene: {SceneManager.GetActiveScene().name}. Button: {sceneButton.button?.name}");
        Debug.Log($"[SceneLoaderUtility] Post-config: isLoading = {isLoading}, currentScene = {SceneManager.GetActiveScene().name}, loadKey = '{loadKey}', loadKey.Length = {loadKey.Length}");
        Debug.Log($"[SceneLoaderUtility] ===== CRITICAL CHECK: isLoading = {isLoading} =====");
        if (isLoading)
        {
            Debug.LogError($"[SceneLoaderUtility] ✗✗✗ Scene load already in progress! isLoading = true. Ignoring request for: {loadKey}");
            Debug.LogError($"[SceneLoaderUtility] This means a previous scene load didn't complete properly or isLoading wasn't reset!");
            // Re-enable button since we're not proceeding
            if (sceneButton.button != null)
            {
                sceneButton.button.interactable = true;
            }
            return;
        }
        Debug.Log("[SceneLoaderUtility] ✓ Passed isLoading check");
        Debug.Log($"[SceneLoaderUtility] ===== CHECKING IF ALREADY IN TARGET SCENE =====");
        Debug.Log($"[SceneLoaderUtility] Current scene: '{SceneManager.GetActiveScene().name}', Target: '{loadKey}'");
        if (SceneManager.GetActiveScene().name == loadKey)
        {
            Debug.LogWarning($"[SceneLoaderUtility] ✗ Already in target scene '{loadKey}'. Disabling button.");
            if (sceneButton.button != null)
            {
                sceneButton.button.interactable = false;
            }
            return;
        }
        Debug.Log("[SceneLoaderUtility] ✓ Passed 'already in scene' check");
        // Disable the triggering button to prevent spam-clicks during load
        Debug.Log($"[SceneLoaderUtility] About to disable button '{sceneButton.button?.name ?? "null"}'");
        if (sceneButton.button != null)
        {
            sceneButton.button.interactable = false;
            Debug.Log($"[SceneLoaderUtility] Disabled button '{sceneButton.button.name}' during load.");
        }
        else
        {
            Debug.LogWarning("[SceneLoaderUtility] Button is null - skipping disable");
        }
        Debug.Log("[SceneLoaderUtility] Passed disable button");
        Debug.Log($"[SceneLoaderUtility] Skipping Build Settings check - using Addressables for '{loadKey}' with catalog '{catalogURL}'.");
        Debug.Log("[SceneLoaderUtility] Passed scene existence check (Addressables)");
        // Photon handling
        bool isCurrentSceneMultiplayer = System.Array.Exists(multiplayerScenes, scene => scene == SceneManager.GetActiveScene().name);
        bool isTargetSceneMultiplayer = System.Array.Exists(multiplayerScenes, scene => scene == loadKey);
        // Debug.Log($"[SceneLoaderUtility] Photon check: Current multiplayer? {isCurrentSceneMultiplayer}, Target multiplayer? {isTargetSceneMultiplayer}. Connected: {PhotonNetwork.IsConnected}, InRoom: {PhotonNetwork.InRoom}");
        if (isCurrentSceneMultiplayer && !isTargetSceneMultiplayer)
        {
            /* if (PhotonNetwork.InRoom)
               {
                   PhotonNetwork.LeaveRoom();
                   Debug.Log("[SceneLoaderUtility] Leaving Photon room before scene change.");
               }
               if (PhotonNetwork.IsConnected)
               {
                   PhotonNetwork.Disconnect();
                   Debug.Log("[SceneLoaderUtility] Disconnecting from Photon before scene change.");
               }*/
        }
        else
        {
            Debug.Log($"[SceneLoaderUtility] No Photon cleanup needed.");
        }
        Debug.Log("[SceneLoaderUtility] Passed Photon handling");
        if (!string.IsNullOrEmpty(loadKey))
        {
            Debug.Log("[SceneLoaderUtility] About to store target data and load transition scene");
            // CRITICAL FIX: Set isLoading BEFORE loading the transition scene
            isLoading = true;
            Debug.Log($"[SceneLoaderUtility] Set isLoading = true before transition scene load");
            // Store target scene data for LoadingScene to pick up
            TargetSceneName = loadKey;
            TargetUseAddressable = true;
            TargetCatalogURL = catalogURL;
            Debug.Log($"[SceneLoaderUtility] Stored target scene data: Name='{TargetSceneName}', UseAddressable={TargetUseAddressable}, CatalogURL='{TargetCatalogURL}'");
            Debug.Log($"[SceneLoaderUtility] Loading transition scene: {LOADING_SCENE}");
            // Load the LoadingScene first
            SceneManager.LoadScene(LOADING_SCENE);
            Debug.Log("[SceneLoaderUtility] SceneManager.LoadScene(LOADING_SCENE) called");
        }
        else
        {
            Debug.LogError("[SceneLoaderUtility] Invalid scene name provided.");
        }
        Debug.Log($"[SceneLoaderUtility] === LoadScene EXIT ===");
    }
    // Coroutine for loading the target scene, with progress callback for UI updates
    public static IEnumerator LoadSceneAsync(string loadKey, bool useAddressable, string catalogURL, System.Action<float> progressCallback = null)
    {
        Debug.Log($"[SceneLoaderUtility] === LoadSceneAsync ENTRY === for {loadKey} (Addressable: {useAddressable})");
        Debug.Log($"[SceneLoaderUtility] LoadSceneAsync started for {loadKey}. isLoading is currently: {isLoading}");
        // Note: isLoading should already be true from LoadScene(), but we verify it here
        if (!isLoading)
        {
            Debug.LogWarning("[SceneLoaderUtility] LoadSceneAsync called but isLoading was false! Setting it to true now.");
            isLoading = true;
        }
        float timeout = 90f; // Increased timeout for heavy scenes
        float startTime = Time.realtimeSinceStartup;
        AsyncOperationHandle<IResourceLocator> catalogHandle = default;
        AsyncOperationHandle<SceneInstance> sceneHandle = default;
        AsyncOperation asyncLoad = default;
        try
        {
            if (useAddressable)
            {
                if (string.IsNullOrEmpty(catalogURL))
                {
                    Debug.LogError("[SceneLoaderUtility] No catalog URL for addressable load. Aborting.");
                    yield break;
                }
                Debug.Log($"[SceneLoaderUtility] Addressable load: Loading remote catalog from {catalogURL}...");
                // Load remote catalog
                catalogHandle = Addressables.LoadContentCatalogAsync(catalogURL, false); // false = don't initialize
                startTime = Time.realtimeSinceStartup;
                while (!catalogHandle.IsDone)
                {
                    float elapsed = Time.realtimeSinceStartup - startTime;
                    if (elapsed > timeout)
                    {
                        Debug.LogError($"[SceneLoaderUtility] Catalog load timed out after {timeout} seconds (elapsed: {elapsed:F2}s).");
                        yield break;
                    }
                    float catProgress = catalogHandle.PercentComplete;
                    if (progressCallback != null) progressCallback(catProgress * 0.1f); // Catalog contributes 10% to overall progress
                    Debug.Log($"[SceneLoaderUtility] Loading catalog: Progress = {catProgress * 100:F2}%, Elapsed: {elapsed:F2}s");
                    yield return null;
                }
                Debug.Log($"[SceneLoaderUtility] Catalog handle status: {catalogHandle.Status}, PercentComplete: {catalogHandle.PercentComplete * 100:F2}%");
                if (catalogHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"[SceneLoaderUtility] Failed to load remote catalog: {catalogHandle.OperationException}. Status: {catalogHandle.Status}");
                    yield break;
                }
                if (progressCallback != null) progressCallback(0.1f); // Catalog complete
                // Load the scene (locator stays active until catalogHandle is released later)
                Debug.Log($"[SceneLoaderUtility] Addressable load: Starting LoadSceneAsync for key '{loadKey}'...");
                sceneHandle = Addressables.LoadSceneAsync(loadKey, LoadSceneMode.Single);
                startTime = Time.realtimeSinceStartup;
                while (!sceneHandle.IsDone)
                {
                    float elapsed = Time.realtimeSinceStartup - startTime;
                    if (elapsed > timeout)
                    {
                        Debug.LogError($"[SceneLoaderUtility] Scene {loadKey} load timed out after {timeout} seconds (elapsed: {elapsed:F2}s).");
                        yield break;
                    }
                    float sceneProgress = sceneHandle.PercentComplete;
                    float overallProgress = 0.1f + 0.9f * sceneProgress; // Scene contributes 90%
                    if (progressCallback != null) progressCallback(overallProgress);
                    Debug.Log($"[SceneLoaderUtility] Loading {loadKey}: Progress = {overallProgress * 100:F2}%, Status: {sceneHandle.Status}, Elapsed: {elapsed:F2}s");
                    yield return null;
                }
                Debug.Log($"[SceneLoaderUtility] Scene handle final status: {sceneHandle.Status}, IsDone: {sceneHandle.IsDone}");
                if (sceneHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"[SceneLoaderUtility] Failed to load scene: {sceneHandle.OperationException}. Status: {sceneHandle.Status}");
                    yield break;
                }
                Debug.Log($"[SceneLoaderUtility] Scene {loadKey} loaded successfully via Addressables. SceneInstance: {sceneHandle.Result.Scene.name}");
            }
            else
            {
                Debug.LogError("[SceneLoaderUtility] Local loading not supported in this configuration.");
                yield break;
            }
            if (progressCallback != null) progressCallback(1f); // Loading complete
            Debug.Log($"[SceneLoaderUtility] Scene {loadKey} loaded asynchronously. Performing cleanup.");
            Resources.UnloadUnusedAssets();
            yield return null; // Brief wait for background unloading to start (helps with consecutive heavy loads)
            System.GC.Collect();
            Debug.Log($"[SceneLoaderUtility] Cleanup completed (UnloadUnusedAssets + GC) for {loadKey}. Memory should now be stabilized.");
        }
        finally
        {
            // Cleanup handles
            if (sceneHandle.IsValid()) Addressables.Release(sceneHandle);
            if (catalogHandle.IsValid()) Addressables.Release(catalogHandle);
            // Clear stored target data
            TargetSceneName = "";
            TargetUseAddressable = true;
            TargetCatalogURL = "";
            // CRITICAL: Reset isLoading flag
            isLoading = false;
            Debug.Log($"[SceneLoaderUtility] LoadSceneAsync completed for {loadKey}. isLoading set to false. New active scene: {SceneManager.GetActiveScene().name}");
            Debug.Log($"[SceneLoaderUtility] === LoadSceneAsync EXIT ===");
        }
    }
}


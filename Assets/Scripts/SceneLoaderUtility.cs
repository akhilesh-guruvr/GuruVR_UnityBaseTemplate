//using System.Collections;
//using System.Linq;
//using UnityEngine;
//using UnityEngine.SceneManagement;
//using UnityEngine.AddressableAssets;
//using UnityEngine.ResourceManagement.AsyncOperations;
//using UnityEngine.ResourceManagement.ResourceProviders;
//using UnityEngine.AddressableAssets.ResourceLocators;
//using System.IO;

//public static class SceneLoaderUtility
//{
//    private static bool isLoading = false;
//    private const string LOADING_SCENE = "LoadingScene";

//    public static string TargetSceneName { get; private set; } = "";
//    public static bool TargetUseAddressable { get; private set; } = false;
//    public static string TargetCatalogURL { get; private set; } = "";

//    public static void ResetLoadingFlag()
//    {
//        Debug.LogWarning("[SceneLoaderUtility] Forced reset of loading flag.");
//        isLoading = false;
//    }

//    public static void LoadScene(SceneLoader.SceneButton sceneButton)
//    {
//        Debug.Log($"[SceneLoaderUtility] LoadScene called for: {sceneButton.sceneName}");

//        string loadKey = sceneButton.sceneName;
//        bool useAddressable = sceneButton.useAddressable;

//        // DO NOT LOWERCASE URL ANYMORE
//        string catalogURL = sceneButton.catalogURL;
//        string contentUrl = sceneButton.contentUrl;

//        // Construct final catalog URL if using contentUrl
//        if (useAddressable)
//        {
//            if (!string.IsNullOrEmpty(contentUrl) && contentUrl != "string")
//            {
//                catalogURL = contentUrl.EndsWith("/")
//                    ? contentUrl + "catalog_1.0.0.json"
//                    : contentUrl + "/catalog_1.0.0.json";

//                Debug.Log($"[SceneLoaderUtility] Using contentUrl catalog path: {catalogURL}");
//            }
//        }

//        Debug.Log($"[SceneLoaderUtility] FINAL CONFIG → Scene='{loadKey}', Addressable={useAddressable}, CatalogURL='{catalogURL}'");

//        if (isLoading)
//        {
//            Debug.LogWarning("[SceneLoaderUtility] Already loading a scene.");
//            sceneButton.button?.SetInteractable(true);
//            return;
//        }

//        if (SceneManager.GetActiveScene().name == loadKey)
//        {
//            Debug.LogWarning($"[SceneLoaderUtility] Already in '{loadKey}'");
//            sceneButton.button?.SetInteractable(false);
//            return;
//        }

//        sceneButton.button?.SetInteractable(false);
//        isLoading = true;

//        TargetSceneName = loadKey;
//        TargetUseAddressable = useAddressable;
//        TargetCatalogURL = catalogURL;

//        Debug.Log($"[SceneLoaderUtility] Loading intermediate scene '{LOADING_SCENE}'");
//        SceneManager.LoadScene(LOADING_SCENE);
//    }

//    public static IEnumerator LoadSceneAsync(string loadKey, bool useAddressable, string catalogURL, System.Action<float> progressCallback = null)
//    {
//        Debug.Log($"[SceneLoaderUtility] Begin async load: '{loadKey}' Addressable={useAddressable}");

//        float timeout = 90f;
//        float startTime;

//        AsyncOperationHandle<IResourceLocator> catalogHandle = default;
//        AsyncOperationHandle<SceneInstance> sceneHandle = default;
//        AsyncOperation asyncLoad = default;

//        try
//        {
//            if (useAddressable)
//            {
//                if (string.IsNullOrEmpty(catalogURL))
//                {
//                    Debug.LogError("[SceneLoaderUtility] No catalog URL provided!");
//                    yield break;
//                }

//                Debug.Log($"[SceneLoaderUtility] Loading catalog: {catalogURL}");

//                catalogHandle = Addressables.LoadContentCatalogAsync(catalogURL, false);
//                startTime = Time.realtimeSinceStartup;

//                while (!catalogHandle.IsDone)
//                {
//                    if (Time.realtimeSinceStartup - startTime > timeout)
//                    {
//                        Debug.LogError("[SceneLoaderUtility] Catalog load timeout!");
//                        yield break;
//                    }

//                    progressCallback?.Invoke(catalogHandle.PercentComplete * 0.1f);
//                    yield return null;
//                }

//                if (catalogHandle.Status != AsyncOperationStatus.Succeeded)
//                {
//                    Debug.LogError($"[SceneLoaderUtility] Catalog failed: {catalogHandle.OperationException}");
//                    yield break;
//                }

//                Debug.Log("[SceneLoaderUtility] Catalog loaded successfully.");

//                sceneHandle = Addressables.LoadSceneAsync(loadKey, LoadSceneMode.Single);
//                startTime = Time.realtimeSinceStartup;

//                while (!sceneHandle.IsDone)
//                {
//                    if (Time.realtimeSinceStartup - startTime > timeout)
//                    {
//                        Debug.LogError("[SceneLoaderUtility] Scene load timeout!");
//                        yield break;
//                    }

//                    progressCallback?.Invoke(0.1f + 0.9f * sceneHandle.PercentComplete);
//                    yield return null;
//                }

//                if (sceneHandle.Status != AsyncOperationStatus.Succeeded)
//                {
//                    Debug.LogError($"[SceneLoaderUtility] Scene load failed: {sceneHandle.OperationException}");
//                    yield break;
//                }

//                Debug.Log($"[SceneLoaderUtility] Scene '{loadKey}' loaded via Addressables.");
//            }
//            else
//            {
//                Debug.Log($"[SceneLoaderUtility] Loading local scene '{loadKey}'");
//                asyncLoad = SceneManager.LoadSceneAsync(loadKey);
//                if (asyncLoad == null)
//                {
//                    Debug.LogError("[SceneLoaderUtility] Local scene load failed to start.");
//                    yield break;
//                }

//                asyncLoad.allowSceneActivation = true;
//                startTime = Time.realtimeSinceStartup;

//                while (!asyncLoad.isDone)
//                {
//                    if (Time.realtimeSinceStartup - startTime > timeout)
//                    {
//                        Debug.LogError("[SceneLoaderUtility] Local scene load timeout!");
//                        yield break;
//                    }

//                    progressCallback?.Invoke(asyncLoad.progress);
//                    yield return null;
//                }
//            }

//            progressCallback?.Invoke(1f);
//        }
//        finally
//        {
//            if (sceneHandle.IsValid()) Addressables.Release(sceneHandle);
//            if (catalogHandle.IsValid()) Addressables.Release(catalogHandle);

//            TargetSceneName = "";
//            TargetUseAddressable = false;
//            TargetCatalogURL = "";
//            isLoading = false;
//        }
//    }

//    private static void SetInteractable(this UnityEngine.UI.Button button, bool value)
//    {
//        if (button != null) button.interactable = value;
//    }
//}

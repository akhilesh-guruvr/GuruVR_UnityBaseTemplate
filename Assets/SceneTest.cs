//using System.Collections;
//using UnityEngine;
//using UnityEngine.SceneManagement;
//using UnityEngine.AddressableAssets;
//using UnityEngine.ResourceManagement.AsyncOperations;
//using UnityEngine.ResourceManagement.ResourceProviders;
//using UnityEngine.AddressableAssets.ResourceLocators;

//public class SceneLoader : MonoBehaviour
//{
//    [Header("Scene Settings")]
//    public string sceneName; // Name or key of the scene to load
//    public bool useAddressable = true; // Flag to load the scene via Addressables
//    public string catalogURL; // URL for the Addressables catalog (if using remote catalogs)

//    private void Awake()
//    {
//        Debug.Log($"[SceneLoader] Starting to load scene: {sceneName}");

//        if (useAddressable)
//        {
//            // Check if catalogURL is provided, and load the catalog first
//            if (!string.IsNullOrEmpty(catalogURL))
//            {
//                StartCoroutine(LoadAddressableScene());
//            }
//            else
//            {
//                Debug.LogError("[SceneLoader] Catalog URL is empty. Cannot load Addressable scene.");
//            }
//        }
//        else
//        {
//            // Load the scene directly (no Addressables)
//            SceneManager.LoadScene(sceneName);
//        }
//    }

//    private IEnumerator LoadAddressableScene()
//    {
//        Debug.Log($"[SceneLoader] Loading Addressable scene '{sceneName}'...");

//        // Load the addressable content catalog
//        AsyncOperationHandle<IResourceLocator> catalogHandle = Addressables.LoadContentCatalogAsync(catalogURL, false);
//        yield return catalogHandle;

//        if (catalogHandle.Status == AsyncOperationStatus.Succeeded)
//        {
//            Debug.Log($"[SceneLoader] Catalog loaded successfully from {catalogURL}. Now loading scene...");

//            // Load the scene via Addressables
//            AsyncOperationHandle<SceneInstance> sceneHandle = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Single);
//            yield return sceneHandle;

//            if (sceneHandle.Status == AsyncOperationStatus.Succeeded)
//            {
//                Debug.Log($"[SceneLoader] Addressable scene '{sceneName}' loaded successfully.");
//            }
//            else
//            {
//                Debug.LogError($"[SceneLoader] Failed to load Addressable scene '{sceneName}'. Error: {sceneHandle.OperationException}");
//            }
//        }
//        else
//        {
//            Debug.LogError($"[SceneLoader] Failed to load catalog from {catalogURL}. Error: {catalogHandle.OperationException}");
//        }
//    }
//}

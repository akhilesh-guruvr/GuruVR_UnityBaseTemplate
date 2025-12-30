//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.SceneManagement;
//using System.Collections;

//public class LoadingSceneController : MonoBehaviour
//{
//    [SerializeField] private Slider loadingSlider; // Assign in Inspector: a UI Slider for progress (set min=0, max=1)
//    [SerializeField] private Text loadingText; // Optional: Assign in Inspector: a UI Text for percentage display

//    void Start()
//    {
//        Debug.Log("[LoadingSceneController] === START ===");
//        SceneLoaderUtility.ResetLoadingFlag();

//        // Validate UI references (optional, for debugging)
//        if (loadingSlider == null) Debug.LogWarning("[LoadingSceneController] LoadingSlider not assigned!");
//        if (loadingText == null) Debug.LogWarning("[LoadingSceneController] LoadingText not assigned!");

//        // Reset progress to 0
//        if (loadingSlider != null) loadingSlider.value = 0f;
//        if (loadingText != null) loadingText.text = "0%";

//        // Check for target scene data
//        if (string.IsNullOrEmpty(SceneLoaderUtility.TargetSceneName))
//        {
//            Debug.LogError("[LoadingSceneController] ✗✗✗ No target scene stored. Cannot proceed with loading.");
//            Debug.LogError("[LoadingSceneController] This means LoadScene() didn't properly set the target data!");

//            // Reset the loading flag since we can't proceed
//            SceneLoaderUtility.ResetLoadingFlag();

//            // Optionally, load a fallback scene (uncomment if you have a main menu)
//            // StartCoroutine(LoadFallbackScene("MainMenu"));
//            return;
//        }

//        Debug.Log($"[LoadingSceneController] ✓ Target scene data found:");
//        Debug.Log($"  - TargetSceneName: '{SceneLoaderUtility.TargetSceneName}'");
//        Debug.Log($"  - UseAddressable: {SceneLoaderUtility.TargetUseAddressable}");
//        Debug.Log($"  - CatalogURL: '{SceneLoaderUtility.TargetCatalogURL}'");

//        // Start the loading coroutine with progress callback
//        Debug.Log("[LoadingSceneController] Starting LoadSceneAsync coroutine...");
//        StartCoroutine(SceneLoaderUtility.LoadSceneAsync(
//            SceneLoaderUtility.TargetSceneName,
//            SceneLoaderUtility.TargetUseAddressable,
//            SceneLoaderUtility.TargetCatalogURL,
//            OnLoadingProgress
//        ));

//        Debug.Log("[LoadingSceneController] Coroutine started successfully");
//    }

//    private void OnLoadingProgress(float progress)
//    {
//        if (loadingSlider != null)
//        {
//            loadingSlider.value = progress;
//        }
//        if (loadingText != null)
//        {
//            int percentage = Mathf.RoundToInt(progress * 100f);
//            loadingText.text = $"{percentage}%";
//        }
//        Debug.Log($"[LoadingSceneController] Progress updated: {progress * 100f:F0}%");
//    }

//    // Optional fallback scene loader
//    private IEnumerator LoadFallbackScene(string fallbackSceneName)
//    {
//        Debug.LogWarning($"[LoadingSceneController] Loading fallback scene: {fallbackSceneName}");
//        yield return new WaitForSeconds(2f); // Show error message briefly
//        SceneManager.LoadSceneAsync(fallbackSceneName);
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

public class LoadingSceneController : MonoBehaviour
{
    public Slider progressBar; // optional UI slider

    private void Start()
    {
        StartCoroutine(LoadNow());
    }

    private IEnumerator LoadNow()
    {
        Debug.Log($"[LoadingSceneController] Loading scene: {SceneLoadData.targetScene}, Addressable={SceneLoadData.useAddressable}");

        if (SceneLoadData.useAddressable)
        {
            // Load catalog
            var catalogHandle = Addressables.LoadContentCatalogAsync(SceneLoadData.catalogURL, false);
            yield return catalogHandle;

            if (catalogHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("[Loading] Catalog load failed: " + catalogHandle.OperationException);
                yield break;
            }

            // Load addressable scene
            var sceneHandle = Addressables.LoadSceneAsync(SceneLoadData.targetScene, LoadSceneMode.Single);

            while (!sceneHandle.IsDone)
            {
                if (progressBar != null)
                    progressBar.value = sceneHandle.PercentComplete;

                yield return null;
            }

            if (sceneHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("[Loading] Scene load failed: " + sceneHandle.OperationException);
            }
        }
        else
        {
            // Load local scene
            var asyncOp = SceneManager.LoadSceneAsync(SceneLoadData.targetScene);

            while (!asyncOp.isDone)
            {
                if (progressBar != null)
                    progressBar.value = asyncOp.progress;

                yield return null;
            }
        }
    }
}

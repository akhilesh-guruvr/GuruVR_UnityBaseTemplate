using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoReturnOnLoadFreeze : MonoBehaviour
{
    public float freezeThreshold = 3f; // seconds without progress
    public float timeout = 15f;        // maximum wait time

    private float lastProgress = 0f;
    private float freezeTimer = 0f;
    private float totalTimer = 0f;

    void Update()
    {
        totalTimer += Time.deltaTime;

        // Seek any Slider in the scene (your loading bar)
        var slider = FindObjectOfType<UnityEngine.UI.Slider>();
        float currentProgress = slider != null ? slider.value : 0f;

        // Detect frozen progress
        if (Mathf.Approximately(currentProgress, lastProgress))
        {
            freezeTimer += Time.deltaTime;
        }
        else
        {
            freezeTimer = 0f; // progress changed → reset freeze timer
        }

        lastProgress = currentProgress;

        // If loading freezes → fail
        if (freezeTimer >= freezeThreshold)
        {
            Debug.LogError("[AutoReturnOnLoadFreeze] Loading froze — returning to previous scene");
            GoBack();
        }

        // If loading takes too long → fail
        if (totalTimer >= timeout)
        {
            Debug.LogError("[AutoReturnOnLoadFreeze] Loading timeout — returning to previous scene");
            GoBack();
        }
    }

    void GoBack()
    {
        int backIndex = SceneManager.GetActiveScene().buildIndex - 1;
        if (backIndex < 0) backIndex = 0;

        SceneManager.LoadScene(backIndex);
    }
}

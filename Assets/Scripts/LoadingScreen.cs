using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoadingScreen : MonoBehaviour
{
    public TMP_Text loadingText;
    public Slider progressBar; // Tùy chọn

    private static string targetSceneToLoad; // Scene cần load

    // Gọi để load scene
    public static void LoadScene(string sceneName)
    {
        targetSceneToLoad = sceneName;
        SceneManager.LoadScene("Loading Screen");
    }

    void Start()
    {
        if (!string.IsNullOrEmpty(targetSceneToLoad))
        {
            StartCoroutine(LoadTargetSceneAsync());
        }
    }

    private System.Collections.IEnumerator LoadTargetSceneAsync()
    {
        yield return null; // Đợi frame để giao diện hiển thị
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneToLoad);

        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            if (progressBar != null) progressBar.value = progress;
            if (loadingText != null) loadingText.text = $"Loading... {(progress * 100):0}%";
            yield return null;
        }
    }
}
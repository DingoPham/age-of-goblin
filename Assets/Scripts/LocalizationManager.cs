using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    private string currentLanguage = "English";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Khởi tạo ngôn ngữ từ PlayerPrefs (nếu có)
            currentLanguage = PlayerPrefs.GetString("Language", "English");
            Debug.Log($"LocalizationManager initialized with language: {currentLanguage}");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetLanguage(string language)
    {
        currentLanguage = language;
        Debug.Log($"LocalizationManager: Language set to {language}");
        // Thêm logic để cập nhật văn bản trong game (ví dụ: cập nhật Text components)
        // Ví dụ: Gửi sự kiện để các script khác cập nhật UI
    }

    public string GetCurrentLanguage()
    {
        return currentLanguage;
    }

    // Phương thức để tạo instance tự động nếu chưa tồn tại
    public static LocalizationManager GetInstance()
    {
        if (Instance == null)
        {
            GameObject localizationManagerObj = new GameObject("LocalizationManager");
            Instance = localizationManagerObj.AddComponent<LocalizationManager>();
            Debug.Log("LocalizationManager instance created automatically.");
        }
        return Instance;
    }
}
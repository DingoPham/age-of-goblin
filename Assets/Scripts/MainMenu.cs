using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public Button campaignButton; // Nút mở Campaign
    public Button inventoryButton; // Nút mở Inventory (nếu có)
    public Button shopButton; // Nút mở Shop (nếu có)
    public Button quitButton; // Nút thoát game (nếu có)
    public Button skirmishButton; // Nút mở Skirmish
    public Button loadGameButton; // Nút tải game

    // Quản lý âm thanh
    [SerializeField] private AudioSource backgroundMusic;
    [SerializeField] private AudioSource soundEffects;

    // Tham chiếu đến các hệ thống khác (nếu có trong MainMenu)
    [SerializeField] private GameSpeedManager gameSpeedManager; // Giữ lại nếu cần trong MainMenu

    // Quản lý UI cho Settings và Language
    [SerializeField] private Button openSettingsButton; // Nút để mở Settings Panel trong MainMenu
    [SerializeField] private GameObject settingsPanel; // Panel Settings
    [SerializeField] private Slider backgroundMusicSlider; // Slider điều chỉnh âm lượng nhạc nền
    [SerializeField] private Slider soundEffectSlider; // Slider điều chỉnh âm lượng hiệu ứng âm thanh
    [SerializeField] private Slider gameSpeedSlider; // Slider điều chỉnh tốc độ game
    [SerializeField] private Toggle showGridToggle; // Toggle hiển thị/ẩn lưới (chỉ lưu giá trị)
    [SerializeField] private Button languageButton; // Nút để mở Language Panel
    [SerializeField] private GameObject languagePanel; // Panel để chọn ngôn ngữ
    [SerializeField] private Button englishButton; // Nút chọn tiếng Anh
    [SerializeField] private Button vietnameseButton; // Nút chọn tiếng Việt
    [SerializeField] private Button closeLanguageButton; // Nút đóng Language Panel
    [SerializeField] private Button closeSettingsButton; // Nút đóng Settings

    void Start()
    {
        // Đảm bảo LocalizationManager được khởi tạo
        LocalizationManager.GetInstance();

        // Giữ nguyên logic hiện có của MainMenu
        if (campaignButton != null)
        {
            campaignButton.onClick.AddListener(OpenCampaign);
        }
        if (inventoryButton != null)
        {
            inventoryButton.onClick.AddListener(OpenInventory);
        }
        if (shopButton != null)
        {
            shopButton.onClick.AddListener(OpenShop);
        }
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }

        // Thêm sự kiện cho các nút mới
        if (skirmishButton != null)
        {
            skirmishButton.onClick.AddListener(OpenSkirmish);
        }
        if (loadGameButton != null)
        {
            loadGameButton.onClick.AddListener(LoadGame);
        }

        // Thêm logic cho Settings và Language
        UpdateReferences();

        // Khởi tạo âm thanh
        if (backgroundMusic != null) backgroundMusic.Play();
    }

    private void Update()
    {
        // Thêm logic xử lý nút Back (Escape) cho Settings và Language
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBackButton();
        }
    }

    #region Campaign Close/Open
    public void OpenCampaign()
    {
        LoadingScreen.LoadScene("Campaign");
    }
    public void CloseCampaign()
    {
        LoadingScreen.LoadScene("Main Menu");
    }
    #endregion

    #region Inventory Close/Open
    public void OpenInventory()
    {
        LoadingScreen.LoadScene("Inventory"); // Load thay thế (Single mode)
    }

    public void CloseInventory() // Gọi từ scene Inventory, không cần ở đây
    {
        LoadingScreen.LoadScene("Main Menu");
    }
    #endregion

    #region Shop Close/Open
    public void OpenShop()
    {
        LoadingScreen.LoadScene("Shop"); // Load thay thế (Single mode)
    }

    public void CloseShop() // Gọi từ scene Shop, không cần ở đây
    {
        LoadingScreen.LoadScene("Main Menu");
    }
    #endregion

    #region Battle Open/Close
    public void OpenBattle()
    {
        LoadingScreen.LoadScene("Battle");
    }
    public void CloseBattle()
    {
        LoadingScreen.LoadScene("Campaign");
    }
    #endregion

    #region Skirmish Open
    public void OpenSkirmish()
    {
        LoadingScreen.LoadScene("Skirmish");
    }
    #endregion

    #region Load Game
    public void LoadGame()
    {
        // Mở scene Battle
        LoadingScreen.LoadScene("Battle");
        // Logic tải game có thể được xử lý trong GameManager sau khi scene Battle được tải
        Debug.Log("Loading game...");
    }
    #endregion

    public void QuitGame()
    {
        Application.Quit();
    }

    // Các phương thức liên quan đến Settings và Language

    private void UpdateReferences()
    {
        // Cập nhật các tham chiếu (nếu có trong MainMenu)
        if (gameSpeedManager == null) gameSpeedManager = FindObjectOfType<GameSpeedManager>();

        // Cập nhật trạng thái UI
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (languagePanel != null) languagePanel.SetActive(false);

        // Gán sự kiện cho các nút
        if (openSettingsButton != null)
        {
            openSettingsButton.onClick.RemoveAllListeners();
            openSettingsButton.onClick.AddListener(OpenSettings);
        }
        if (languageButton != null)
        {
            languageButton.onClick.RemoveAllListeners();
            languageButton.onClick.AddListener(OpenLanguagePanel);
        }
        if (englishButton != null)
        {
            englishButton.onClick.RemoveAllListeners();
            englishButton.onClick.AddListener(() => SetLanguage("English"));
        }
        if (vietnameseButton != null)
        {
            vietnameseButton.onClick.RemoveAllListeners();
            vietnameseButton.onClick.AddListener(() => SetLanguage("Vietnamese"));
        }
        if (closeLanguageButton != null)
        {
            closeLanguageButton.onClick.RemoveAllListeners();
            closeLanguageButton.onClick.AddListener(CloseLanguagePanel);
        }
        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.RemoveAllListeners();
            closeSettingsButton.onClick.AddListener(CloseSettings);
        }

        // Khởi tạo lại Settings
        InitializeSettings();
    }

    // Xử lý nút Back
    private void HandleBackButton()
    {
        if (languagePanel != null && languagePanel.activeSelf)
        {
            CloseLanguagePanel();
            return;
        }
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            CloseSettings();
            return;
        }
        // Nếu không có panel nào mở, thoát ứng dụng
        QuitGame();
    }

    // Mở Settings
    private void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    // Đóng Settings
    private void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    // Mở Language Panel
    private void OpenLanguagePanel()
    {
        if (languagePanel != null)
        {
            languagePanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false); // Ẩn SettingsPanel khi mở LanguagePanel
        }
    }

    // Đóng Language Panel
    private void CloseLanguagePanel()
    {
        if (languagePanel != null)
        {
            languagePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true); // Hiển thị lại SettingsPanel
        }
    }

    // Khởi tạo giá trị cho các thành phần trong Settings
    private void InitializeSettings()
    {
        // Background Music Slider
        if (backgroundMusicSlider != null && backgroundMusic != null)
        {
            backgroundMusicSlider.value = PlayerPrefs.GetFloat("BackgroundMusicVolume", 1f);
            backgroundMusic.volume = backgroundMusicSlider.value;
            backgroundMusicSlider.onValueChanged.RemoveAllListeners();
            backgroundMusicSlider.onValueChanged.AddListener(SetBackgroundMusicVolume);
        }

        // Sound Effect Slider
        if (soundEffectSlider != null && soundEffects != null)
        {
            soundEffectSlider.value = PlayerPrefs.GetFloat("SoundEffectVolume", 1f);
            soundEffects.volume = soundEffectSlider.value;
            soundEffectSlider.onValueChanged.RemoveAllListeners();
            soundEffectSlider.onValueChanged.AddListener(SetSoundEffectVolume);
        }

        // Game Speed Slider
        if (gameSpeedSlider != null && gameSpeedManager != null)
        {
            gameSpeedSlider.minValue = gameSpeedManager.MinSpeed;
            gameSpeedSlider.maxValue = gameSpeedManager.MaxSpeed;
            gameSpeedSlider.value = PlayerPrefs.GetFloat("GameSpeed", 1f);
            gameSpeedManager.SetGameSpeed(gameSpeedSlider.value);
            gameSpeedSlider.onValueChanged.RemoveAllListeners();
            gameSpeedSlider.onValueChanged.AddListener(SetGameSpeed);
        }

        // Show Grid Toggle (chỉ lưu giá trị vào PlayerPrefs)
        if (showGridToggle != null)
        {
            showGridToggle.isOn = PlayerPrefs.GetInt("ShowGrid", 1) == 1;
            showGridToggle.onValueChanged.RemoveAllListeners();
            showGridToggle.onValueChanged.AddListener(SetGridVisibility);
        }
    }

    // Điều chỉnh âm lượng Background Music
    private void SetBackgroundMusicVolume(float volume)
    {
        if (backgroundMusic != null)
        {
            backgroundMusic.volume = volume;
            PlayerPrefs.SetFloat("BackgroundMusicVolume", volume);
            PlayerPrefs.Save();
        }
    }

    // Điều chỉnh âm lượng Sound Effect
    private void SetSoundEffectVolume(float volume)
    {
        if (soundEffects != null)
        {
            soundEffects.volume = volume;
            PlayerPrefs.SetFloat("SoundEffectVolume", volume);
            PlayerPrefs.Save();
        }
    }

    // Điều chỉnh tốc độ game
    private void SetGameSpeed(float speed)
    {
        if (gameSpeedManager != null)
        {
            gameSpeedManager.SetGameSpeed(speed);
            PlayerPrefs.SetFloat("GameSpeed", speed);
            PlayerPrefs.Save();
        }
    }

    // Lưu giá trị Show Grid vào PlayerPrefs
    private void SetGridVisibility(bool isVisible)
    {
        PlayerPrefs.SetInt("ShowGrid", isVisible ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"ShowGrid set to: {isVisible} (saved to PlayerPrefs)");
    }

    // Thay đổi ngôn ngữ
    private void SetLanguage(string language)
    {
        PlayerPrefs.SetString("Language", language); // Lưu ngôn ngữ dưới dạng string
        PlayerPrefs.Save();
        Debug.Log($"Language changed to: {language}");
        UpdateLanguage(language);
        CloseLanguagePanel(); // Đóng Language Panel sau khi chọn ngôn ngữ
    }

    // Phương thức cập nhật ngôn ngữ
    private void UpdateLanguage(string language)
    {
        // Sử dụng GetInstance để đảm bảo instance tồn tại
        LocalizationManager.GetInstance().SetLanguage(language);
    }
}
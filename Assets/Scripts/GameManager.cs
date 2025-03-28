using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Trạng thái game
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver
    }
    private GameState currentState = GameState.MainMenu;

    // Tham chiếu đến các hệ thống khác
    [SerializeField] private GridMapController gridMapController;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private BuildingManager buildingManager;
    [SerializeField] private GameSpeedManager gameSpeedManager;
    [SerializeField] private OutlineManager outlineManager;
    [SerializeField] private UnitInfoUI unitInfoUI;

    // Quản lý âm thanh
    [SerializeField] private AudioSource backgroundMusic;
    [SerializeField] private AudioSource soundEffects;

    // Quản lý UI
    [SerializeField] private GameObject pauseMenuUI; // UI menu tạm dừng
    [SerializeField] private Image pauseOverlay; // Overlay để chặn tương tác UI
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private Button pauseButton; // Nút Pause
    [SerializeField] private Button resumeButton; // Nút Resume trong PauseMenuUI
    [SerializeField] private Button restartButton; // Nút Restart trong PauseMenuUI
    [SerializeField] private Button settingsButton; // Nút Settings trong PauseMenuUI
    [SerializeField] private GameObject settingsPanel; // Panel Settings
    [SerializeField] private Slider backgroundMusicSlider; // Slider điều chỉnh âm lượng nhạc nền
    [SerializeField] private Slider soundEffectSlider; // Slider điều chỉnh âm lượng hiệu ứng âm thanh
    [SerializeField] private Slider gameSpeedSlider; // Slider điều chỉnh tốc độ game
    [SerializeField] private Toggle showGridToggle; // Toggle hiển thị/ẩn lưới
    [SerializeField] private Button languageButton; // Nút để mở Language Panel
    [SerializeField] private GameObject languagePanel; // Panel để chọn ngôn ngữ
    [SerializeField] private Button englishButton; // Nút chọn tiếng Anh
    [SerializeField] private Button vietnameseButton; // Nút chọn tiếng Việt
    [SerializeField] private Button closeLanguageButton; // Nút đóng Language Panel
    [SerializeField] private Button closeSettingsButton; // Nút đóng Settings

    // Danh sách các đơn vị trong game
    private List<Unit> allUnits = new List<Unit>();

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Đảm bảo LocalizationManager được khởi tạo
        LocalizationManager.GetInstance();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateReferences();
        if (currentState != GameState.MainMenu)
        {
            StartGame();
        }
        Debug.Log("Scene loaded, references updated.");
    }

    private void Start()
    {
        UpdateReferences();

        // Khởi tạo trạng thái game
        if (SceneManager.GetActiveScene().name != "Campaign")
        {
            StartGame();
        }

        // Khởi tạo âm thanh
        if (backgroundMusic != null) backgroundMusic.Play();
    }

    private void UpdateReferences()
    {
        // Cập nhật các tham chiếu
        if (gridMapController == null) gridMapController = FindObjectOfType<GridMapController>();
        if (turnManager == null) turnManager = FindObjectOfType<TurnManager>();
        if (buildingManager == null) buildingManager = FindObjectOfType<BuildingManager>();
        if (gameSpeedManager == null) gameSpeedManager = FindObjectOfType<GameSpeedManager>();
        if (outlineManager == null) outlineManager = FindObjectOfType<OutlineManager>();
        if (unitInfoUI == null) unitInfoUI = FindObjectOfType<UnitInfoUI>();

        // Cập nhật trạng thái UI và các hệ thống
        if (pauseOverlay != null) pauseOverlay.gameObject.SetActive(false);
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (languagePanel != null) languagePanel.SetActive(false); // Đảm bảo Language Panel tắt ban đầu
        if (pauseButton != null) pauseButton.gameObject.SetActive(true);

        // Gán sự kiện cho các nút
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(OnPauseButtonClicked);
        }
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
        }
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
        }
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OpenSettings);
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

    private void Update()
    {
        // Phát hiện nút Back trên Android (ánh xạ thành phím Escape trong Unity)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBackButton();
        }
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
        if (currentState == GameState.Playing)
        {
            PauseGame();
        }
        else if (currentState == GameState.Paused)
        {
            ResumeGame();
        }
        else if (currentState == GameState.MainMenu)
        {
            Application.Quit();
            Debug.Log("Application Quit");
        }
    }

    // Phương thức được gọi khi nhấn nút Pause
    private void OnPauseButtonClicked()
    {
        if (currentState == GameState.Playing)
        {
            PauseGame();
        }
    }

    public void StartGame()
    {
        currentState = GameState.Playing;
        Time.timeScale = 1f;
        if (pauseOverlay != null) pauseOverlay.gameObject.SetActive(false);
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (languagePanel != null) languagePanel.SetActive(false);
        if (pauseButton != null) pauseButton.gameObject.SetActive(true);
        Debug.Log("Game Started");
    }

    public void PauseGame()
    {
        if (currentState != GameState.Playing) return;

        currentState = GameState.Paused;
        Time.timeScale = 0f;
        if (pauseOverlay != null) pauseOverlay.gameObject.SetActive(true);
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        if (backgroundMusic != null) backgroundMusic.Pause();
        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;

        currentState = GameState.Playing;
        Time.timeScale = 1f;
        if (pauseOverlay != null) pauseOverlay.gameObject.SetActive(false);
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (languagePanel != null) languagePanel.SetActive(false);
        if (backgroundMusic != null) backgroundMusic.Play();
        Debug.Log("Game Resumed");
    }

    public void GameOver(bool playerWon)
    {
        currentState = GameState.GameOver;
        Time.timeScale = 0f;
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
            Debug.Log(playerWon ? "Player Won!" : "Player Lost!");
        }
        if (pauseButton != null) pauseButton.gameObject.SetActive(false);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Debug.Log("Game Restarted");
    }

    public void ExitGame()
    {
        SceneManager.LoadScene("Campaign");
        currentState = GameState.MainMenu;
        Debug.Log("Returned to Main Menu");
    }

    // Mở Settings
    private void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false); // Ẩn PauseMenuUI khi mở Settings
        }
    }

    // Đóng Settings
    private void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            if (pauseMenuUI != null) pauseMenuUI.SetActive(true); // Hiển thị lại PauseMenuUI
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

        // Show Grid Toggle
        if (showGridToggle != null)
        {
            showGridToggle.isOn = PlayerPrefs.GetInt("ShowGrid", 1) == 1;
            if (gridMapController != null)
            {
                gridMapController.SetGridVisibility(showGridToggle.isOn);
            }
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

    // Hiển thị/ẩn lưới
    private void SetGridVisibility(bool isVisible)
    {
        if (gridMapController == null)
        {
            gridMapController = FindObjectOfType<GridMapController>();
            if (gridMapController == null)
            {
                Debug.LogError("GridMapController not found in the scene!");
                return;
            }
        }
        gridMapController.SetGridVisibility(isVisible);
        PlayerPrefs.SetInt("ShowGrid", isVisible ? 1 : 0);
        PlayerPrefs.Save();
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

    // Getter để các script khác kiểm tra trạng thái tạm dừng
    public bool IsPaused()
    {
        return currentState == GameState.Paused;
    }

    // Quản lý đơn vị
    public void RegisterUnit(Unit unit)
    {
        if (!allUnits.Contains(unit))
        {
            allUnits.Add(unit);
            Debug.Log($"Unit {unit.UnitName} registered with GameManager");
        }
    }

    public void UnregisterUnit(Unit unit)
    {
        if (allUnits.Contains(unit))
        {
            allUnits.Remove(unit);
            Debug.Log($"Unit {unit.UnitName} unregistered from GameManager");
        }
    }

    public List<Unit> GetAllUnits()
    {
        return allUnits;
    }

    // Kiểm tra điều kiện thắng/thua
    public void CheckWinLoseConditions()
    {
        bool playerHasUnits = false;
        foreach (Unit unit in allUnits)
        {
            if (unit.tag == "PlayerUnit")
            {
                playerHasUnits = true;
                break;
            }
        }

        if (!playerHasUnits)
        {
            GameOver(false);
        }
    }

    // Quản lý âm thanh
    public void PlaySoundEffect(AudioClip clip)
    {
        if (soundEffects != null && clip != null)
        {
            soundEffects.PlayOneShot(clip);
        }
    }

    // Lưu và tải game
    public void SaveGame()
    {
        PlayerPrefs.SetInt("TurnCount", turnManager != null ? turnManager.GetCurrentTurn() : 0);
        for (int i = 0; i < allUnits.Count; i++)
        {
            Unit unit = allUnits[i];
            PlayerPrefs.SetString($"Unit_{i}_Name", unit.UnitName);
            PlayerPrefs.SetFloat($"Unit_{i}_PosX", unit.transform.position.x);
            PlayerPrefs.SetFloat($"Unit_{i}_PosY", unit.transform.position.y);
            PlayerPrefs.SetInt($"Unit_{i}_Health", unit.Health);
        }
        PlayerPrefs.SetInt("UnitCount", allUnits.Count);
        PlayerPrefs.Save();
        Debug.Log("Game Saved");
    }

    public void LoadGame()
    {
        if (turnManager != null)
        {
            turnManager.SetCurrentTurn(PlayerPrefs.GetInt("TurnCount", 0));
        }
        int unitCount = PlayerPrefs.GetInt("UnitCount", 0);
        Debug.Log($"Loading {unitCount} units...");
    }

    // Truy cập các hệ thống khác
    public GridMapController GetGridMapController() => gridMapController;
    public TurnManager GetTurnManager() => turnManager;
    public BuildingManager GetBuildingManager() => buildingManager;
    public GameSpeedManager GetGameSpeedManager() => gameSpeedManager;
    public OutlineManager GetOutlineManager() => outlineManager;
    public UnitInfoUI GetUnitInfoUI() => unitInfoUI;
}
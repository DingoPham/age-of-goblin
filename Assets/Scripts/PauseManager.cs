using UnityEngine;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    [SerializeField] private Image pauseOverlay; // Overlay để chặn UI
    private bool isPaused = false;

    public bool IsPaused => isPaused;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (pauseOverlay != null)
        {
            pauseOverlay.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Chỉ xử lý input tạm dừng/tiếp tục
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // Dừng hoạt động dựa trên thời gian
        if (pauseOverlay != null)
        {
            pauseOverlay.gameObject.SetActive(true); // Hiển thị overlay để chặn UI
        }
        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Tiếp tục hoạt động
        if (pauseOverlay != null)
        {
            pauseOverlay.gameObject.SetActive(false); // Ẩn overlay
        }
        Debug.Log("Game Resumed");
    }
}
using UnityEngine;
using System;

public class GameSpeedManager : MonoBehaviour
{
    public static GameSpeedManager Instance { get; private set; }

    [SerializeField] private float gameSpeed = 1f; // Tốc độ hành động mặc định
    [SerializeField] private float minSpeed = 0.1f; // Giới hạn tốc độ tối thiểu
    [SerializeField] private float maxSpeed = 5f;   // Giới hạn tốc độ tối đa

    // Sự kiện để thông báo khi tốc độ game thay đổi
    public event Action<float> OnGameSpeedChanged;

    private void Awake()
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

        // Khởi tạo giá trị từ PlayerPrefs
        gameSpeed = PlayerPrefs.GetFloat("GameSpeed", gameSpeed);
        gameSpeed = Mathf.Clamp(gameSpeed, minSpeed, maxSpeed);
    }

    // Phương thức để lấy tốc độ game
    public float GetGameSpeed()
    {
        return Mathf.Clamp(gameSpeed, minSpeed, maxSpeed);
    }

    // Phương thức để thiết lập tốc độ game
    public void SetGameSpeed(float newSpeed)
    {
        float previousSpeed = gameSpeed;
        gameSpeed = Mathf.Clamp(newSpeed, minSpeed, maxSpeed);

        if (previousSpeed != gameSpeed)
        {
            // Lưu giá trị vào PlayerPrefs
            PlayerPrefs.SetFloat("GameSpeed", gameSpeed);
            PlayerPrefs.Save();

            // Kích hoạt sự kiện để thông báo các script khác
            OnGameSpeedChanged?.Invoke(gameSpeed);
            Debug.Log($"Game speed updated to: {gameSpeed}");
        }
    }

    // Getter cho minSpeed và maxSpeed (nếu cần điều chỉnh từ script khác)
    public float MinSpeed => minSpeed;
    public float MaxSpeed => maxSpeed;
}
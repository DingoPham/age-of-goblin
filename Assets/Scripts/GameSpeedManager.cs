using UnityEngine;

public class GameSpeedManager : MonoBehaviour
{
    public static GameSpeedManager Instance { get; private set; }

    [SerializeField] private float gameSpeed = 1f; // Tốc độ hành động mặc định (1 = bình thường, >1 = nhanh, <1 = chậm)
    [SerializeField] private float minSpeed = 0.1f; // Giới hạn tốc độ tối thiểu
    [SerializeField] private float maxSpeed = 5f;   // Giới hạn tốc độ tối đa

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

    // Phương thức để lấy tốc độ game (dùng trong các script khác)
    public float GetGameSpeed()
    {
        return Mathf.Clamp(gameSpeed, minSpeed, maxSpeed);
    }

    // Phương thức để thiết lập tốc độ game (dùng cho UI Settings)
    public void SetGameSpeed(float newSpeed)
    {
        gameSpeed = Mathf.Clamp(newSpeed, minSpeed, maxSpeed);
        Debug.Log($"Game speed updated to: {gameSpeed}");
    }
}
using UnityEngine;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    private int currentTurn = 1; // Lượt hiện tại (1 cho Player 1, 2 cho Player 2, v.v.)
    private List<Unit> units = new List<Unit>(); // Danh sách tất cả các đơn vị

    // Singleton để dễ dàng truy cập từ bất kỳ đâu
    public static TurnManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Giữ TurnManager khi chuyển scene (nếu cần)
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Đăng ký đơn vị khi khởi tạo
    public void RegisterUnit(Unit unit)
    {
        if (!units.Contains(unit))
        {
            units.Add(unit);
            Debug.Log($"Unit registered: {unit.name}");
        }
    }

    // Chuyển sang lượt mới và reset tất cả đơn vị
    public void NextTurn()
    {
        currentTurn = (currentTurn % 2) + 1; // Chuyển giữa Player 1 và Player 2
        Debug.Log($"Turn {currentTurn} started!");

        // Reset move range cho tất cả đơn vị
        foreach (Unit unit in units)
        {
            if (unit != null) // Kiểm tra để tránh lỗi nếu unit bị hủy
            {
                unit.ResetMoveRange();
            }
        }
    }

    // Lấy lượt hiện tại (dùng để kiểm tra logic nếu cần)
    public int GetCurrentTurn()
    {
        return currentTurn;
    }
}
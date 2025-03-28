using UnityEngine;
using System.Collections.Generic;
using System;

public class TurnManager : MonoBehaviour
{
    private int currentTurn = 1; // Lượt hiện tại (1 cho Player 1, 2 cho Player 2, v.v.)
    private int totalPlayers = 2; // Số lượng người chơi (có thể điều chỉnh)
    private List<Unit> units = new List<Unit>(); // Danh sách tất cả các đơn vị

    // Sự kiện khi lượt thay đổi
    public event Action<int> OnTurnChanged;

    // Singleton để dễ dàng truy cập từ bất kỳ đâu
    public static TurnManager Instance { get; private set; }

    void Awake()
    {
        // Đảm bảo Instance được gán trước
        Instance = this;

        if (Instance != this)
        {
            Debug.LogWarning("Another instance of TurnManager already exists! Destroying this one.");
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject); // Giữ TurnManager khi chuyển scene (nếu cần)
    }

    // Đăng ký đơn vị khi khởi tạo
    public void RegisterUnit(Unit unit)
    {
        if (unit == null)
        {
            Debug.LogWarning("Attempted to register a null unit!");
            return;
        }

        if (!units.Contains(unit))
        {
            units.Add(unit);
            Debug.Log($"Unit registered: {unit.UnitName}");
        }
    }

    // Xóa đơn vị khỏi danh sách khi bị hủy
    public void UnregisterUnit(Unit unit)
    {
        if (unit == null)
        {
            Debug.LogWarning("Attempted to unregister a null unit!");
            return;
        }

        if (units.Contains(unit))
        {
            units.Remove(unit);
            Debug.Log($"Unit unregistered: {unit.UnitName}");
        }
    }

    // Lấy danh sách các đơn vị (loại bỏ các đơn vị null nếu có)
    public List<Unit> GetUnits()
    {
        units.RemoveAll(unit => unit == null); // Xóa các đơn vị đã bị hủy (null)
        return units;
    }

    // Chuyển sang lượt mới và reset tất cả đơn vị
    public void NextTurn()
    {
        currentTurn = (currentTurn % totalPlayers) + 1; // Chuyển giữa các người chơi
        Debug.Log($"Turn {currentTurn} started!");

        // Reset move range cho tất cả đơn vị
        foreach (Unit unit in GetUnits()) // Sử dụng GetUnits() để đảm bảo không có unit null
        {
            unit.ResetMoveRange();
        }

        // Kích hoạt sự kiện khi lượt thay đổi
        OnTurnChanged?.Invoke(currentTurn);
    }

    // Lấy lượt hiện tại (dùng để kiểm tra logic nếu cần)
    public int GetCurrentTurn()
    {
        return currentTurn;
    }

    // Thiết lập lượt hiện tại
    public void SetCurrentTurn(int turn)
    {
        if (turn < 1)
        {
            Debug.LogWarning($"Invalid turn value: {turn}. Turn must be at least 1.");
            turn = 1;
        }
        currentTurn = turn;
        Debug.Log($"Turn set to {currentTurn}");

        // Kích hoạt sự kiện khi lượt thay đổi
        OnTurnChanged?.Invoke(currentTurn);
    }

    // Thiết lập số lượng người chơi (nếu cần mở rộng)
    public void SetTotalPlayers(int players)
    {
        if (players < 2)
        {
            Debug.LogWarning($"Total players must be at least 2. Received: {players}");
            players = 2;
        }
        totalPlayers = players;
        Debug.Log($"Total players set to {totalPlayers}");
    }
}
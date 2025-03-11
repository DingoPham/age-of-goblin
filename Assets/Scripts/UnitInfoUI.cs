using UnityEngine;
using TMPro;

public class UnitInfoUI : MonoBehaviour
{
    [SerializeField] private TMP_Text unitNameText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text moveSpeedText;
    [SerializeField] private TMP_Text moveRangeText;
    [SerializeField] private TMP_Text remainingMoveRangeText;
    [SerializeField] private TMP_Text hasMovedText;

    public void UpdateUI(Unit unit)
    {
        UnitInfo info = unit.GetUnitInfo();

        // Kiểm tra null và cập nhật UI
        if (unitNameText == null) Debug.LogError("unitNameText is not assigned in UnitInfoUI!");
        else unitNameText.text = $"Tên: {info.UnitName}";

        if (healthText == null) Debug.LogError("healthText is not assigned in UnitInfoUI!");
        else healthText.text = $"Máu: {info.Health}";

        if (attackText == null) Debug.LogError("attackText is not assigned in UnitInfoUI!");
        else attackText.text = $"Sát thương: {info.Attack}";

        if (moveRangeText == null) Debug.LogError("moveRangeText is not assigned in UnitInfoUI!");
        else moveRangeText.text = $"Phạm vi: {info.MoveRange}";

        if (remainingMoveRangeText == null) Debug.LogError("remainingMoveRangeText is not assigned in UnitInfoUI!");
        else remainingMoveRangeText.text = $"Số ô còn lại: {info.RemainingMoveRange}";

        if (hasMovedText == null) Debug.LogError("hasMovedText is not assigned in UnitInfoUI!");
        else hasMovedText.text = $"Đã di chuyển: {(info.HasMoved ? "Có" : "Không")}";
    }
}
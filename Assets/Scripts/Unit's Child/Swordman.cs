using UnityEngine;

public class Swordman : Unit
{
    [SerializeField] private int moveRange = 5; // Phạm vi di chuyển riêng
    [SerializeField] private int health = 100; // Máu riêng
    [SerializeField] private int attack = 15; // Sát thương riêng
    [SerializeField] private int defense = 5; // Phòng thủ riêng

    private int currentHealth; // Lưu trữ máu hiện tại

    public override string UnitName => "Swordman";
    public override int Health
    {
        get => currentHealth;
        set => currentHealth = value;
    }
    public override int Attack => attack;
    public override int Defense => defense;
    public override int MoveRange => moveRange;

    void Start()
    {
        base.Start(); // Gọi Start của lớp cha
        currentHealth = health; // Khởi tạo máu
    }

    protected override void TakeDeepSeaDamage()
    {
        Health -= (int)(deepSeaDamagePerTick * 0.8f); // Giảm 20% damage
        Debug.Log($"{UnitName} takes {(int)(deepSeaDamagePerTick * 0.8f)} damage from DeepSea! Health: {Health}");
        if (Health <= 0)
        {
            Debug.Log($"{UnitName} has been destroyed by DeepSea!");
            Destroy(gameObject);
        }
    }

    public override void OnTouch()
    {
        base.OnTouch();
        Debug.Log($"Swordman special touch logic!");
    }
}
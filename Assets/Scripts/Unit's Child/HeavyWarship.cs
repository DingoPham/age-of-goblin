using UnityEngine;

public class HeavyWarship : Unit
{
    [SerializeField] private int moveRange = 4;
    [SerializeField] private int health = 180;
    [SerializeField] private int attack = 30;
    [SerializeField] private int defense = 15;

    private int currentHealth;

    public override string UnitName => "Heavy Warship";
    public override int Health
    {
        get => currentHealth;
        set => currentHealth = value;
    }
    public override int Attack => attack;
    public override int Defense => defense;
    public override int MoveRange => moveRange;

    protected void Start()
    {
        base.Start();
        currentHealth = health;
    }

    protected override TerrainEffect GetTerrainEffect(TerrainType terrainType)
    {
        switch (terrainType)
        {
            case TerrainType.Sea:
                return new TerrainEffect(0f, 0f, false); // Di chuyển bình thường trên biển
            case TerrainType.DeepSea:
                return new TerrainEffect(0f, 0f, false); // Không giảm tốc độ, không chịu sát thương
            default:
                return new TerrainEffect(0f, 0f, true); // Không thể di chuyển trên đất liền
        }
    }

    protected override void TakeDeepSeaDamage()
    {
        // Thuyền chiến không chịu sát thương từ DeepSea
        Debug.Log($"{UnitName} is a warship and takes no damage from DeepSea!");
    }
}
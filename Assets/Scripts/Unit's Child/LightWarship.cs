using UnityEngine;

public class LightWarship : Unit
{
    [SerializeField] private int moveRange = 5;
    [SerializeField] private int health = 130;
    [SerializeField] private int attack = 20;
    [SerializeField] private int defense = 10;

    private int currentHealth;

    public override string UnitName => "Light Warship";
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

    // Ghi đè AdjustTerrainEffect để điều chỉnh moveCost cho tàu chiến
    protected override TerrainEffect AdjustTerrainEffect(TerrainEffect effect, TerrainType terrainType)
    {
        if (terrainType == TerrainType.Sea)
        {
            effect.MoveCost = 1f; // Di chuyển bình thường trên biển
        }
        else if (terrainType == TerrainType.DeepSea)
        {
            effect.MoveCost = 1.2f; // Di chuyển hơi chậm hơn trên DeepSea
        }
        else
        {
            effect.MoveCost = float.MaxValue; // Không thể di chuyển trên đất liền
        }

        return effect;
    }

    protected override void TakeDeepSeaDamage()
    {
        // Thuyền chiến không chịu sát thương từ DeepSea
        Debug.Log($"{UnitName} is a warship and takes no damage from DeepSea!");
    }
}
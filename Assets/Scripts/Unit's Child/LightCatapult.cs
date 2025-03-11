using UnityEngine;

public class LightCatapult : Unit
{
    [SerializeField] private int moveRange = 2;
    [SerializeField] private int health = 150;
    [SerializeField] private int attack = 25;
    [SerializeField] private int defense = 10;

    private int currentHealth;

    public override string UnitName => "Light Catapult";
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
            case TerrainType.Plain:
                return new TerrainEffect(0f, 0f, false);
            case TerrainType.Forest:
                return new TerrainEffect(-0.1f, 0.15f, false);
            case TerrainType.Mountain:
                return new TerrainEffect(0.2f, 0.3f, false);
            case TerrainType.Sea:
                return new TerrainEffect(0f, 0f, true);
            case TerrainType.DeepSea:
                return new TerrainEffect(-0.3f, -0.2f, false);
            default:
                return base.GetTerrainEffect(terrainType);
        }
    }

    protected override void TakeDeepSeaDamage()
    {
        Health -= deepSeaDamagePerTick;
        Debug.Log($"{UnitName} takes {deepSeaDamagePerTick} damage from DeepSea! Health: {Health}");
        if (Health <= 0)
        {
            Debug.Log($"{UnitName} has been destroyed by DeepSea!");
            Destroy(gameObject);
        }
    }
}
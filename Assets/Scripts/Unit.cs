using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public abstract class Unit : MonoBehaviour
{
    [SerializeField] protected GameObject moveRangeHighlightPrefab; // Prefab cho ô highlight phạm vi di chuyển
    [SerializeField] protected GridMapController gridMapController; // Tham chiếu đến GridMapController
    [SerializeField] protected string highlightHexColor = "#00FF00"; // Mã màu Hex (mặc định là xanh lá)
    [SerializeField] protected float highlightAlpha = 0.5f; // Độ trong suốt (0 = trong suốt, 1 = mờ đục)
    [SerializeField] protected LayerMask unitLayer; // Layer của đơn vị để kiểm tra chồng tréo

    protected Color moveRangeHighlightColor; // Màu cuối cùng sau khi áp dụng Hex và alpha
    protected bool isSelected = false; // Trạng thái được chọn
    protected Vector3 targetPosition; // Vị trí đích để di chuyển
    protected Vector3 startPosition; // Vị trí ban đầu
    protected bool isMoving = false; // Trạng thái đang di chuyển
    protected SpriteRenderer spriteRenderer; // SpriteRenderer của unit
    protected int remainingMoveRange; // Số ô còn lại
    protected GameObject moveRangeHighlights; // Container cho các ô highlight
    protected bool hasMoved = false; // Theo dõi di chuyển

    // Thêm biến để xác định kích thước đơn vị (2 ô theo chiều ngang)
    [SerializeField] protected bool isTwoTileWide = false; // Đơn vị chiếm 2 ô theo chiều ngang
    protected Vector3Int[] occupiedTiles; // Lưu các ô mà đơn vị đang chiếm

    // Thêm biến tạm để lưu hiệu ứng địa hình hiện tại
    protected TerrainEffect currentTerrainEffect;
    protected TerrainType currentTerrainType; // Lưu loại địa hình hiện tại để kiểm tra DeepSea

    // Biến để theo dõi mất máu mỗi lượt trên DeepSea
    protected float deepSeaDamageTimer = 0f;
    protected float deepSeaDamageInterval = 1f; // Thời gian giữa các lần mất máu (sẽ bị ảnh hưởng bởi game speed)
    protected const int deepSeaDamagePerTick = 5; // Mất 5 máu mỗi lần

    // Thời gian cố định cho animation di chuyển (sẽ được điều chỉnh bởi game speed)
    private const float MOVE_ANIMATION_DURATION = 0.5f;

    // Thuộc tính ảo để các lớp con định nghĩa
    public abstract string UnitName { get; }
    public abstract int Health { get; set; }
    public abstract int Attack { get; }
    public abstract int Defense { get; }
    public abstract int MoveRange { get; }

    protected void Start()
    {
        // Chuyển mã Hex thành Color và áp dụng độ trong suốt
        if (!ColorUtility.TryParseHtmlString(highlightHexColor, out moveRangeHighlightColor))
        {
            Debug.LogWarning($"Invalid Hex Color: {highlightHexColor}. Using default green.");
            moveRangeHighlightColor = Color.green; // Mặc định nếu mã Hex không hợp lệ
        }
        moveRangeHighlightColor.a = Mathf.Clamp01(highlightAlpha); // Áp dụng độ trong suốt

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"{this.GetType().Name} needs a SpriteRenderer!");
        }

        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            Debug.LogWarning($"Added BoxCollider2D to {this.GetType().Name}. Adjust size if needed!");
            if (isTwoTileWide)
            {
                Tilemap tilemap = gridMapController.GetTilemap();
                if (tilemap != null)
                {
                    collider.size = new Vector2(tilemap.cellSize.x * 2, tilemap.cellSize.y); // Kích thước 2 ô ngang
                }
            }
            else
            {
                collider.size = new Vector2(1f, 1f); // Kích thước mặc định 1 ô
            }
        }
        else
        {
            BoxCollider2D collider = GetComponent<BoxCollider2D>();
            if (isTwoTileWide)
            {
                Tilemap tilemap = gridMapController.GetTilemap();
                if (tilemap != null)
                {
                    collider.size = new Vector2(tilemap.cellSize.x * 2, tilemap.cellSize.y); // Kích thước 2 ô ngang
                }
            }
        }

        // Gọi OutlineManager để thêm outline
        if (OutlineManager.Instance != null)
        {
            OutlineManager.Instance.AddOutline(this);
        }
        else
        {
            Debug.LogWarning("OutlineManager not found in scene!");
        }

        remainingMoveRange = MoveRange;
        startPosition = transform.position;

        moveRangeHighlights = new GameObject("MoveRangeHighlights");
        moveRangeHighlights.transform.SetParent(transform);
        moveRangeHighlights.SetActive(false);

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.RegisterUnit(this);
        }
        else
        {
            Debug.LogWarning("TurnManager not found in scene!");
        }

        // Căn giữa đơn vị khi khởi tạo, điều chỉnh cho 2 ô
        if (gridMapController != null)
        {
            Tilemap tilemap = gridMapController.GetTilemap();
            if (tilemap != null)
            {
                Vector3Int currentTile = tilemap.WorldToCell(transform.position);
                UpdateOccupiedTiles(currentTile);
                transform.position = tilemap.CellToWorld(currentTile) + new Vector3(tilemap.cellSize.x * (isTwoTileWide ? 1 : 0.5f), tilemap.cellSize.y / 2, 0);
                startPosition = transform.position;
            }
        }

        UpdateTerrainEffect();
    }

    protected virtual void Update()
    {
        // Xử lý mất máu trên DeepSea, điều chỉnh thời gian dựa trên game speed
        if (currentTerrainType == TerrainType.DeepSea)
        {
            float gameSpeed = GetGameSpeedSafely(); // Lấy game speed an toàn
            float adjustedInterval = deepSeaDamageInterval / gameSpeed;
            deepSeaDamageTimer += Time.deltaTime;
            if (deepSeaDamageTimer >= adjustedInterval)
            {
                TakeDeepSeaDamage();
                deepSeaDamageTimer = 0f;
            }
        }
    }

    protected virtual void TakeDeepSeaDamage()
    {
        Health -= deepSeaDamagePerTick;
        Debug.Log($"{UnitName} takes {deepSeaDamagePerTick} damage from DeepSea! Health: {Health}");
        if (Health <= 0)
        {
            Debug.Log($"{UnitName} has been destroyed by DeepSea!");
            Destroy(gameObject);
        }
    }

    public virtual void OnTouch()
    {
        isSelected = true;
        UpdateSelectionVisual();
        Debug.Log($"Unit touched at {transform.position}, Selected: {isSelected}, Remaining Move Range: {remainingMoveRange}, HasMoved: {hasMoved}");
    }

    public virtual void SetMoveTarget(Vector3 target)
    {
        if (isSelected && !hasMoved)
        {
            Vector3Int currentTile = Vector3Int.FloorToInt(startPosition);
            Vector3Int targetTile = Vector3Int.FloorToInt(target);
            int distance = CalculateDistanceWithTerrain(currentTile, targetTile);

            if (distance <= remainingMoveRange && IsTileAccessible(targetTile) && AreAllTilesAccessible(occupiedTiles) && !IsAnyTileOccupied(targetTile))
            {
                // Căn giữa ô tile, điều chỉnh cho 2 ô
                Tilemap tilemap = gridMapController.GetTilemap();
                targetPosition = tilemap.CellToWorld(targetTile) + new Vector3(tilemap.cellSize.x * (isTwoTileWide ? 1 : 0.5f), tilemap.cellSize.y / 2, 0);

                // Bắt đầu animation di chuyển
                StartCoroutine(MoveAnimation(startPosition, targetPosition, distance));
                remainingMoveRange -= distance;
                hasMoved = true;
                isSelected = false;
                UpdateOccupiedTiles(targetTile); // Cập nhật ô bị chiếm
                UpdateSelectionVisual();
                UpdateTerrainEffect(); // Cập nhật địa hình sau khi di chuyển
                Debug.Log($"Unit moved to {targetPosition}, Distance: {distance}, Remaining Move Range: {remainingMoveRange}");
            }
            else
            {
                Debug.Log($"Cannot move! Distance ({distance}) exceeds remaining move range ({remainingMoveRange}), tile is inaccessible, or occupied");
            }
        }
        else
        {
            Debug.Log("Unit cannot move: either not selected or has already moved!");
        }
    }

    private System.Collections.IEnumerator MoveAnimation(Vector3 startPos, Vector3 endPos, int distance)
    {
        isMoving = true;
        float elapsedTime = 0f;
        float gameSpeed = GetGameSpeedSafely(); // Lấy game speed an toàn
        float adjustedDuration = MOVE_ANIMATION_DURATION / gameSpeed;

        while (elapsedTime < adjustedDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / adjustedDuration;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        // Đảm bảo đơn vị đến đúng vị trí đích
        transform.position = endPos;
        startPosition = endPos;
        isMoving = false;
    }

    // Phương thức an toàn để lấy game speed, tránh null reference
    private float GetGameSpeedSafely()
    {
        if (GameSpeedManager.Instance != null)
        {
            return GameSpeedManager.Instance.GetGameSpeed();
        }
        else
        {
            Debug.LogWarning("GameSpeedManager.Instance is null! Using default speed of 1.");
            return 1f; // Giá trị mặc định nếu GameSpeedManager không tồn tại
        }
    }

    protected virtual void UpdateSelectionVisual()
    {
        // Gọi OutlineManager để cập nhật outline
        if (OutlineManager.Instance != null)
        {
            OutlineManager.Instance.UpdateOutline(this, isSelected);
        }

        if (isSelected && !hasMoved && !isMoving)
        {
            UpdateMoveRangeHighlights();
            moveRangeHighlights.SetActive(true);
        }
        else
        {
            moveRangeHighlights.SetActive(false);
        }
    }

    protected virtual void UpdateMoveRangeHighlights()
    {
        foreach (Transform child in moveRangeHighlights.transform)
        {
            Destroy(child.gameObject);
        }

        if (gridMapController == null)
        {
            Debug.LogWarning("GridMapController not assigned in Unit!");
            return;
        }

        Tilemap tilemap = gridMapController.GetTilemap();
        if (tilemap == null)
        {
            Debug.LogWarning("Tilemap not assigned in GridMapController!");
            return;
        }

        Vector3Int unitPos = tilemap.WorldToCell(startPosition);
        HashSet<Vector3Int> reachableTiles = FindReachableTiles(unitPos, remainingMoveRange);

        foreach (Vector3Int tilePos in reachableTiles)
        {
            Vector3 worldPos = tilemap.CellToWorld(tilePos);
            worldPos += new Vector3(tilemap.cellSize.x * (isTwoTileWide ? 1 : 0.5f), tilemap.cellSize.y / 2, 0);

            GameObject highlight = Instantiate(moveRangeHighlightPrefab, worldPos, Quaternion.identity, moveRangeHighlights.transform);
            SpriteRenderer highlightRenderer = highlight.GetComponent<SpriteRenderer>();
            if (highlightRenderer != null)
            {
                highlightRenderer.color = moveRangeHighlightColor; // Áp dụng màu với độ trong suốt
            }
        }
    }

    protected virtual HashSet<Vector3Int> FindReachableTiles(Vector3Int startPos, int maxMoveRange)
    {
        HashSet<Vector3Int> reachableTiles = new HashSet<Vector3Int>();
        Dictionary<Vector3Int, float> costs = new Dictionary<Vector3Int, float>();
        PriorityQueue<Vector3Int> frontier = new PriorityQueue<Vector3Int>();
        Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();

        costs[startPos] = 0;
        frontier.Enqueue(startPos, 0);
        reachableTiles.Add(startPos);

        Tilemap tilemap = gridMapController.GetTilemap();

        while (frontier.Count > 0)
        {
            Vector3Int current = frontier.Dequeue();

            Vector3Int[] neighbors = new Vector3Int[]
            {
                current + new Vector3Int(0, 1, 0),  // Lên
                current + new Vector3Int(0, -1, 0), // Xuống
                current + new Vector3Int(-1, 0, 0), // Trái
                current + new Vector3Int(1, 0, 0)   // Phải
            };

            foreach (Vector3Int next in neighbors)
            {
                if (!tilemap.HasTile(next) || !IsTileAccessible(next) || IsAnyTileOccupied(next))
                    continue;

                float terrainCost = GetTerrainCost(next);
                if (terrainCost == float.MaxValue) // Không thể đi qua
                    continue;

                float newCost = costs[current] + terrainCost;

                if (newCost <= maxMoveRange && AreAllTilesAccessible(GetOccupiedTilesForPosition(next)))
                {
                    if (!costs.ContainsKey(next) || newCost < costs[next])
                    {
                        costs[next] = newCost;
                        float priority = newCost; // Ưu tiên chi phí thấp
                        frontier.Enqueue(next, priority);
                        cameFrom[next] = current;
                        reachableTiles.Add(next);
                    }
                }
            }
        }

        return reachableTiles;
    }

    protected virtual float GetTerrainCost(Vector3Int tilePos)
    {
        TerrainType terrainType = gridMapController.GetTerrainTypeAtPosition(tilePos);
        TerrainEffect effect = GetTerrainEffect(terrainType);

        if (effect.IsImpassable)
            return float.MaxValue;

        return 1f; // Giá trị cố định vì không còn MoveSpeedModifier
    }

    protected virtual void UpdateTerrainEffect()
    {
        if (gridMapController != null)
        {
            Tilemap baseTilemap = gridMapController.GetTilemap();
            if (baseTilemap != null)
            {
                Vector3Int currentTile = baseTilemap.WorldToCell(transform.position);
                currentTerrainType = gridMapController.GetTerrainTypeAtPosition(currentTile);
                currentTerrainEffect = GetTerrainEffect(currentTerrainType);
                Debug.Log($"Unit at {transform.position} on {currentTerrainType} terrain");
            }
        }
    }

    public virtual int CalculateDistanceWithTerrain(Vector3Int startTile, Vector3Int targetTile)
    {
        Dictionary<Vector3Int, float> costs = new Dictionary<Vector3Int, float>();
        PriorityQueue<Vector3Int> frontier = new PriorityQueue<Vector3Int>();
        Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();

        costs[startTile] = 0;
        frontier.Enqueue(startTile, 0);

        Tilemap tilemap = gridMapController.GetTilemap();

        while (frontier.Count > 0)
        {
            Vector3Int current = frontier.Dequeue();

            if (current == targetTile)
                break;

            Vector3Int[] neighbors = new Vector3Int[]
            {
                current + new Vector3Int(0, 1, 0),
                current + new Vector3Int(0, -1, 0),
                current + new Vector3Int(-1, 0, 0),
                current + new Vector3Int(1, 0, 0)
            };

            foreach (Vector3Int next in neighbors)
            {
                if (!tilemap.HasTile(next) || !IsTileAccessible(next) || IsAnyTileOccupied(next))
                    continue;

                float terrainCost = GetTerrainCost(next);
                if (terrainCost == float.MaxValue)
                    continue;

                float newCost = costs[current] + terrainCost;

                if (!costs.ContainsKey(next) || newCost < costs[next])
                {
                    costs[next] = newCost;
                    float priority = newCost + Mathf.Abs(next.x - targetTile.x) + Mathf.Abs(next.y - targetTile.y); // Heuristic
                    frontier.Enqueue(next, priority);
                    cameFrom[next] = current;
                }
            }
        }

        if (!costs.ContainsKey(targetTile))
            return int.MaxValue;

        return Mathf.CeilToInt(costs[targetTile]);
    }

    public virtual bool IsTileAccessible(Vector3Int tilePos)
    {
        if (gridMapController != null)
        {
            TerrainType terrainType = gridMapController.GetTerrainTypeAtPosition(tilePos);
            TerrainEffect effect = GetTerrainEffect(terrainType);
            return !effect.IsImpassable;
        }
        return true;
    }

    protected virtual TerrainEffect GetTerrainEffect(TerrainType terrainType)
    {
        switch (terrainType)
        {
            case TerrainType.Forest:
                return new TerrainEffect(-0.1f, 0.15f, false); // -10% tấn công, +15% phòng thủ
            case TerrainType.Mountain:
                return new TerrainEffect(0.2f, 0.3f, false); // +20% tấn công, +30% phòng thủ
            case TerrainType.Sea:
                return new TerrainEffect(0f, 0f, true); // Không thể đi qua
            case TerrainType.DeepSea:
                return new TerrainEffect(-0.3f, -0.2f, false); // -30% tấn công, -20% phòng thủ
            default: // Plain
                return new TerrainEffect(0f, 0f, false); // Không ảnh hưởng
        }
    }

    protected virtual bool IsAnyTileOccupied(Vector3Int tilePos)
    {
        if (gridMapController == null || gridMapController.GetTilemap() == null)
        {
            Debug.LogWarning("GridMapController or Tilemap not assigned in Unit!");
            return false;
        }

        Vector3Int[] tilesToCheck = GetOccupiedTilesForPosition(tilePos);
        foreach (Vector3Int tile in tilesToCheck)
        {
            Vector3 worldPos = gridMapController.GetTilemap().CellToWorld(tile) + new Vector3(gridMapController.GetTilemap().cellSize.x / 2, gridMapController.GetTilemap().cellSize.y / 2, 0);
            Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, 0.1f, unitLayer);
            foreach (var hit in hits)
            {
                Unit otherUnit = hit.GetComponent<Unit>();
                if (otherUnit != null && otherUnit != this)
                {
                    Debug.Log($"Tile {tile} is occupied by {otherUnit.UnitName}");
                    return true;
                }
            }
        }
        return false;
    }

    protected virtual bool AreAllTilesAccessible(Vector3Int[] tiles)
    {
        if (gridMapController == null) return false;
        foreach (Vector3Int tile in tiles)
        {
            if (!IsTileAccessible(tile)) return false;
        }
        return true;
    }

    protected virtual Vector3Int[] GetOccupiedTilesForPosition(Vector3Int tilePos)
    {
        Vector3Int[] tiles = new Vector3Int[2];
        tiles[0] = tilePos; // Ô đầu tiên
        tiles[1] = tilePos + new Vector3Int(1, 0, 0); // Ô thứ hai (ngang sang phải)
        return tiles;
    }

    protected virtual void UpdateOccupiedTiles(Vector3Int baseTile)
    {
        occupiedTiles = GetOccupiedTilesForPosition(baseTile);
    }

    public virtual bool IsSelected()
    {
        return isSelected;
    }

    public virtual void Deselect()
    {
        isSelected = false;
        UpdateSelectionVisual();
        Debug.Log($"Unit deselected at {transform.position}, HasMoved: {hasMoved}");
    }

    public virtual void ResetMoveRange()
    {
        remainingMoveRange = MoveRange;
        hasMoved = false;
        isSelected = false;
        UpdateSelectionVisual();
        Debug.Log($"Move range reset to {remainingMoveRange} for {name}, HasMoved: {hasMoved}");
    }

    void OnDestroy()
    {
        if (moveRangeHighlights != null)
        {
            Destroy(moveRangeHighlights);
        }
        if (OutlineManager.Instance != null)
        {
            OutlineManager.Instance.RemoveOutline(this);
        }
    }

    public virtual UnitInfo GetUnitInfo()
    {
        return new UnitInfo
        {
            UnitName = UnitName,
            Health = Health,
            Attack = Attack,
            Defense = Defense,
            MoveRange = MoveRange,
            RemainingMoveRange = remainingMoveRange,
            HasMoved = hasMoved
        };
    }
}

// Enum cho loại địa hình, thêm DeepSea
public enum TerrainType
{
    Plain,
    Forest,
    Mountain,
    Sea,
    DeepSea
}

// Struct để lưu hiệu ứng địa hình (loại bỏ MoveSpeedModifier)
public struct TerrainEffect
{
    public float AttackModifier; // Hệ số tấn công (-1 đến 1: giảm hoặc tăng)
    public float DefenseModifier; // Hệ số phòng thủ (-1 đến 1: giảm hoặc tăng)
    public bool IsImpassable; // Có thể đi qua không

    public TerrainEffect(float attackMod, float defenseMod, bool impassable)
    {
        AttackModifier = attackMod;
        DefenseModifier = defenseMod;
        IsImpassable = impassable;
    }
}

public struct UnitInfo
{
    public string UnitName;
    public int Health;
    public int Attack;
    public int Defense; // Thêm chỉ số phòng thủ
    public int MoveRange;
    public int RemainingMoveRange;
    public bool HasMoved;
}

// Class PriorityQueue để hỗ trợ A*
public class PriorityQueue<T>
{
    private List<(T item, float priority)> elements = new List<(T, float)>();

    public int Count => elements.Count;

    public void Enqueue(T item, float priority)
    {
        elements.Add((item, priority));
    }

    public T Dequeue()
    {
        int bestIndex = 0;

        for (int i = 1; i < elements.Count; i++)
        {
            if (elements[i].priority < elements[bestIndex].priority)
            {
                bestIndex = i;
            }
        }

        T bestItem = elements[bestIndex].item;
        elements.RemoveAt(bestIndex);
        return bestItem;
    }
}
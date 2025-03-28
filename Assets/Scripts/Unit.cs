using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using static TerrainEffect;

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
    protected bool isFacingRight = true; // Theo dõi hướng của đơn vị (mặc định quay phải)

    // Thêm biến tạm để lưu hiệu ứng địa hình hiện tại
    protected TerrainEffect currentTerrainEffect;
    protected TerrainType currentTerrainType; // Lưu loại địa hình hiện tại để kiểm tra DeepSea

    // Biến để theo dõi mất máu mỗi lượt trên DeepSea
    protected float deepSeaDamageTimer = 0f;
    protected float deepSeaDamageInterval = 1f; // Thời gian giữa các lần mất máu (sẽ bị ảnh hưởng bởi game speed)
    protected const int deepSeaDamagePerTick = 5; // Mất 5 máu mỗi lần

    // Thời gian cố định cho animation di chuyển (sẽ được điều chỉnh bởi game speed)
    private const float MOVE_ANIMATION_DURATION = 0.5f;

    // Biến liên quan đến xây dựng (cho các đơn vị có khả năng xây dựng như Worker)
    protected bool canBuild = false; // Xác định đơn vị có khả năng xây dựng hay không
    protected float buildProgress = 0f; // Tiến độ xây dựng (0-100%)
    protected float buildTime = 3f; // Thời gian cần để xây dựng (có thể điều chỉnh trong Inspector)
    protected bool isBuilding = false; // Đang trong quá trình xây dựng
    protected Vector3Int buildingTilePosition; // Vị trí ô đang xây dựng

    // Thuộc tính ảo để các lớp con định nghĩa
    public abstract string UnitName { get; }
    public abstract int Health { get; set; }
    public abstract int Attack { get; }
    public abstract int Defense { get; }
    public abstract int MoveRange { get; }

    protected void Start()
    {
        if (GameSpeedManager.Instance != null)
        {
            GameSpeedManager.Instance.OnGameSpeedChanged += OnGameSpeedChangedHandler;
        }

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
            gameObject.AddComponent<BoxCollider2D>();
            Debug.LogWarning($"Added BoxCollider2D to {this.GetType().Name}. Adjust size if needed!");
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

        if (GameManager.Instance != null)
        {
            gridMapController = GameManager.Instance.GetGridMapController();
            GameManager.Instance.RegisterUnit(this);
        }
        else
        {
            Debug.LogWarning("GameManager not found in scene!");
        }

        remainingMoveRange = MoveRange;

        // Căn giữa đơn vị khi khởi tạo
        if (gridMapController != null)
        {
            Tilemap tilemap = gridMapController.GetTilemap();
            if (tilemap != null)
            {
                Vector3Int currentTile = tilemap.WorldToCell(transform.position);
                startPosition = tilemap.CellToWorld(currentTile) + new Vector3(tilemap.cellSize.x / 2, tilemap.cellSize.y / 2, 0);
                transform.position = startPosition; // Đảm bảo vị trí ban đầu được căn chỉnh
                Debug.Log($"Unit initialized at tile {currentTile}, world position {startPosition}");
            }
        }

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

        UpdateTerrainEffect();
    }

    private void OnGameSpeedChangedHandler(float newSpeed)
    {
        // Ví dụ: Điều chỉnh thời gian di chuyển hoặc xây dựng dựa trên tốc độ game
        Debug.Log($"{UnitName} received game speed change: {newSpeed}");
    }

    protected virtual void Update()
    {
        // Kiểm tra trạng thái tạm dừng
        if (GameManager.Instance != null && GameManager.Instance.IsPaused()) return;

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

        // Xử lý tiến độ xây dựng
        if (isBuilding)
        {
            float gameSpeed = GetGameSpeedSafely();
            buildProgress += Time.deltaTime / (buildTime / gameSpeed);
            if (buildProgress >= 1f)
            {
                CompleteBuilding();
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
        // Kiểm tra trạng thái tạm dừng
        if (GameManager.Instance != null && GameManager.Instance.IsPaused()) return;

        isSelected = true;
        UpdateSelectionVisual();
        Debug.Log($"Unit touched at {transform.position}, Selected: {isSelected}, Remaining Move Range: {remainingMoveRange}, HasMoved: {hasMoved}");
    }

    public virtual void SetMoveTarget(Vector3 target)
    {
        // Kiểm tra trạng thái tạm dừng
        if (GameManager.Instance != null && GameManager.Instance.IsPaused()) return;

        if (isSelected && !hasMoved)
        {
            if (gridMapController == null || gridMapController.GetTilemap() == null)
            {
                Debug.LogWarning("GridMapController or Tilemap is not assigned!");
                return;
            }

            Tilemap tilemap = gridMapController.GetTilemap();

            // Chuyển đổi tọa độ thế giới thành tọa độ ô
            Vector3Int currentTile = tilemap.WorldToCell(startPosition);
            Vector3Int targetTile = tilemap.WorldToCell(target);

            // Debug tọa độ ô
            Debug.Log($"Current Tile: {currentTile}, Target Tile: {targetTile}");

            // Tính khoảng cách với địa hình
            int distance = CalculateDistanceWithTerrain(currentTile, targetTile);
            Debug.Log($"Distance to target: {distance}, Remaining Move Range: {remainingMoveRange}");

            // Kiểm tra xem ô đích có hợp lệ không
            if (distance <= remainingMoveRange && IsTileAccessible(targetTile) && !IsTileOccupied(targetTile))
            {
                // Căn giữa ô đích
                targetPosition = tilemap.CellToWorld(targetTile) + new Vector3(tilemap.cellSize.x / 2, tilemap.cellSize.y / 2, 0);
                Debug.Log($"Target Position (world): {targetPosition}");

                // Xác định hướng di chuyển và lật sprite
                UpdateFacingDirection(targetPosition);

                // Bắt đầu animation di chuyển
                StartCoroutine(MoveAnimation(startPosition, targetPosition, distance));
                remainingMoveRange -= distance;
                hasMoved = true;
                isSelected = false;
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
        Debug.Log($"Move completed. Final position: {transform.position}");
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

    // Phương thức để lật sprite dựa trên hướng của mục tiêu
    protected virtual void UpdateFacingDirection(Vector3 target)
    {
        if (spriteRenderer == null) return;

        // So sánh vị trí x của đơn vị và mục tiêu
        float direction = target.x - transform.position.x;
        bool shouldFaceRight = direction > 0;

        // Lật sprite nếu cần
        if (shouldFaceRight != isFacingRight)
        {
            isFacingRight = shouldFaceRight;
            spriteRenderer.flipX = !isFacingRight; // Lật sprite theo trục X
            Debug.Log($"{UnitName} is now facing {(isFacingRight ? "right" : "left")}");
        }
    }

    // Lấy chỉ số tấn công đã điều chỉnh bởi địa hình
    public virtual int GetAdjustedAttack()
    {
        float modifier = currentTerrainEffect.AttackModifier;
        return Mathf.Max(0, Mathf.RoundToInt(Attack * (1f + modifier)));
    }

    // Lấy chỉ số phòng thủ đã điều chỉnh bởi địa hình
    public virtual int GetAdjustedDefense()
    {
        float modifier = currentTerrainEffect.DefenseModifier;
        return Mathf.Max(0, Mathf.RoundToInt(Defense * (1f + modifier)));
    }

    // Phương thức tấn công (quay mặt về phía mục tiêu trước khi tấn công)
    public virtual void AttackUnit(Unit targetUnit)
    {
        // Kiểm tra trạng thái tạm dừng
        if (GameManager.Instance != null && GameManager.Instance.IsPaused()) return;

        if (targetUnit == null)
        {
            Debug.LogWarning($"{UnitName} cannot attack: target is null!");
            return;
        }

        // Quay mặt về phía mục tiêu
        UpdateFacingDirection(targetUnit.transform.position);

        // Tính toán sát thương với chỉ số đã điều chỉnh
        int adjustedAttack = GetAdjustedAttack();
        int adjustedDefense = targetUnit.GetAdjustedDefense();
        int damage = Mathf.Max(0, adjustedAttack - adjustedDefense);
        targetUnit.TakeDamage(damage, this);
        Debug.Log($"{UnitName} attacks {targetUnit.UnitName} for {damage} damage! (Adjusted Attack: {adjustedAttack}, Adjusted Defense: {adjustedDefense})");
    }
    // Cập nhật phương thức nhận sát thương
    public virtual void TakeDamage(int damage, Unit attacker)
    {
        // Kiểm tra trạng thái tạm dừng
        if (GameManager.Instance != null && GameManager.Instance.IsPaused()) return;

        if (attacker != null)
        {
            // Quay mặt về phía kẻ tấn công
            UpdateFacingDirection(attacker.transform.position);
        }

        Health -= damage;
        Debug.Log($"{UnitName} takes {damage} damage! Health: {Health}");
        if (Health <= 0)
        {
            Debug.Log($"{UnitName} has been destroyed!");
            Destroy(gameObject);
        }
    }

    // Phương thức để bắt đầu xây dựng công trình
    public virtual void StartBuilding(Vector3Int tilePosition, BuildingType buildingType)
    {
        // Kiểm tra trạng thái tạm dừng
        if (GameManager.Instance != null && GameManager.Instance.IsPaused()) return;

        if (!canBuild)
        {
            Debug.Log($"{UnitName} cannot build structures!");
            return;
        }

        if (isBuilding)
        {
            Debug.Log($"{UnitName} is already building!");
            return;
        }

        if (IsTileOccupied(tilePosition))
        {
            Debug.Log($"Cannot build at {tilePosition}: Tile is occupied!");
            return;
        }

        if (!IsTileAccessible(tilePosition))
        {
            Debug.Log($"Cannot build at {tilePosition}: Tile is inaccessible!");
            return;
        }

        // Kiểm tra khoảng cách từ đơn vị đến ô xây dựng (phải ở gần, ví dụ: trong phạm vi 1 ô)
        Vector3Int currentTile = gridMapController.GetTilemap().WorldToCell(transform.position);
        int distance = Mathf.Abs(currentTile.x - tilePosition.x) + Mathf.Abs(currentTile.y - tilePosition.y);
        if (distance > 1)
        {
            Debug.Log($"Cannot build at {tilePosition}: Too far from {UnitName}!");
            return;
        }

        // Quay mặt về phía ô xây dựng
        Vector3 worldPos = gridMapController.GetTilemap().CellToWorld(tilePosition);
        UpdateFacingDirection(worldPos);

        // Bắt đầu quá trình xây dựng
        isBuilding = true;
        buildProgress = 0f;
        buildingTilePosition = tilePosition;
        Debug.Log($"{UnitName} started building {buildingType} at {tilePosition}");
    }

    // Phương thức hoàn thành xây dựng
    protected virtual void CompleteBuilding()
    {
        isBuilding = false;
        buildProgress = 0f;
        Debug.Log($"{UnitName} completed building at {buildingTilePosition}");

        // Tạo công trình (có thể gọi một hệ thống quản lý công trình)
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.PlaceBuilding(buildingTilePosition, BuildingType.Fortress); // Ví dụ: Xây pháo đài
        }
        else
        {
            Debug.LogWarning("BuildingManager not found! Cannot place building.");
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
            worldPos += new Vector3(tilemap.cellSize.x / 2, tilemap.cellSize.y / 2, 0);

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
                if (!tilemap.HasTile(next) || !IsTileAccessible(next) || IsTileOccupied(next))
                    continue;

                float terrainCost = GetTerrainCost(next);
                if (terrainCost == float.MaxValue) // Không thể đi qua
                    continue;

                float newCost = costs[current] + terrainCost;

                if (newCost <= maxMoveRange)
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

    // Cập nhật GetTerrainCost để sử dụng AdjustTerrainEffect
    protected virtual float GetTerrainCost(Vector3Int tilePos)
    {
        TerrainType terrainType = gridMapController.GetTerrainTypeAtPosition(tilePos);
        TerrainEffect effect = GetTerrainEffect(terrainType);
        effect = AdjustTerrainEffect(effect, terrainType);

        if (effect.IsImpassable)
            return float.MaxValue;

        return effect.MoveCost;
    }

    // Cập nhật UpdateTerrainEffect để sử dụng AdjustTerrainEffect
    protected virtual void UpdateTerrainEffect()
    {
        if (gridMapController != null)
        {
            Tilemap baseTilemap = gridMapController.GetTilemap();
            if (baseTilemap != null)
            {
                Vector3Int currentTile = baseTilemap.WorldToCell(transform.position);
                currentTerrainType = gridMapController.GetTerrainTypeAtPosition(currentTile);
                TerrainEffect effect = GetTerrainEffect(currentTerrainType);
                currentTerrainEffect = AdjustTerrainEffect(effect, currentTerrainType);
                Debug.Log($"Unit at {transform.position} on {currentTerrainType} terrain, MoveCost: {currentTerrainEffect.MoveCost}");
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
                if (!tilemap.HasTile(next) || !IsTileAccessible(next) || IsTileOccupied(next))
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
        {
            Debug.LogWarning($"Cannot find path from {startTile} to {targetTile}. Returning int.MaxValue.");
            return int.MaxValue;
        }

        Debug.Log($"Calculated distance from {startTile} to {targetTile}: {costs[targetTile]}");
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

    // Phương thức mới để điều chỉnh moveCost dựa trên loại địa hình
    protected virtual TerrainEffect AdjustTerrainEffect(TerrainEffect effect, TerrainType terrainType)
    {
        // Nếu địa hình không thể đi qua, giữ nguyên moveCost
        if (effect.IsImpassable)
        {
            effect.MoveCost = float.MaxValue;
            return effect;
        }

        // Điều chỉnh moveCost dựa trên loại địa hình
        switch (terrainType)
        {
            case TerrainType.Plain:
                effect.MoveCost = 1f; // Bình thường
                break;
            case TerrainType.Forest:
                effect.MoveCost = 1.5f; // Khó đi hơn
                break;
            case TerrainType.Mountain:
                effect.MoveCost = 2f; // Rất khó đi
                break;
            case TerrainType.DeepSea:
                effect.MoveCost = 2.5f; // Cực kỳ khó đi
                break;
            case TerrainType.Sea:
                effect.MoveCost = float.MaxValue; // Không thể đi qua (trừ tàu chiến)
                break;
            default:
                effect.MoveCost = 1f; // Mặc định
                break;
        }

        return effect;
    }

    protected virtual bool IsTileOccupied(Vector3Int tilePos)
    {
        if (gridMapController == null || gridMapController.GetTilemap() == null)
        {
            Debug.LogWarning("GridMapController or Tilemap not assigned in Unit!");
            return false;
        }

        Vector3 worldPos = gridMapController.GetTilemap().CellToWorld(tilePos) + new Vector3(gridMapController.GetTilemap().cellSize.x / 2, gridMapController.GetTilemap().cellSize.y / 2, 0);
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, 0.1f, unitLayer);
        foreach (var hit in hits)
        {
            Unit otherUnit = hit.GetComponent<Unit>();
            if (otherUnit != null && otherUnit != this)
            {
                Debug.Log($"Tile {tilePos} is occupied by {otherUnit.UnitName}");
                return true;
            }
        }
        Debug.Log($"Tile {tilePos} is not occupied.");
        return false;
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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterUnit(this);
        }
        if (OutlineManager.Instance != null)
        {
            OutlineManager.Instance.RemoveOutline(this);
        }
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.UnregisterUnit(this);
        }
        if (GameSpeedManager.Instance != null)
        {
            GameSpeedManager.Instance.OnGameSpeedChanged -= OnGameSpeedChangedHandler;
        }
    }

    public virtual UnitInfo GetUnitInfo()
    {
        return new UnitInfo
        {
            UnitName = UnitName,
            Health = Health,
            Attack = GetAdjustedAttack(), // Sử dụng chỉ số đã điều chỉnh
            Defense = GetAdjustedDefense(), // Sử dụng chỉ số đã điều chỉnh
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

// Enum cho loại công trình
public enum BuildingType
{
    Fortress,  // Pháo đài
    Factory,   // Nhà máy
    Barracks   // Doanh trại
}

// Struct để lưu hiệu ứng địa hình (loại bỏ MoveSpeedModifier)
public struct TerrainEffect
{
    public float AttackModifier; // Hệ số tấn công (-1 đến 1: giảm hoặc tăng)
    public float DefenseModifier; // Hệ số phòng thủ (-1 đến 1: giảm hoặc tăng)
    public bool IsImpassable; // Có thể đi qua không
    public float MoveCost; // Chi phí di chuyển (1 là bình thường, >1 là khó đi, <1 là dễ đi)

    // Hàm khởi tạo với 4 tham số (bao gồm moveCost)
    public TerrainEffect(float attackMod, float defenseMod, bool impassable, float moveCost)
    {
        AttackModifier = attackMod;
        DefenseModifier = defenseMod;
        IsImpassable = impassable;
        MoveCost = moveCost;
    }

    // Hàm khởi tạo với 3 tham số (không có moveCost, để tương thích với các lớp con hiện tại)
    public TerrainEffect(float attackMod, float defenseMod, bool impassable)
    {
        AttackModifier = attackMod;
        DefenseModifier = defenseMod;
        IsImpassable = impassable;
        MoveCost = 1f; // Giá trị mặc định, sẽ được điều chỉnh sau trong lớp cha
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
}
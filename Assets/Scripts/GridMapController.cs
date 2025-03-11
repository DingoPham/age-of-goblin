using UnityEngine;
using UnityEngine.Tilemaps;

public class GridMapController : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap; // Tilemap cơ bản (để xác định ô có thể đi)
    [SerializeField] private Tilemap overlayTilemap; // Tilemap lớp phủ (chỉ để hiển thị, không ảnh hưởng gameplay)
    [SerializeField] private Tilemap plainTilemap; // Tilemap cho địa hình đồng bằng
    [SerializeField] private Tilemap forestTilemap; // Tilemap cho địa hình rừng
    [SerializeField] private Tilemap mountainTilemap; // Tilemap cho địa hình núi
    [SerializeField] private Tilemap seaTilemap; // Tilemap cho địa hình biển
    [SerializeField] private Tilemap deepSeaTilemap; // Tilemap cho địa hình biển sâu
    [SerializeField] private LayerMask unitLayer; // Layer của đơn vị
    [SerializeField] private UnitInfoUI unitInfoUI; // UI thông tin đơn vị

    private Vector3Int[] tilePositions; // Tọa độ các ô
    private Unit selectedUnit; // Đơn vị được chọn

    private float cellGapX = 0.1f; // Khoảng cách giữa các ô
    private float cellGapY = 0.1f;

    private int minX, maxX, minY, maxY; // Phạm vi Tilemap

    private float minZoom = 1f; // Giới hạn thu nhỏ
    private float maxZoom = 10f; // Giới hạn phóng to
    private Vector3 lastTouchPosition; // Vị trí chạm cuối cùng
    private bool isDragging = false;
    private float dragThreshold = 0.1f; // Ngưỡng kéo

    private Vector2 endTurnTouchAreaMin = new Vector2(Screen.width * 0.8f, Screen.height * 0.8f);
    private Vector2 endTurnTouchAreaMax = new Vector2(Screen.width, Screen.height);

    void Start()
    {
        GenerateTilePositions();
        if (unitInfoUI != null)
        {
            unitInfoUI.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        HandleInput();
        ClampCameraPosition();
    }

    void HandleInput()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    lastTouchPosition = Camera.main.ScreenToWorldPoint(touch.position);
                    isDragging = false;
                    Debug.Log($"Touch Began at {lastTouchPosition}");
                    if (IsTouchInEndTurnArea(touch.position))
                    {
                        if (TurnManager.Instance != null)
                        {
                            TurnManager.Instance.NextTurn();
                            Debug.Log("End Turn triggered by touch area!");
                        }
                        else
                        {
                            Debug.LogWarning("TurnManager not found!");
                        }
                    }
                    else
                    {
                        CheckUnitSelection(touch.position);
                    }
                    break;

                case TouchPhase.Moved:
                    Vector3 currentTouchPosition = Camera.main.ScreenToWorldPoint(touch.position);
                    float distanceMoved = Vector3.Distance(currentTouchPosition, lastTouchPosition);

                    if (distanceMoved > dragThreshold && !isDragging)
                    {
                        isDragging = true;
                        Debug.Log($"Dragging started, distance: {distanceMoved}");
                        if (selectedUnit != null)
                        {
                            selectedUnit.Deselect();
                            selectedUnit = null;
                            if (unitInfoUI != null) unitInfoUI.gameObject.SetActive(false);
                            Debug.Log("Unit deselected due to dragging.");
                        }
                    }

                    if (isDragging)
                    {
                        Vector3 delta = currentTouchPosition - lastTouchPosition;
                        Camera.main.transform.position -= delta;
                        lastTouchPosition = currentTouchPosition;
                    }
                    break;

                case TouchPhase.Ended:
                    if (isDragging)
                    {
                        Debug.Log("Drag ended, no unit action.");
                        isDragging = false;
                    }
                    else if (selectedUnit != null)
                    {
                        Debug.Log("Touch ended, handling click or move.");
                        HandleClickOrTouchEnd(touch.position);
                    }
                    break;
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
            {
                Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
                Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

                float prevTouchDeltaMag = (touch0PrevPos - touch1PrevPos).magnitude;
                float touchDeltaMag = (touch0.position - touch1.position).magnitude;

                float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

                float newSize = Camera.main.orthographicSize + deltaMagnitudeDiff * 0.01f;
                Camera.main.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
            }
        }
    }

    private bool IsTouchInEndTurnArea(Vector2 touchPosition)
    {
        return touchPosition.x >= endTurnTouchAreaMin.x && touchPosition.x <= endTurnTouchAreaMax.x &&
               touchPosition.y >= endTurnTouchAreaMin.y && touchPosition.y <= endTurnTouchAreaMax.y;
    }

    void CheckUnitSelection(Vector3 screenPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, unitLayer);
        if (hit.collider != null)
        {
            Unit unit = hit.collider.GetComponent<Unit>();
            if (unit != null)
            {
                Debug.Log($"Raycast hit: {hit.collider.name} at {hit.point}");
                if (selectedUnit != null && selectedUnit != unit)
                {
                    selectedUnit.Deselect();
                    if (unitInfoUI != null) unitInfoUI.gameObject.SetActive(false);
                }
                selectedUnit = unit;
                selectedUnit.OnTouch();
                if (unitInfoUI != null && selectedUnit.IsSelected())
                {
                    unitInfoUI.UpdateUI(selectedUnit);
                    unitInfoUI.gameObject.SetActive(true);
                }
            }
        }
        else
        {
            Debug.Log("Raycast did not hit any Unit");
        }
    }

    void HandleClickOrTouchEnd(Vector3 screenPosition)
    {
        if (selectedUnit != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            RaycastHit2D hitUnit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, unitLayer);

            if (hitUnit.collider != null)
            {
                Unit unit = hitUnit.collider.GetComponent<Unit>();
                if (unit != null && unit != selectedUnit)
                {
                    selectedUnit.Deselect();
                    if (unitInfoUI != null) unitInfoUI.gameObject.SetActive(false);
                    selectedUnit = unit;
                    selectedUnit.OnTouch();
                    if (unitInfoUI != null && selectedUnit.IsSelected())
                    {
                        unitInfoUI.UpdateUI(selectedUnit);
                        unitInfoUI.gameObject.SetActive(true);
                    }
                    Debug.Log("Switched to a different unit.");
                    return;
                }
            }

            Vector3Int clickedPos = GetTileAtTouchPosition(screenPosition);
            if (IsValidTile(clickedPos))
            {
                if (hitUnit.collider == null)
                {
                    UnitInfo unitInfo = selectedUnit.GetUnitInfo();
                    if (!unitInfo.HasMoved)
                    {
                        Vector3 worldPos = tilemap.CellToWorld(clickedPos);
                        worldPos += new Vector3(tilemap.cellSize.x / 2, tilemap.cellSize.y / 2, 0);

                        Vector3Int currentTile = tilemap.WorldToCell(selectedUnit.transform.position);
                        int distance = selectedUnit.CalculateDistanceWithTerrain(currentTile, clickedPos);
                        Debug.Log($"Clicked tile: {clickedPos}, Current tile: {currentTile}, Distance: {distance}, Remaining Move Range: {unitInfo.RemainingMoveRange}");
                        if (distance <= unitInfo.RemainingMoveRange && selectedUnit.IsTileAccessible(clickedPos))
                        {
                            selectedUnit.SetMoveTarget(worldPos);
                            Debug.Log($"Unit moving to tile at {worldPos}, Distance: {distance}");
                        }
                        else
                        {
                            selectedUnit.Deselect();
                            selectedUnit = null;
                            if (unitInfoUI != null) unitInfoUI.gameObject.SetActive(false);
                            Debug.Log("Deselected unit: Clicked tile is out of move range or inaccessible.");
                        }
                    }
                    else
                    {
                        selectedUnit.Deselect();
                        selectedUnit = null;
                        if (unitInfoUI != null) unitInfoUI.gameObject.SetActive(false);
                        Debug.Log("Deselected unit: Unit has already moved this turn.");
                    }
                }
            }
            else
            {
                selectedUnit.Deselect();
                selectedUnit = null;
                if (unitInfoUI != null) unitInfoUI.gameObject.SetActive(false);
                Debug.Log("Deselected unit due to invalid tile click.");
            }
        }
        else
        {
            Debug.Log("No unit selected to handle click.");
        }
    }

    Vector3Int GetTileAtTouchPosition(Vector3 screenPosition)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPosition);
        return tilemap.WorldToCell(worldPos);
    }

    void ClampCameraPosition()
    {
        float totalWidth = (maxX - minX + 1) * tilemap.cellSize.x + ((maxX - minX) * cellGapX);
        float totalHeight = (maxY - minY + 1) * tilemap.cellSize.y + ((maxY - minY) * cellGapY);
        Vector3 camPos = Camera.main.transform.position;

        float camHalfWidth = Camera.main.orthographicSize * Camera.main.aspect;
        float camHalfHeight = Camera.main.orthographicSize;

        float minXBound = minX - camHalfWidth;
        float maxXBound = maxX + 1 + camHalfWidth;
        float minYBound = minY - camHalfHeight;
        float maxYBound = maxY + 1 + camHalfHeight;

        camPos.x = Mathf.Clamp(camPos.x, minXBound, maxXBound);
        camPos.y = Mathf.Clamp(camPos.y, minYBound, maxYBound);
        camPos.z = -10;

        Camera.main.transform.position = camPos;

        Debug.Log($"Camera Position: {Camera.main.transform.position}, Bounds: ({minXBound}, {maxXBound}, {minYBound}, {maxYBound})");
    }

    void GenerateTilePositions()
    {
        System.Collections.Generic.List<Vector3Int> tempPositions = new System.Collections.Generic.List<Vector3Int>();
        minX = int.MaxValue; maxX = int.MinValue;
        minY = int.MaxValue; maxY = int.MinValue;

        for (int x = -50; x <= 50; x++)
        {
            for (int y = -50; y <= 50; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                if (tilemap.HasTile(pos))
                {
                    tempPositions.Add(pos);

                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
            }
        }

        tilePositions = tempPositions.ToArray();

        if (tilePositions.Length == 0)
        {
            Debug.LogWarning("No tiles found in Tilemap! Please draw some tiles.");
        }
        else
        {
            Debug.Log($"Found {tilePositions.Length} tiles in Tilemap. Range: ({minX}, {minY}) to ({maxX}, {maxY})");
        }
    }

    bool IsValidTile(Vector3Int pos)
    {
        return tilemap.HasTile(pos);
    }

    Vector3Int[] GetAllTilePositions()
    {
        return tilePositions;
    }

    public Tilemap GetTilemap()
    {
        return tilemap;
    }

    // Để lại phương thức này cho tương thích, nhưng không cần dùng nữa
    public Tilemap GetTerrainTilemap()
    {
        Debug.LogWarning("GetTerrainTilemap is deprecated. Use specific terrain tilemaps instead.");
        return null;
    }

    // Phương thức mới để lấy địa hình tại một vị trí
    public TerrainType GetTerrainTypeAtPosition(Vector3Int tilePos)
    {
        // Ưu tiên: DeepSea > Sea > Mountain > Forest > Plain
        // OverlayTilemap không ảnh hưởng đến logic gameplay
        if (deepSeaTilemap != null && deepSeaTilemap.HasTile(tilePos))
            return TerrainType.DeepSea;
        if (seaTilemap != null && seaTilemap.HasTile(tilePos))
            return TerrainType.Sea;
        if (mountainTilemap != null && mountainTilemap.HasTile(tilePos))
            return TerrainType.Mountain;
        if (forestTilemap != null && forestTilemap.HasTile(tilePos))
            return TerrainType.Forest;
        if (plainTilemap != null && plainTilemap.HasTile(tilePos))
            return TerrainType.Plain;

        // Mặc định là đồng bằng nếu không có tile nào
        return TerrainType.Plain;
    }
}
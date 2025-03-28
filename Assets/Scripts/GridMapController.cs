using static TerrainEffect;
using UnityEngine.Tilemaps;
using UnityEngine;

public class GridMapController : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap; // Tilemap cơ bản (để xác định ô có thể đi)
    [SerializeField] private Tilemap overlayTilemap; // Tilemap lớp phủ (chỉ để hiển thị, không ảnh hưởng gameplay)
    [SerializeField] private Tilemap gridTilemap; // Tilemap chỉ để vẽ lưới chia ô
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
    private Vector3 dragOrigin; // Vị trí bắt đầu kéo
    private bool isDragging = false;
    [SerializeField] private float dragSpeed = 1f; // Hệ số tốc độ kéo (có thể điều chỉnh trong Inspector)

    private Vector2 endTurnTouchAreaMin = new Vector2(0.8f, 0.8f); // Tỷ lệ màn hình
    private Vector2 endTurnTouchAreaMax = new Vector2(1f, 1f);

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        GenerateTilePositions();
        if (unitInfoUI != null)
        {
            unitInfoUI.gameObject.SetActive(false);
        }
        InitializeCamera();

        // Khởi tạo trạng thái lưới từ PlayerPrefs
        bool showGrid = PlayerPrefs.GetInt("ShowGrid", 1) == 1; // Mặc định là hiển thị (1)
        SetGridVisibility(showGrid);
    }

    void Update()
    {
        // Kiểm tra trạng thái tạm dừng
        if (GameManager.Instance != null && GameManager.Instance.IsPaused()) return;

        // Cập nhật khu vực "End Turn" dựa trên kích thước màn hình
        endTurnTouchAreaMin = new Vector2(Screen.width * 0.8f, Screen.height * 0.8f);
        endTurnTouchAreaMax = new Vector2(Screen.width, Screen.height);

        HandleInput();
        if (!isDragging)
        {
            ClampCameraPosition();
        }
    }

    void InitializeCamera()
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap is not assigned in GridMapController!");
            return;
        }

        // Tính kích thước bản đồ
        float totalWidth = (maxX - minX + 1) * tilemap.cellSize.x + ((maxX - minX) * cellGapX);
        float totalHeight = (maxY - minY + 1) * tilemap.cellSize.y + ((maxY - minY) * cellGapY);

        // Đặt orthographicSize để camera hiển thị toàn bộ bản đồ
        float desiredHeight = totalHeight / 2f;
        float desiredWidth = totalWidth / 2f / mainCamera.aspect;
        mainCamera.orthographicSize = Mathf.Max(desiredHeight, desiredWidth);
        mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize, minZoom, maxZoom);

        // Đặt vị trí ban đầu của camera ở giữa bản đồ
        Vector3 minWorldPos = tilemap.CellToWorld(new Vector3Int(minX, minY, 0));
        Vector3 maxWorldPos = tilemap.CellToWorld(new Vector3Int(maxX, maxY, 0)) + new Vector3(tilemap.cellSize.x, tilemap.cellSize.y, 0);
        Vector3 centerPos = (minWorldPos + maxWorldPos) / 2f;
        centerPos.z = -10;
        mainCamera.transform.position = centerPos;

        Debug.Log($"Camera initialized: OrthographicSize = {mainCamera.orthographicSize}, Position = {centerPos}");
    }

    void HandleInput()
    {
        // Kiểm tra trạng thái tạm dừng
        if (GameManager.Instance != null && GameManager.Instance.IsPaused()) return;

        // Xử lý cảm ứng (touch) - dành cho thiết bị di động
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    dragOrigin = mainCamera.ScreenToWorldPoint(touch.position);
                    isDragging = false;
                    Debug.Log($"Touch Began at {dragOrigin}");
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
                    isDragging = true;
                    if (selectedUnit != null)
                    {
                        selectedUnit.Deselect();
                        selectedUnit = null;
                        if (unitInfoUI != null) unitInfoUI.gameObject.SetActive(false);
                        Debug.Log("Unit deselected due to dragging.");
                    }

                    Vector3 currentTouchPosition = mainCamera.ScreenToWorldPoint(touch.position);
                    Vector3 move = (dragOrigin - currentTouchPosition) * dragSpeed;
                    Vector3 newPosition = mainCamera.transform.position + move;

                    // Áp dụng giới hạn ngay trong khi kéo
                    float camHalfHeight = mainCamera.orthographicSize;
                    float camHalfWidth = camHalfHeight * mainCamera.aspect;
                    Vector3 minWorldPos = tilemap.CellToWorld(new Vector3Int(minX, minY, 0));
                    Vector3 maxWorldPos = tilemap.CellToWorld(new Vector3Int(maxX, maxY, 0)) + new Vector3(tilemap.cellSize.x, tilemap.cellSize.y, 0);
                    newPosition.x = Mathf.Clamp(newPosition.x, minWorldPos.x + camHalfWidth, maxWorldPos.x - camHalfWidth);
                    newPosition.y = Mathf.Clamp(newPosition.y, minWorldPos.y + camHalfHeight, maxWorldPos.y - camHalfHeight);
                    newPosition.z = -10;

                    mainCamera.transform.position = newPosition;
                    Debug.Log($"Dragging: Move = {move}, New Position = {newPosition}");
                    break;

                case TouchPhase.Ended:
                    if (isDragging)
                    {
                        Debug.Log("Drag ended (touch), no unit action.");
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
        // Xử lý chuột (mouse) - dành cho PC
        else if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            isDragging = false;
            CheckUnitSelection(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0))
        {
            isDragging = true;
            if (selectedUnit != null)
            {
                selectedUnit.Deselect();
                selectedUnit = null;
                if (unitInfoUI != null) unitInfoUI.gameObject.SetActive(false);
                Debug.Log("Unit deselected due to dragging.");
            }

            Vector3 currentMousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 move = (dragOrigin - currentMousePosition) * dragSpeed;
            Vector3 newPosition = mainCamera.transform.position + move;

            // Áp dụng giới hạn ngay trong khi kéo
            float camHalfHeight = mainCamera.orthographicSize;
            float camHalfWidth = camHalfHeight * mainCamera.aspect;
            Vector3 minWorldPos = tilemap.CellToWorld(new Vector3Int(minX, minY, 0));
            Vector3 maxWorldPos = tilemap.CellToWorld(new Vector3Int(maxX, maxY, 0)) + new Vector3(tilemap.cellSize.x, tilemap.cellSize.y, 0);
            newPosition.x = Mathf.Clamp(newPosition.x, minWorldPos.x + camHalfWidth, maxWorldPos.x - camHalfWidth);
            newPosition.y = Mathf.Clamp(newPosition.y, minWorldPos.y + camHalfHeight, maxWorldPos.y - camHalfHeight);
            newPosition.z = -10;

            mainCamera.transform.position = newPosition;
            Debug.Log($"Dragging: Move = {move}, New Position = {newPosition}");
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                Debug.Log("Drag ended (mouse), no unit action.");
                isDragging = false;
            }
            else if (selectedUnit != null)
            {
                Debug.Log("Mouse click ended, handling click or move.");
                HandleClickOrTouchEnd(Input.mousePosition);
            }
        }
        // Xử lý zoom bằng hai ngón tay (touch)
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

                float newSize = mainCamera.orthographicSize + deltaMagnitudeDiff * 0.01f;
                mainCamera.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
            }
        }
        // Xử lý zoom bằng scroll wheel (mouse)
        else
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                float zoomSpeed = 5f;
                float newZoom = mainCamera.orthographicSize - scroll * zoomSpeed;
                mainCamera.orthographicSize = Mathf.Clamp(newZoom, minZoom, maxZoom);
            }
        }
    }

    private bool IsTouchInEndTurnArea(Vector2 touchPosition)
    {
        bool inArea = touchPosition.x >= endTurnTouchAreaMin.x && touchPosition.x <= endTurnTouchAreaMax.x &&
                      touchPosition.y >= endTurnTouchAreaMin.y && touchPosition.y <= endTurnTouchAreaMax.y;
        Debug.Log($"Touch Position: {touchPosition}, End Turn Area: ({endTurnTouchAreaMin}, {endTurnTouchAreaMax}), In Area: {inArea}");
        return inArea;
    }

    void CheckUnitSelection(Vector3 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, unitLayer);
        if (hit.collider != null)
        {
            Unit unit = hit.collider.GetComponent<Unit>();
            if (unit != null)
            {
                Debug.Log($"Raycast hit: {hit.collider.name} at {hit.point}, Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
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
            Debug.Log($"Raycast did not hit any Unit at screen position {screenPosition}, Ray: {ray.origin} -> {ray.direction}");
        }
    }

    void HandleClickOrTouchEnd(Vector3 screenPosition)
    {
        if (selectedUnit == null)
        {
            Debug.Log("No unit selected to handle click.");
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
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

    Vector3Int GetTileAtTouchPosition(Vector3 screenPosition)
    {
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPosition);
        return tilemap.WorldToCell(worldPos);
    }

    void ClampCameraPosition()
    {
        if (tilemap == null) return;

        Vector3 minWorldPos = tilemap.CellToWorld(new Vector3Int(minX, minY, 0));
        Vector3 maxWorldPos = tilemap.CellToWorld(new Vector3Int(maxX, maxY, 0)) + new Vector3(tilemap.cellSize.x, tilemap.cellSize.y, 0);

        float camHalfHeight = mainCamera.orthographicSize;
        float camHalfWidth = camHalfHeight * mainCamera.aspect;

        float minXBound = minWorldPos.x + camHalfWidth;
        float maxXBound = maxWorldPos.x - camHalfWidth;
        float minYBound = minWorldPos.y + camHalfHeight;
        float maxYBound = maxWorldPos.y - camHalfHeight;

        Vector3 camPos = mainCamera.transform.position;

        camPos.x = Mathf.Clamp(camPos.x, minXBound, maxXBound);
        camPos.y = Mathf.Clamp(camPos.y, minYBound, maxYBound);
        camPos.z = -10;

        mainCamera.transform.position = camPos;

        Debug.Log($"Camera Position: {mainCamera.transform.position}, Bounds: ({minXBound}, {maxXBound}, {minYBound}, {maxYBound})");
    }

    void GenerateTilePositions()
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap is not assigned in GridMapController!");
            return;
        }

        System.Collections.Generic.List<Vector3Int> tempPositions = new System.Collections.Generic.List<Vector3Int>();
        BoundsInt bounds = tilemap.cellBounds;

        minX = bounds.xMin; maxX = bounds.xMax - 1;
        minY = bounds.yMin; maxY = bounds.yMax - 1;

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (tilemap.HasTile(pos))
            {
                tempPositions.Add(pos);
                minX = Mathf.Min(minX, pos.x);
                maxX = Mathf.Max(maxX, pos.x);
                minY = Mathf.Min(minY, pos.y);
                maxY = Mathf.Max(maxY, pos.y);
            }
        }

        tilePositions = tempPositions.ToArray();

        if (tilePositions.Length == 0)
        {
            Debug.LogWarning("No tiles found in Tilemap! Please draw some tiles.");
            minX = 0; maxX = 0; minY = 0; maxY = 0;
        }
        else
        {
            Debug.Log($"Found {tilePositions.Length} tiles in Tilemap. Range: ({minX}, {minY}) to ({maxX}, {maxY})");
        }
    }

    bool IsValidTile(Vector3Int pos)
    {
        return tilemap != null && tilemap.HasTile(pos);
    }

    public void SetGridVisibility(bool isVisible)
    {
        if (gridTilemap != null)
        {
            gridTilemap.gameObject.SetActive(isVisible);
            PlayerPrefs.SetInt("ShowGrid", isVisible ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"Grid visibility set to: {isVisible}");
        }
        else
        {
            Debug.LogWarning("Grid Tilemap is not assigned, cannot toggle grid visibility!");
        }
    }

    public Vector3Int[] GetAllTilePositions()
    {
        return tilePositions;
    }

    public Tilemap GetTilemap()
    {
        return tilemap;
    }

    public Tilemap GetTerrainTilemap()
    {
        Debug.LogWarning("GetTerrainTilemap is deprecated. Use specific terrain tilemaps instead.");
        return null;
    }

    public TerrainType GetTerrainTypeAtPosition(Vector3Int tilePos)
    {
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

        return TerrainType.Plain;
    }
}
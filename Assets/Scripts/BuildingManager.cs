using UnityEngine;
using UnityEngine.Tilemaps;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    [SerializeField] private GridMapController gridMapController; // Tham chiếu đến GridMapController
    [SerializeField] private GameObject fortressPrefab; // Prefab cho pháo đài
    [SerializeField] private GameObject factoryPrefab;  // Prefab cho nhà máy
    [SerializeField] private GameObject barracksPrefab; // Prefab cho doanh trại

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlaceBuilding(Vector3Int tilePosition, BuildingType buildingType)
    {
        if (gridMapController == null)
        {
            Debug.LogWarning("GridMapController not assigned in BuildingManager!");
            return;
        }

        Tilemap tilemap = gridMapController.GetTilemap();
        if (tilemap == null)
        {
            Debug.LogWarning("Tilemap not assigned in GridMapController!");
            return;
        }

        Vector3 worldPos = tilemap.CellToWorld(tilePosition) + new Vector3(tilemap.cellSize.x / 2, tilemap.cellSize.y / 2, 0);
        GameObject buildingPrefab = null;

        switch (buildingType)
        {
            case BuildingType.Fortress:
                buildingPrefab = fortressPrefab;
                break;
            case BuildingType.Factory:
                buildingPrefab = factoryPrefab;
                break;
            case BuildingType.Barracks:
                buildingPrefab = barracksPrefab;
                break;
        }

        if (buildingPrefab != null)
        {
            Instantiate(buildingPrefab, worldPos, Quaternion.identity);
            Debug.Log($"Placed {buildingType} at {tilePosition}");
        }
        else
        {
            Debug.LogWarning($"No prefab assigned for {buildingType}!");
        }
    }
}
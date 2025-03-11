using UnityEngine;
using System.Collections.Generic;

public class OutlineManager : MonoBehaviour
{
    public static OutlineManager Instance { get; private set; }

    [SerializeField] private GameObject outlinePrefab; // Prefab cho outline (có SpriteRenderer và Animator)
    [SerializeField] private float outlineHeightOffset = 0.5f; // Độ cao của outline khi được chọn (tính bằng đơn vị thế giới)
    [SerializeField] private float selectedScale = 1.5f; // Tỷ lệ phóng to khi được chọn
    [SerializeField] private float defaultScale = 1.0f; // Tỷ lệ mặc định khi không được chọn

    private Dictionary<Unit, GameObject> unitOutlines = new Dictionary<Unit, GameObject>();

    void Awake()
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

    public void AddOutline(Unit unit)
    {
        if (!unitOutlines.ContainsKey(unit) && outlinePrefab != null)
        {
            GameObject outline = Instantiate(outlinePrefab, unit.transform.position, Quaternion.identity, unit.transform);
            outline.name = "Outline";
            Animator animator = outline.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("Animator not found on Outline prefab! Please add an Animator component.");
            }
            outline.transform.localScale = new Vector3(defaultScale, defaultScale, 1f); // Đặt tỷ lệ mặc định
            unitOutlines[unit] = outline;
            outline.SetActive(false);
        }
    }

    public void UpdateOutline(Unit unit, bool isSelected)
    {
        if (unitOutlines.ContainsKey(unit))
        {
            GameObject outline = unitOutlines[unit];
            outline.SetActive(isSelected);

            if (isSelected)
            {
                // Căn vị trí lên cao
                Vector3 newPosition = unit.transform.position + new Vector3(0f, outlineHeightOffset, 0f);
                outline.transform.position = newPosition;

                // Phóng to outline
                outline.transform.localScale = new Vector3(selectedScale, selectedScale, 1f);

                Animator animator = outline.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.enabled = true;
                    animator.Play("OutlineAnimation", -1, 0f);
                }
            }
            else
            {
                // Đặt lại vị trí và tỷ lệ khi không được chọn
                outline.transform.position = unit.transform.position;
                outline.transform.localScale = new Vector3(defaultScale, defaultScale, 1f);

                Animator animator = outline.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.enabled = false;
                }
            }
        }
    }

    public void RemoveOutline(Unit unit)
    {
        if (unitOutlines.ContainsKey(unit))
        {
            Destroy(unitOutlines[unit]);
            unitOutlines.Remove(unit);
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapSlider : MonoBehaviour
{
    public RectTransform mapContainer;  // Gắn MapContainer vào đây
    public float slideSpeed = 500f;     // Tốc độ trượt (pixel/giây)
    public float mapWidth = 960f;       // Chiều rộng mỗi map
    public TMP_Text dialogueText; // Gắn DialogueText trong Content vào đây
    public ScrollRect scrollView; // Gắn DialogueScrollView vào đây

    private int currentMapIndex = 0;    // Map hiện tại (0 = Map1, 1 = Map2, ...)
    private int totalMaps;              // Tổng số map
    private Vector2 targetPosition;     // Vị trí mục tiêu khi trượt
    private bool isSliding = false;     // Đang trượt hay không

    private string[] mapDialogues = new string[]
    {
        "Đây là Rừng Xanh, nơi Goblin ẩn nấp trong bóng tối. Câu chuyện dài dòng về khu rừng này bắt đầu từ hàng trăm năm trước khi một trận chiến kinh hoàng diễn ra...",
        "Đồng Bằng Vàng, vùng đất giàu tài nguyên của Con người. Nơi đây từng là chiến trường giữa các vương quốc cổ đại, giờ chỉ còn lại những cánh đồng trải dài bất tận và những mỏ vàng lấp lánh.",
        "Núi Đá Đen, pháo đài bất khả xâm phạm của chiến tranh. Truyền thuyết kể rằng một vị thần đã nguyền rủa ngọn núi này, khiến nó trở thành nơi trú ẩn của những sinh vật kỳ bí."
    };

    void Start()
    {
        totalMaps = mapContainer.childCount; // Đếm số map trong container
        targetPosition = mapContainer.anchoredPosition; // Vị trí ban đầu
        UpdateDialogue();
    }

    void Update()
    {
        if (!isSliding && Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                float deltaX = touch.deltaPosition.x;
                if (Mathf.Abs(deltaX) > 50f) // Ngưỡng vuốt
                {
                    if (deltaX > 0) SlideLeft();  // Vuốt phải -> sang trái
                    else SlideRight();            // Vuốt trái -> sang phải
                }
            }
        }

        if (isSliding)
        {
            // Trượt mượt mà đến vị trí mục tiêu
            mapContainer.anchoredPosition = Vector2.MoveTowards(
                mapContainer.anchoredPosition,
                targetPosition,
                slideSpeed * Time.deltaTime
            );

            // Kiểm tra nếu đã đến vị trí
            if (Vector2.Distance(mapContainer.anchoredPosition, targetPosition) < 0.1f)
            {
                mapContainer.anchoredPosition = targetPosition;
                isSliding = false;
                UpdateDialogue();
            }
        }
    }

    // Gọi khi bấm mũi tên phải
    public void SlideRight()
    {
        if (!isSliding && currentMapIndex < totalMaps - 1)
        {
            currentMapIndex++;
            targetPosition = new Vector2(-currentMapIndex * mapWidth, mapContainer.anchoredPosition.y);
            isSliding = true;
        }
    }

    // Gọi khi bấm mũi tên trái
    public void SlideLeft()
    {
        if (!isSliding && currentMapIndex > 0)
        {
            currentMapIndex--;
            targetPosition = new Vector2(-currentMapIndex * mapWidth, mapContainer.anchoredPosition.y);
            isSliding = true;
        }
    }

    private void UpdateDialogue()
    {
        if (currentMapIndex >= 0 && currentMapIndex < mapDialogues.Length)
        {
            dialogueText.text = mapDialogues[currentMapIndex];
            // Reset scroll về đầu khi đổi nội dung
            scrollView.verticalNormalizedPosition = 1f;
        }
    }
}
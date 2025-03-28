using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OpenScenes : MonoBehaviour
{
    public Button campaignButton; // Nút mở Campaign
    public Button inventoryButton; // Nút mở Inventory (nếu có)
    public Button shopButton; // Nút mở Shop (nếu có)
    public Button quitButton; // Nút thoát game (nếu có)

    void Start()
    {
        if (campaignButton != null)
        {
            campaignButton.onClick.AddListener(OpenCampaign);
        }
        if (inventoryButton != null)
        {
            inventoryButton.onClick.AddListener(OpenInventory);
        }
        if (shopButton != null)
        {
            shopButton.onClick.AddListener(OpenShop);
        }
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    #region Campaign Close/Open
    public void OpenCampaign()
    {
        LoadingScreen.LoadScene("Campaign");
    }
    public void CloseCampaign()
    {
        LoadingScreen.LoadScene("Main Menu");
    }
    #endregion

    #region Inventory Close/Open
    public void OpenInventory()
    {
        LoadingScreen.LoadScene("Inventory"); // Load thay thế (Single mode)
    }

    public void CloseInventory() // Gọi từ scene Inventory, không cần ở đây
    {
        LoadingScreen.LoadScene("Main Menu");
    }
    #endregion

    #region Shop Close/Open
    public void OpenShop()
    {
        LoadingScreen.LoadScene("Shop"); // Load thay thế (Single mode)
    }

    public void CloseShop() // Gọi từ scene Shop, không cần ở đây
    {
        LoadingScreen.LoadScene("Main Menu");
    }
    #endregion

    #region Battle Open/Close
    public void OpenBattle()
    {
        LoadingScreen.LoadScene("Battle");
    }
    public void CloseBattle()
    {
        LoadingScreen.LoadScene("Campaign");
    }
    #endregion

    public void QuitGame()
    {
        Application.Quit();
    }
}
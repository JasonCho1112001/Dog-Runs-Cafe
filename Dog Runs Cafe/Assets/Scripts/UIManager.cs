using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Win/Lose UI")]
    public GameObject winLosePanel;
    public TextMeshProUGUI winLoseText;

    private void Awake()
    {
        Instance = this;
        winLosePanel.SetActive(false);
    }

    public void ShowWin(string message = "YOU WIN!")
    {
        winLoseText.text = message;
        winLosePanel.SetActive(true);
    }

    public void ShowLose(string message = "YOU LOSE!")
    {
        winLoseText.text = message;
        winLosePanel.SetActive(true);
    }
}

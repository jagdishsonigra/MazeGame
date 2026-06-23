using TMPro;
using UnityEngine;

public class LeaderboardRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI penaltyText;

    public void SetData(int rank, int score, string time, int penalties)
    {
        rankText.text = $"#{rank}";
        scoreText.text = score.ToString("N0");
        timeText.text = time;
        penaltyText.text = penalties.ToString();
    }

    public void Clear(int rank)
    {
        rankText.text = $"#{rank}";
        scoreText.text = "---";
        timeText.text = "--:--";
        penaltyText.text = "---";
    }
}
// PlayerScoreUI.cs
using UnityEngine;
using TMPro;

public class PlayerScoreUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI scoreText;

    public void Setup(string playerId, int score)
    {
        nameText.text  = playerId;
        scoreText.text = $"{score} / 100";
    }
}
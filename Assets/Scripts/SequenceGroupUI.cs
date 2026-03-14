// SequenceGroupUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SequenceGroupUI : MonoBehaviour
{
    [SerializeField] Transform  cardContainer;
    [SerializeField] GameObject miniCardPrefab;
    [SerializeField] TextMeshProUGUI ownerText;
    [SerializeField] TextMeshProUGUI scoreText;

    public void Setup(string ownerId, CardSequence sequence)
    {
        ownerText.text = ownerId;
        scoreText.text = $"+{sequence.ScoreValue}";

        foreach (var card in sequence.Cards)
        {
            var go = Instantiate(miniCardPrefab, cardContainer);
            go.GetComponent<Image>().color = card.data.elementColor;
            go.GetComponentInChildren<TextMeshProUGUI>().text = card.Value.ToString();
        }
    }
}
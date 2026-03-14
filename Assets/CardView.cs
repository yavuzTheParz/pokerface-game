// CardView.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardView : MonoBehaviour
{
    [SerializeField] Image background;
    [SerializeField] TextMeshProUGUI valueText;
    [SerializeField] TextMeshProUGUI elementText;

    Card runtimeCard;

    public void Init(Card card)
    {
        runtimeCard      = card;
        background.color = card.data.elementColor;
        valueText.text   = card.Value.ToString();
        elementText.text = card.Element.ToString();

        // Tıklanabilirlik için Button bileşeni de eklenebilir
        GetComponent<Button>()?.onClick.AddListener(OnCardClicked);
    }

    void OnCardClicked()
    {
        // Seçim mantığı UIManager'a event olarak gider
        UIManager.Instance?.OnCardSelected(runtimeCard);

    }
}
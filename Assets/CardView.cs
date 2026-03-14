// CardView.cs — güncellenmiş
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardView : MonoBehaviour
{
    [SerializeField] Image           background;
    [SerializeField] TextMeshProUGUI valueText;
    [SerializeField] TextMeshProUGUI elementText;
    [SerializeField] Image           selectionHighlight;

    Card runtimeCard;
    bool isSelected = false;

    public void Init(Card card)
    {
        runtimeCard      = card;
        background.color = card.data.elementColor;
        valueText.text   = card.Value.ToString();
        elementText.text = card.Element.ToString();

        selectionHighlight.gameObject.SetActive(false);
        GetComponent<Button>().onClick.AddListener(OnClicked);
    }

    void OnClicked()
    {
        GameUIManager.Instance?.OnCardSelected(runtimeCard, this);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        selectionHighlight.gameObject.SetActive(selected);

        // Seçili kart hafifçe yukarı kayar
        var pos = GetComponent<RectTransform>().anchoredPosition;
        pos.y = selected ? 20f : 0f;
        GetComponent<RectTransform>().anchoredPosition = pos;
    }
}
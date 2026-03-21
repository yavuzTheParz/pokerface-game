// CardView.cs — güncellenmiş
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardView : MonoBehaviour
{
    [SerializeField] Image           background;
    [SerializeField] TextMeshProUGUI valueText;
    [SerializeField] TextMeshProUGUI elementText;
    [SerializeField] Image           selectionBorder; // outline image

    public Card RuntimeCard { get; private set; }
    public bool IsSelected  { get; private set; }
    Vector2 originalPosition;

    public void Init(Card card)
    {
        RuntimeCard      = card;
        background.sprite = card.data.cardSprite;
        selectionBorder.sprite = card.data.cardSprite;
        valueText.text   = card.Value.ToString();
        elementText.text = card.Element.ToString();
        SetSelected(false);
        originalPosition = GetComponent<RectTransform>().anchoredPosition;

        GetComponent<Button>().onClick.AddListener(OnClicked);
    }

    void OnClicked()
    {
        GameUIManager.Instance?.OnCardSelected(RuntimeCard, this);
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;


        if (selectionBorder != null)
            selectionBorder.gameObject.SetActive(selected);
    }

    public void SetSequenceStyle()
    {
        // Hafif koyulaştır — dizide olduğunu belli et
        if (background != null)
        {
            Color c = RuntimeCard.data.elementColor;
            background.color = new Color(c.r * 0.85f, c.g * 0.85f, c.b * 0.85f);
        }
    }

}

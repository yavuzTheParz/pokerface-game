// CardRequestPanelUI.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardRequestPanelUI : MonoBehaviour
{
    [SerializeField] Transform  cardButtonContainer;
    [SerializeField] GameObject cardButtonPrefab;
    [SerializeField] Button     confirmBtn;
    [SerializeField] Button     cancelBtn;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI selectedCardText;

    CardData selectedCardData;

    void OnEnable()
    {
        selectedCardData = null;
        confirmBtn.interactable = false;
        selectedCardText.text = "Kart seç";
        titleText.text = "Hangi kartı istiyorsun?";

        confirmBtn.onClick.AddListener(OnConfirm);
        cancelBtn.onClick.AddListener(() => GameUIManager.Instance.CloseRequestPanel());
        cancelBtn.gameObject.SetActive(false);

        BuildCardList();
    }

    void OnDisable()
    {
        confirmBtn.onClick.RemoveAllListeners();
        cancelBtn.onClick.RemoveAllListeners();
    }

    // Elindeki kartlarla dizi kurabilecek kartları listele
    void BuildCardList()
    {
        foreach (Transform t in cardButtonContainer) Destroy(t.gameObject);

        string localId = NetworkManager.Instance.LocalPlayerId;
        var hand = CardManager.Instance.GetHand(localId);

        var shown = new HashSet<string>();

        // Tüm mümkün CardData'ları tara
        foreach (var cardData in CardManager.Instance.AllCardData)
        {
            string key = $"{cardData.element}_{cardData.value}";
            if (shown.Contains(key)) continue;

            // Zaten elde var mı? (aynı kartı tekrar isteme)
            bool alreadyInHand = hand.Cards.Exists(c => c.data == cardData);
            if (alreadyInHand) continue;

            // Bu kart elde dizi kurabilir mi?
            if (!hand.CanRequestCard(cardData)) continue;

            shown.Add(key);

            var go  = Instantiate(cardButtonPrefab, cardButtonContainer);
            var btn = go.GetComponent<Button>();
            var txt = go.GetComponentInChildren<TextMeshProUGUI>();
            txt.text = $"{cardData.element} {cardData.value}";

            var captured = cardData;
            btn.onClick.AddListener(() => SelectCard(captured));
        }
    }

    void SelectCard(CardData data)
    {
        selectedCardData = data;
        selectedCardText.text = $"Seçilen: {data.element} {data.value}";
        confirmBtn.interactable = true;
    }

    void OnConfirm()
    {
        if (selectedCardData == null) return;

        // Hedef yok — sistem kendisi bulur (2+ varsa elden, yoksa desteden)
        TurnManager.Instance.SubmitCardRequest(selectedCardData);
        GameUIManager.Instance.CloseRequestPanel();
        GameUIManager.Instance.RefreshHand();
    }
}
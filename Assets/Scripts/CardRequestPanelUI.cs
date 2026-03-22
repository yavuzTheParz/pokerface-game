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
        
        // cancelBtn.gameObject.SetActive(false); ← bunu sil
        // Zaten çift turda iptal yok, sadece interactable'ı kapat
        cancelBtn.gameObject.SetActive(false); // sadece görünümü kapat, butonu değil

        BuildCardList();
    }

    void OnDisable()
    {
        confirmBtn.onClick.RemoveAllListeners();
        cancelBtn.onClick.RemoveAllListeners();
    }

    // Elindeki kartlarla dizi kurabilecek kartları listele
   // CardRequestPanelUI.cs — BuildCardList başına ekle
void BuildCardList()
{
    foreach (Transform t in cardButtonContainer) Destroy(t.gameObject);


    string localId = NetworkManager.Instance.LocalPlayerId;
    var hand = CardManager.Instance.GetHand(localId);

    // Hand null ise henüz oyun başlamamış demek, paneli kapat
    if (hand == null)
    {
        Debug.LogWarning($"Hand bulunamadı: {localId}");
        GameUIManager.Instance.CloseRequestPanel();
        return;
    }


    Debug.Log($"AllCardData sayısı: {CardManager.Instance.AllCardData.Count}");
    Debug.Log($"Eldeki kartlar: {string.Join(", ", hand.Cards.ConvertAll(c => $"{c.Element}{c.Value}"))}");

    int gosterilen = 0;
    var shown = new HashSet<string>();

    foreach (var cardData in CardManager.Instance.AllCardData)
    {
        string key = $"{cardData.element}_{cardData.value}";
        if (shown.Contains(key)) continue;

        bool alreadyInHand = hand.Cards.Exists(c => c.data == cardData);
        if (alreadyInHand)
        {
            Debug.Log($"Zaten elde var, atla: {cardData.element}{cardData.value}");
            continue;
        }

        bool canRequest = hand.CanRequestCard(cardData);
        Debug.Log($"Kontrol: {cardData.element}{cardData.value} → CanRequest: {canRequest}");

        if (!canRequest) continue;

        shown.Add(key);
        gosterilen++;

        var go  = Instantiate(cardButtonPrefab, cardButtonContainer);
        var btn = go.GetComponent<Button>();
        var txt = go.GetComponentInChildren<TextMeshProUGUI>();
        txt.text = $"{cardData.element} {cardData.value}";
        var captured = cardData;
        btn.onClick.AddListener(() => SelectCard(captured));
    }

    Debug.Log($"Gösterilen kart sayısı: {gosterilen}");
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
        gameObject.SetActive(false); 
    }
}
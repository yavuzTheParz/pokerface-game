// GameUIManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Paneller")]
    [SerializeField] GameObject cardRequestPanel;
    [SerializeField] GameObject specialCardPanel;
    [SerializeField] GameObject notificationPanel;

    [Header("El kartları")]
    [SerializeField] Transform   handPanelContent;
    [SerializeField] GameObject  cardViewPrefab;

    [Header("Masa")]
    [SerializeField] Transform   sequenceContainer;
    [SerializeField] GameObject  sequenceGroupPrefab;
    [SerializeField] TextMeshProUGUI deckCountText;

    [Header("Üst bar")]
    [SerializeField] Transform   playerScoreContainer;
    [SerializeField] GameObject  playerScorePrefab;
    [SerializeField] TextMeshProUGUI turnIndicatorText;

    [Header("Butonlar")]
    [SerializeField] Button formSequenceBtn;
    [SerializeField] Button endTurnBtn;

    // Seçili kartlar
    readonly List<CardView> selectedCardViews = new();
    readonly List<Card>     selectedCards     = new();

    public void OpenRequestPanel()  => cardRequestPanel.SetActive(true);
    public void CloseRequestPanel() => cardRequestPanel.SetActive(false);
    public GameObject actButtons;

    public TurnManager tm;


   void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        formSequenceBtn.onClick.AddListener(OnFormSequenceClicked);
        endTurnBtn.onClick.AddListener(OnEndTurnClicked);

        // Event'leri dinle
        CardManager.Instance.OnSequenceFormed += OnSequenceFormed;
        TurnManager.Instance.OnTurnStarted    += OnTurnStarted;
        CardManager.Instance.OnPlayerWon      += OnPlayerWon;

        cardRequestPanel.SetActive(false);
        notificationPanel.SetActive(false);

        RefreshHand();
        RefreshScores();
    }

    void OnDestroy()
    {
        if (CardManager.Instance == null) return;
        CardManager.Instance.OnSequenceFormed -= OnSequenceFormed;
        TurnManager.Instance.OnTurnStarted    -= OnTurnStarted;
        CardManager.Instance.OnPlayerWon      -= OnPlayerWon;
    }

    // ── El kartları ─────────────────────────────────────────────

    public void RefreshHand()
    {

        foreach (Transform t in handPanelContent) Destroy(t.gameObject);
        selectedCards.Clear();
        selectedCardViews.Clear();

        string localId = NetworkManager.Instance.LocalPlayerId;
        var cards = CardManager.Instance.GetHandCards(localId);

        Debug.Log($"Local ID: {localId} | Kart sayısı: {cards.Count}");


        foreach (var card in cards)
        {
            var go = Instantiate(cardViewPrefab, handPanelContent);
            var cv = go.GetComponent<CardView>();
            cv.Init(card);
        }

        UpdateActionButtons();
    }

    // CardView tıklandığında çağrılır
    public void OnCardSelected(Card card, CardView view)
    {
        if (selectedCards.Contains(card))
        {
            selectedCards.Remove(card);
            selectedCardViews.Remove(view);
            view.SetSelected(false);
        }
        else
        {
            selectedCards.Add(card);
            selectedCardViews.Add(view);
            view.SetSelected(true);
        }
        UpdateActionButtons();
    }

    void UpdateActionButtons()
    {
        string localId = NetworkManager.Instance.LocalPlayerId;
        bool isMyTurn  = TurnManager.Instance.CurrentPlayerId == localId;
        Debug.Log($"UpdateActionButtons — isMyTurn: {isMyTurn} | Current: {TurnManager.Instance.CurrentPlayerId} | Local: {localId}");

        formSequenceBtn.interactable = isMyTurn && selectedCards.Count >= 4;
        endTurnBtn.interactable      = isMyTurn;
        if(isMyTurn && tm.IsOddTurn)
        {
            actButtons.SetActive(isMyTurn);    
        }
        if(isMyTurn && tm.IsEvenTurn)
        {
            cardRequestPanel.SetActive(isMyTurn);    
        }
        
    }

    // ── Dizi kurma ───────────────────────────────────────────────

    void OnFormSequenceClicked()
    {
        string localId = NetworkManager.Instance.LocalPlayerId;
        bool success = CardManager.Instance.TryFormSequence(localId, selectedCards);

        if (success)
        {
            ShowNotification("Dizi kuruldu!");
            RefreshHand();
            RefreshScores(); // sadece bu kalır, RefreshSequences() yok
        }
        else
        {
            ShowNotification("Geçersiz dizi — kuralları kontrol et.");
        }
    }


    // ── Dizi görünümü ───────────────────────────────────────────

    void OnSequenceFormed(string playerId, CardSequence seq)
    {
        RefreshScores();
        if (playerId == NetworkManager.Instance.LocalPlayerId)
            RefreshHand(); // Sadece kendi elini güncelle
    }

    // ── Skor güncelleme ──────────────────────────────────────────

    public void RefreshScores()
    {
        foreach (Transform t in playerScoreContainer) Destroy(t.gameObject);

        foreach (var playerId in NetworkManager.Instance.GetPlayerIds())
        {
            var hand  = CardManager.Instance.GetHand(playerId);
            var score = hand?.TotalScore ?? 0;
            var go    = Instantiate(playerScorePrefab, playerScoreContainer);
            go.GetComponent<PlayerScoreUI>().Setup(playerId, score);
        }
    }

    // ── Tur göstergesi ───────────────────────────────────────────

    void OnTurnStarted(string playerId, int turnNumber)
    {
        string localId = NetworkManager.Instance.LocalPlayerId;
        Debug.Log($"Tur başladı: {playerId} | Ben: {localId} | Benim turum: {playerId == localId}");
        string label   = playerId == localId ? "Senin Turun!" : $"{playerId} oynuyor...";
        turnIndicatorText.text = label;
        UpdateActionButtons();
        RefreshHand(); // Seçimleri sıfırla
    }

    // ── Kart talep paneli ────────────────────────────────────────


    // ── Tur sonu ─────────────────────────────────────────────────

    void OnEndTurnClicked()
    {
        GetComponent<GameNetworkBridge>()?.SendEndTurn();
        UpdateActionButtons();
        actButtons.SetActive(false);
    }

    // ── Event handler'lar ────────────────────────────────────────


    void OnPlayerWon(string playerId) =>
        ShowNotification($"{playerId} kazandı! 100 puana ulaştı.");

    // ── UIManager uyumluluk metodları ────────────────────────────

    public void ShowTurnIndicator(string playerId, int turn) =>
        OnTurnStarted(playerId, turn);

    public void ShowCardRequest(string from, string to, string cardName) =>
        ShowNotification($"{from}, {to}'dan {cardName} istedi.");

    public void ShowSequenceFormed(string playerId, string[] cardNames) =>
        ShowNotification($"{playerId} dizi kurdu.");

    public void ShowCurseFeedback(string msg) => ShowNotification(msg);

    public void ShowThiefResult(string from, string to, string[] cards) =>
        ShowNotification($"{from}, {to}'dan kart çaldı.");

    public void ShowSacrificeResult(string from, string to,
        string[] sacrificed, string[] destroyed) =>
        ShowNotification($"{from} kurban etti.");

    // ── Bildirim sistemi ─────────────────────────────────────────

    public void ShowNotification(string message)
    {
        StopAllCoroutines();
        notificationPanel.SetActive(true);
        notificationPanel.GetComponentInChildren<TextMeshProUGUI>().text = message;
        StartCoroutine(HideNotificationAfter(2.5f));
    }

    System.Collections.IEnumerator HideNotificationAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        notificationPanel.SetActive(false);
    }
}
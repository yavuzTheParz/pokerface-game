// GameUIManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

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

    [SerializeField] GameObject cursePanel;

    public void OpenCursePanel(Card card)
    {
        cursePanel.SetActive(true);
    }

    // Seçili kartlar
    readonly List<CardView> selectedCardViews = new();
    readonly List<Card>     selectedCards     = new();

   public void OpenRequestPanel()
{
    string localId = NetworkManager.Instance?.LocalPlayerId;
    var hand = CardManager.Instance?.GetHand(localId);
    
    if (hand == null)
    {
        Debug.LogWarning("El henüz hazır değil, panel açılmıyor.");
        return;
    }
    
    cardRequestPanel.SetActive(true);
}
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
        string localId = NetworkManager.Instance?.LocalPlayerId;
        if (localId == null) return;

        foreach (Transform t in handPanelContent) Destroy(t.gameObject);
        selectedCards.Clear();
        selectedCardViews.Clear();

        ShowSequenceInHand(localId); // sekanslar + el kartları
        UpdateActionButtons();
    }


    // CardView tıklandığında çağrılır
    public void OnCardSelected(Card card, CardView view)
    {
        if (TurnManager.Instance.CurrentPlayerId != NetworkManager.Instance.LocalPlayerId)
            return;

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



    // GameUIManager.cs
    public void UpdateActionButtons()
    {
        string localId = NetworkManager.Instance?.LocalPlayerId;
        if (string.IsNullOrEmpty(localId)) return;

        bool isMyTurn = TurnManager.Instance.CurrentPlayerId == localId;

        // ÖNEMLİ: Tur tipine (Tek/Çift) bakmaksızın, sıra bendeyse butonlar görünmeli!
        actButtons.SetActive(isMyTurn); 

        // Sekans Kur butonu: Seçili kart varsa aktif olsun
        formSequenceBtn.interactable = isMyTurn && selectedCards.Count >= 4;

        // Turu Bitir butonu: Sıra bendeyse hep aktif (kart çektikten/istedikten sonra basabilmesi için)
        endTurnBtn.interactable = isMyTurn;
    }

    // ── Dizi kurma ───────────────────────────────────────────────

    void OnFormSequenceClicked()
    {
        if (selectedCards.Count < 4)
        {
            ShowNotification("En az 4 kart seçmelisin.");
            return;
        }

        string localId = NetworkManager.Instance.LocalPlayerId;
        bool success = CardManager.Instance.TryFormSequence(localId, selectedCards);

        if (success)
        {
            ShowNotification("Sekans kuruldu!");
            RefreshHand();
            RefreshScores();
        }
        else
        {
            ShowNotification("Geçersiz sekans.");
            // Seçimi ve pozisyonları sıfırla
            foreach (var cv in selectedCardViews) cv.SetSelected(false);
            selectedCards.Clear();
            selectedCardViews.Clear();
            UpdateActionButtons();
        }
    }

    void ShowSequenceInHand(string playerId)
    {
        var hand = CardManager.Instance.GetHand(playerId);
        if (hand == null) return;

        foreach (Transform t in handPanelContent) Destroy(t.gameObject);
        selectedCards.Clear();
        selectedCardViews.Clear();

        // Önce sekansları göster — her kart direkt handPanelContent'e
        foreach (var seq in hand.Sequences)
            SpawnSequenceGroup(playerId, seq);

        // Sonra serbest kartlar
        foreach (var card in hand.Cards)
        {
            var go = Instantiate(cardViewPrefab, handPanelContent);
            go.GetComponent<CardView>().Init(card);
        }

        UpdateActionButtons();
    }


    void SpawnSequenceGroup(string playerId, CardSequence sequence)
    {
        // Kartları değere göre sırala
        var sorted = new List<Card>(sequence.Cards);
        sorted.Sort((a, b) => a.Value.CompareTo(b.Value));

        foreach (var card in sorted)
        {
            var cardGo = Instantiate(cardViewPrefab, handPanelContent);
            var cv = cardGo.GetComponent<CardView>();
            cv.Init(card);
            cv.SetSequenceStyle();
        }

        // Sekans sonu ayracı — ince dikey çizgi
        var divider = new GameObject("Divider");
        divider.transform.SetParent(handPanelContent, false);
        var divRect = divider.AddComponent<RectTransform>();
        divRect.sizeDelta = new Vector2(4, 80);
        var divImg = divider.AddComponent<Image>();
        divImg.color = new Color(1, 1, 1, 0.3f);
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

        foreach (var player in PhotonNetwork.PlayerList)
        {
            string playerId = player.ActorNumber.ToString();
            string nickname = player.NickName;
            var hand  = CardManager.Instance.GetHand(playerId);
            var score = hand?.TotalScore ?? 0;
            var go    = Instantiate(playerScorePrefab, playerScoreContainer);
            go.GetComponent<PlayerScoreUI>().Setup(nickname, score);
        }
    }

    // ── Tur göstergesi ───────────────────────────────────────────

    void OnTurnStarted(string playerId, int turnNumber)
    {
        // Önceki seçimleri temizle
        foreach (var cv in selectedCardViews) cv.SetSelected(false);
        selectedCards.Clear();
        selectedCardViews.Clear();

        string localId = NetworkManager.Instance.LocalPlayerId;
        string nickname = playerId;
        foreach (var p in PhotonNetwork.PlayerList)
        {
            if (p.ActorNumber.ToString() == playerId)
            {
                nickname = p.NickName;
                break;
            }
        }

        string label = playerId == localId ? "Senin Turun!" : $"{nickname} oynuyor...";
        turnIndicatorText.text = label;
        UpdateActionButtons();
        RefreshHand();
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
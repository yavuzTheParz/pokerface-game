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
        // Sadece kendi sıramda seçebiliriz
        if (TurnManager.Instance.CurrentPlayerId != NetworkManager.Instance.LocalPlayerId)
            return;

        // Dizideki kartlar seçilemez
        if (card.isInSequence) return;

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
        string localId = NetworkManager.Instance?.LocalPlayerId;
        if (localId == null) return;

        bool isMyTurn  = TurnManager.Instance.CurrentPlayerId == localId;
        bool isOddTurn  = TurnManager.Instance.IsOddTurn;
        bool isEvenTurn = TurnManager.Instance.IsEvenTurn;

        Debug.Log($"UpdateActionButtons — isMyTurn: {isMyTurn} | Current: {TurnManager.Instance.CurrentPlayerId} | Local: {localId}");

        // Tek tur — aksiyon butonları görünür
        actButtons.SetActive(isMyTurn && isOddTurn);

        // Çift tur — kart talep paneli açılır
        if (isMyTurn && isEvenTurn)
            cardRequestPanel.SetActive(true);

        // Sekans kur: sıra bende + tek tur + en az 4 kart seçili
        formSequenceBtn.interactable = isMyTurn && isOddTurn && selectedCards.Count >= 4;

        // Turu bitir: sıra bende + tek tur
        endTurnBtn.interactable = isMyTurn && isOddTurn;

        // Kart butonları: sıra bende + tek tur
        bool cardsClickable = isMyTurn && isOddTurn;
        foreach (Transform t in handPanelContent)
        {
            var cv = t.GetComponent<CardView>();
            if (cv != null)
                t.GetComponent<Button>().interactable = cardsClickable;

            // Sekans group içindeki kartlar her zaman pasif
            if (t.GetComponent<HorizontalLayoutGroup>() != null)
                foreach (Transform child in t)
                    if (child.GetComponent<Button>() != null)
                        child.GetComponent<Button>().interactable = false;
        }
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

        // Önce sekansları göster
        foreach (var seq in hand.Sequences)
            SpawnSequenceGroup(seq);

        // Sonra el kartlarını göster
        foreach (var card in hand.Cards)
        {
            var go = Instantiate(cardViewPrefab, handPanelContent);
            go.GetComponent<CardView>().Init(card);
        }

        UpdateActionButtons();
    }

    void SpawnSequenceGroup(CardSequence sequence)
    {
        // Sekans container'ı — renkli outline ile
        var groupGo = new GameObject("SequenceGroup");
        groupGo.transform.SetParent(handPanelContent, false);

        var groupRect = groupGo.AddComponent<RectTransform>();
        groupRect.sizeDelta = new Vector2(sequence.Cards.Count * 80 + 16, 120);

        var groupImage = groupGo.AddComponent<Image>();

        // Sekansın element rengini al — outline için
        if (sequence.Cards.Count > 0)
        {
            Color seqColor = sequence.Cards[0].data.elementColor;
            seqColor.a = 0.3f;
            groupImage.color = seqColor;
        }

        // Outline ekle
        var outline = groupGo.AddComponent<Outline>();
        if (sequence.Cards.Count > 0)
            outline.effectColor = sequence.Cards[0].data.elementColor;
        outline.effectDistance = new Vector2(3, -3);

        // Layout
        var layout = groupGo.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 4;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childControlWidth  = false;
        layout.childControlHeight = false;
        layout.childAlignment     = TextAnchor.MiddleCenter;

        // Kartları sıralı ekle
        var sorted = new System.Collections.Generic.List<Card>(sequence.Cards);
        sorted.Sort((a, b) => a.Value.CompareTo(b.Value));

        foreach (var card in sorted)
        {
            var cardGo = Instantiate(cardViewPrefab, groupGo.transform);
            var cv = cardGo.GetComponent<CardView>();
            cv.Init(card);
            // Sekans kartları tıklanamaz
            cardGo.GetComponent<Button>().interactable = false;
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
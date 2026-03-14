// TurnManager.cs
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    CardManager cm;

    List<string> playerOrder = new();
    int currentPlayerIndex = 0;
    int turnNumber = 0;

    public string CurrentPlayerId => playerOrder.Count > 0
        ? playerOrder[currentPlayerIndex] : "";

    public bool IsOddTurn  => turnNumber % 2 != 0;
    public bool IsEvenTurn => turnNumber % 2 == 0;

    // Kaç kart turunda bir meme/pokerface safhası başlar?
    [SerializeField] int memePhaseTriggerEvery = 3;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        cm = GetComponent<CardManager>();
    }

    // ── Oyunu başlat ────────────────────────────────────────────

    public void StartGame(List<string> orderedPlayerIds)
    {
        playerOrder = new List<string>(orderedPlayerIds);
        currentPlayerIndex = 0;
        turnNumber = 0;
        BeginCurrentPlayerTurn();
    }

    // ── Tur başlangıcı ──────────────────────────────────────────

    void BeginCurrentPlayerTurn()
    {
        OnTurnStarted?.Invoke(CurrentPlayerId, turnNumber);

        if (ShouldTriggerMemePhase())
        {
            OnMemePhaseTriggered?.Invoke();
            return; // Safha bitince ResumeTurn() çağrılır
        }

        ExecuteCardPhase();
    }

    void ExecuteCardPhase()
    {
        if (turnNumber == 0)
        {
            // İlk tur: ne kart dağıtımı ne talep — sadece oyuncu hamle yapar
            OnTurnStarted?.Invoke(CurrentPlayerId, turnNumber);
            return;
        }

        if (IsOddTurn)
            HandleOddTurn();
        else
            HandleEvenTurn();

    }

    // Tek tur: ortadan otomatik kart ver
    void HandleOddTurn()
    {
        var card = cm.DrawFromDeck(CurrentPlayerId);
        OnOddTurnCardDealt?.Invoke(CurrentPlayerId, card);
        // UI işlemi bitti → oyuncu dizi kurabilir, sonra EndTurn çağırır
    }

    // Çift tur: oyuncu kart talep etmeli
    void HandleEvenTurn()
    {
        // Buton yerine panel otomatik açılır
        GameUIManager.Instance?.OpenRequestPanel();
        OnEvenTurnRequestRequired?.Invoke(CurrentPlayerId);
    }


    // ── Dışarıdan çağrılan aksiyonlar ───────────────────────────

    // Çift tur: oyuncu kart talebini gönderir
    public void SubmitCardRequest(CardData requestedCard)
    {
        var result = cm.RequestCard(CurrentPlayerId, requestedCard);
        OnCardRequestResult?.Invoke(CurrentPlayerId, requestedCard, result);
    }

    // Oyuncu dizi kurmayı bitirdi, turu geçiyor
    public void EndTurn()
    {
        turnNumber++;
        currentPlayerIndex = (currentPlayerIndex + 1) % playerOrder.Count;
        BeginCurrentPlayerTurn();
    }

    // Meme/Pokerface safhası dışarıdan bittiğinde çağrılır
    public void ResumeTurn()
    {
        ExecuteCardPhase();
    }

    // ── Yardımcılar ─────────────────────────────────────────────

    bool ShouldTriggerMemePhase()
    {
        // Her memePhaseTriggerEvery tam turda bir tetikle
        // (turnNumber, tüm oyuncuların toplam tur sayısı)
        return turnNumber > 0 && turnNumber % memePhaseTriggerEvery == 0;
    }

    public string GetNextPlayerId()
    {
        int next = (currentPlayerIndex + 1) % playerOrder.Count;
        return playerOrder[next];
    }

    // ── Olaylar ─────────────────────────────────────────────────

    public event System.Action<string, int>                            OnTurnStarted;
    public event System.Action<string, Card>                           OnOddTurnCardDealt;
    public event System.Action<string>                                 OnEvenTurnRequestRequired;   
    public event System.Action<string, CardData, CardRequestResult> OnCardRequestResult;
    public event System.Action                                         OnMemePhaseTriggered;
}
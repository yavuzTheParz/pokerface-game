// GameNetworkBridge.cs
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class GameNetworkBridge : MonoBehaviourPun
{
    CardManager       cm;
    SpecialCardHandler sh;
    TurnManager       tm;

    void Start()
    {
        StartGameIfHost();
    }

    void Awake()
    {
        cm = GetComponent<CardManager>();
        sh = GetComponent<SpecialCardHandler>();
        tm = GetComponent<TurnManager>();
    }

    System.Collections.IEnumerator WaitAndStart()
    {
        if (!PhotonNetwork.IsMasterClient) yield break; // sadece host başlatır

        // Tüm oyuncular sahneye yüklenene kadar bekle
        int expectedCount = PhotonNetwork.CurrentRoom.PlayerCount;
        
        float timeout = 10f;
        float elapsed  = 0f;

        while (elapsed < timeout)
        {
            // Tüm oyuncular hazır mı? (basit yaklaşım: biraz bekle)
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;

            if (PhotonNetwork.PlayerList.Length >= expectedCount)
                break;
        }

        yield return new WaitForSeconds(0.5f); // son buffer
        StartGameIfHost();
    }


    [PunRPC]
    void RPC_StartGame(string[] playerIds, int seed)
    {
        Debug.Log($"Oyuncu sayısı: {playerIds.Length}");
        foreach (var id in playerIds)
            Debug.Log($"Oyuncu ID: {id}");

        Random.InitState(seed);
        CardManager.Instance.InitializeGame(new List<string>(playerIds));
        TurnManager.Instance.StartGame(new List<string>(playerIds));
        StartCoroutine(RefreshUINextFrame());
    }

    System.Collections.IEnumerator RefreshUINextFrame()
    {
        yield return null; // bir frame bekle
        GameUIManager.Instance?.RefreshHand();
        GameUIManager.Instance?.RefreshScores();
    }
    // OnEnable içini güncelle
    void OnEnable()
    {
        cm.OnSequenceFormed           += OnSequenceFormed_Local;
        cm.OnCardRequestedFromPlayers += OnCardRequestedFromPlayers_Local;
        cm.OnCardRequestedFromDeck    += OnCardRequestedFromDeck_Local;
        tm.OnTurnStarted              += OnTurnStarted_Local;

        sh.OnCursePlaced   += OnCursePlaced_Local;
        sh.OnCurseTriggered += OnCurseTriggered_Local;
        sh.OnCurseExpired  += OnCurseExpired_Local;
    }

    void OnDisable()
    {
        cm.OnSequenceFormed           -= OnSequenceFormed_Local;
        cm.OnCardRequestedFromPlayers -= OnCardRequestedFromPlayers_Local;
        cm.OnCardRequestedFromDeck    -= OnCardRequestedFromDeck_Local;
        tm.OnTurnStarted              -= OnTurnStarted_Local;

        sh.OnCursePlaced   += OnCursePlaced_Local;
        sh.OnCurseTriggered += OnCurseTriggered_Local;
        sh.OnCurseExpired  += OnCurseExpired_Local;
    }

    // Oyunculardan alındı
    void OnCardRequestedFromPlayers_Local(string requesterId,
        Dictionary<string, int> sources, CardData cardData)
    {
        // Dictionary'i string dizisine çevir (RPC sadece temel tipler alır)
        var sourceIds    = new List<string>(sources.Keys).ToArray();
        var sourceCounts = new List<int>(sources.Values).ToArray();

        photonView.RPC(nameof(RPC_CardRequestedFromPlayers), RpcTarget.All,
            requesterId, sourceIds, sourceCounts, cardData.name);
    }

    [PunRPC]
    void RPC_CardRequestedFromPlayers(string requesterId,
        string[] sourceIds, int[] sourceCounts, string cardName)
    {
        // Bildirim mesajı oluştur
        var parts = new List<string>();
        for (int i = 0; i < sourceIds.Length; i++)
            parts.Add($"{sourceIds[i]}'den {sourceCounts[i]} kart");

        string msg = $"{requesterId} → {cardName} aldı: {string.Join(", ", parts)}";
        GameUIManager.Instance?.ShowNotification(msg);
    }

    // Desteden alındı
    void OnCardRequestedFromDeck_Local(string requesterId, CardData cardData)
    {
        photonView.RPC(nameof(RPC_CardRequestedFromDeck), RpcTarget.All,
            requesterId, cardData.name);
    }

    [PunRPC]
    void RPC_CardRequestedFromDeck(string requesterId, string cardName)
    {
        string msg = $"{requesterId} → {cardName} desteden aldı";
        GameUIManager.Instance?.ShowNotification(msg);
}

    // ── Oyun başlangıcı (sadece host çağırır) ───────────────────

    public void StartGameIfHost()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var playerIds = NetworkManager.Instance.GetPlayerIds();

        // Host oyunu başlatır, seed ile deste sırasını senkronize eder
        int seed = Random.Range(0, 999999);
        photonView.RPC(nameof(RPC_StartGame), RpcTarget.All,
            playerIds.ToArray(), seed);
    }

    // ── Tur yönetimi ────────────────────────────────────────────

    void OnTurnStarted_Local(string playerId, int turnNumber)
    {
        photonView.RPC(nameof(RPC_TurnStarted), RpcTarget.Others,
            playerId, turnNumber);
    }

    [PunRPC]
    void RPC_TurnStarted(string playerId, int turnNumber)
    {
        
        GameUIManager.Instance?.RefreshHand();
        GameUIManager.Instance?.ShowTurnIndicator(playerId, turnNumber);
    }

    // ── Kart talebi ─────────────────────────────────────────────

    void OnCardRequested_Local(string requesterId, string targetId, Card card)
    {
        photonView.RPC(nameof(RPC_CardRequested), RpcTarget.Others,
            requesterId, targetId,
            card.data.name); // ScriptableObject asset adını gönder
    }

    [PunRPC]
    void RPC_CardRequested(string requesterId, string targetId, string cardAssetName)
    {
        UIManager.Instance?.ShowCardRequest(requesterId, targetId, cardAssetName);
    }

    // ── Dizi kuruldu ────────────────────────────────────────────

    void OnSequenceFormed_Local(string playerId, CardSequence sequence)
    {
        var cardNames = sequence.Cards.ConvertAll(c => c.data.name).ToArray();
        photonView.RPC(nameof(RPC_SequenceFormed), RpcTarget.Others,
            playerId, cardNames);
    }

    [PunRPC]
    void RPC_SequenceFormed(string playerId, string[] cardAssetNames)
    {
        UIManager.Instance?.ShowSequenceFormed(playerId, cardAssetNames);
    }

    // ── Lanet kartı ─────────────────────────────────────────────

    void OnCursePlaced_Local(string curserId, string cursedId)
    {
        // Sadece lanetleyene ve lanetlenene gönder, diğerleri bilmez
        photonView.RPC(nameof(RPC_CursePlaced), RpcTarget.All, curserId, cursedId);
    }

    [PunRPC]
    void RPC_CursePlaced(string curserId, string cursedId)
{
    string localId = NetworkManager.Instance.LocalPlayerId;
    if (localId == curserId)
        GameUIManager.Instance?.ShowNotification("Lanet yerleştirildi.");
    else if (localId == cursedId)
        GameUIManager.Instance?.ShowNotification("Üzerinde bir lanet var...");
}

void OnCurseTriggered_Local(string curserId, string cursedId, CardSequence seq)
{
    photonView.RPC(nameof(RPC_CurseTriggered), RpcTarget.All, curserId, cursedId);
}

[PunRPC]
void RPC_CurseTriggered(string curserId, string cursedId)
{
    GameUIManager.Instance?.ShowNotification($"Lanet tetiklendi! {cursedId} → {curserId}");
    GameUIManager.Instance?.RefreshHand();
    GameUIManager.Instance?.RefreshScores();
}

void OnCurseExpired_Local(string cursedId)
{
    photonView.RPC(nameof(RPC_CurseExpired), RpcTarget.All, cursedId);
}

[PunRPC]
void RPC_CurseExpired(string cursedId)
{
    string localId = NetworkManager.Instance.LocalPlayerId;
    if (localId == cursedId)
        GameUIManager.Instance?.ShowNotification("Üzerindeki lanet kalktı.");
}

    // ── Hırsız kartı ────────────────────────────────────────────

    void OnThiefResolved_Local(string requesterId, string targetId, List<Card> takenCards)
    {
        var cardNames = takenCards.ConvertAll(c => c.data.name).ToArray();
        photonView.RPC(nameof(RPC_ThiefResolved), RpcTarget.Others,
            requesterId, targetId, cardNames);
    }

    [PunRPC]
    void RPC_ThiefResolved(string requesterId, string targetId, string[] cardNames)
    {
        UIManager.Instance?.ShowThiefResult(requesterId, targetId, cardNames);
    }

    // ── Kurban kartı ────────────────────────────────────────────

    void OnSacrificeResolved_Local(string sacrificerId, string targetId,
        List<Card> sacrificed, List<Card> destroyed)
    {
        var sNames = sacrificed.ConvertAll(c => c.data.name).ToArray();
        var dNames = destroyed.ConvertAll(c => c.data.name).ToArray();
        photonView.RPC(nameof(RPC_SacrificeResolved), RpcTarget.Others,
            sacrificerId, targetId, sNames, dNames);
    }

    [PunRPC]
    void RPC_SacrificeResolved(string sacrificerId, string targetId,
        string[] sacrificedNames, string[] destroyedNames)
    {
        UIManager.Instance?.ShowSacrificeResult(
            sacrificerId, targetId, sacrificedNames, destroyedNames);
    }

    // ── Tur sonu (aktif oyuncu çağırır) ─────────────────────────

    public void SendEndTurn()
    {
        if (NetworkManager.Instance.LocalPlayerId != tm.CurrentPlayerId) return;
        photonView.RPC(nameof(RPC_EndTurn), RpcTarget.All);
    }

    [PunRPC]
    void RPC_EndTurn() => tm.EndTurn();
}

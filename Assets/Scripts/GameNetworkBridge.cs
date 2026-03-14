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

    void Awake()
    {
        cm = GetComponent<CardManager>();
        sh = GetComponent<SpecialCardHandler>();
        tm = GetComponent<TurnManager>();
    }

    void OnEnable()
    {
        cm.OnSequenceFormed          += OnSequenceFormed_Local;
        cm.OnCardRequestedFromPlayer += OnCardRequested_Local;
        sh.OnCursePlaced             += OnCursePlaced_Local;
        sh.OnThiefResolved           += OnThiefResolved_Local;
        sh.OnSacrificeResolved       += OnSacrificeResolved_Local;
        tm.OnTurnStarted             += OnTurnStarted_Local;
    }

    void OnDisable()
    {
        cm.OnSequenceFormed          -= OnSequenceFormed_Local;
        cm.OnCardRequestedFromPlayer -= OnCardRequested_Local;
        sh.OnCursePlaced             -= OnCursePlaced_Local;
        sh.OnThiefResolved           -= OnThiefResolved_Local;
        sh.OnSacrificeResolved       -= OnSacrificeResolved_Local;
        tm.OnTurnStarted             -= OnTurnStarted_Local;
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

    [PunRPC]
    void RPC_StartGame(string[] playerIds, int seed)
    {
        Random.InitState(seed); // Tüm cihazlarda aynı deste sırası
        cm.InitializeGame(new List<string>(playerIds));
        tm.StartGame(new List<string>(playerIds));
        Debug.Log($"Oyun başladı. Oyuncular: {string.Join(", ", playerIds)}");
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
        // Diğer oyuncuların UI'ını güncelle
        UIManager.Instance?.ShowTurnIndicator(playerId, turnNumber);
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
            UIManager.Instance?.ShowCurseFeedback("Lanet yerleştirildi.");
        else if (localId == cursedId)
            UIManager.Instance?.ShowCurseFeedback("Üzerinde bir lanet var...");
        // Diğer oyuncular hiçbir şey görmez
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

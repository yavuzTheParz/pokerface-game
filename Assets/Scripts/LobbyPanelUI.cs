// LobbyPanelUI.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class LobbyPanelUI : MonoBehaviourPunCallbacks
{
    [Header("UI Elemanları")]
    [SerializeField] TextMeshProUGUI roomCodeText;
    [SerializeField] Transform       playerListContent;
    [SerializeField] GameObject      playerRowPrefab;
    [SerializeField] Button          readyBtn;
    [SerializeField] Button          startBtn;   // sadece host görür
    [SerializeField] Button          leaveBtn;
    [SerializeField] TextMeshProUGUI statusText;

    // Yerel "hazır" durumu
    bool isReady = false;

    // Photon custom property anahtarı
    const string READY_KEY  = "ready";
    const string STARTED_KEY = "started";

    void Start()
    {
        readyBtn.onClick.AddListener(OnReadyClicked);
        startBtn.onClick.AddListener(OnStartClicked);
        leaveBtn.onClick.AddListener(() => LobbyManager.Instance.LeaveRoom());

        RefreshPlayerList();
        RefreshReadyButton();

        roomCodeText.text = $"Oda Kodu: {PhotonNetwork.CurrentRoom.Name}";
        startBtn.gameObject.SetActive(PhotonNetwork.IsMasterClient);
    }

    // ── Oyuncu listesini yenile ──────────────────────────────────

    public void RefreshPlayerList()
    {
        // Eski satırları temizle
        foreach (Transform child in playerListContent)
            Destroy(child.gameObject);

        foreach (var player in PhotonNetwork.PlayerList)
        {
            var row = Instantiate(playerRowPrefab, playerListContent);
            var ui  = row.GetComponent<PlayerRowUI>();

            bool ready = player.CustomProperties.TryGetValue(READY_KEY, out var r) && (bool)r;
            ui.Setup(player.NickName, ready, player.IsMasterClient);
        }

        UpdateStatusText();
        startBtn.interactable = AllPlayersReady() && PhotonNetwork.IsMasterClient;
                                //&& PhotonNetwork.CurrentRoom.PlayerCount >= 2;
    }

    public void RefreshReadyButton()
    {
        startBtn.gameObject.SetActive(PhotonNetwork.IsMasterClient);
    }

    // ── Hazır butonu ─────────────────────────────────────────────

    void OnReadyClicked()
    {
        isReady = !isReady;
        readyBtn.GetComponentInChildren<TextMeshProUGUI>().text =
            isReady ? "Hazır ✓" : "Hazır Değil";

        // Photon custom properties ile diğerlerine bildir
        var props = new ExitGames.Client.Photon.Hashtable { { READY_KEY, isReady } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    // ── Oyunu başlat (sadece host) ───────────────────────────────

    void OnStartClicked()
    {
        if (!AllPlayersReady() || PhotonNetwork.CurrentRoom.PlayerCount < 1) return;

        // Odayı kapat (yeni oyuncu giremesin)
        PhotonNetwork.CurrentRoom.IsOpen = false;

        // Oyun sahnesine geç
        PhotonNetwork.LoadLevel("GameScene");
    }

    // ── Photon callbacks ─────────────────────────────────────────

    public override void OnPlayerPropertiesUpdate(Player target, ExitGames.Client.Photon.Hashtable props)
        => RefreshPlayerList();

    public override void OnPlayerEnteredRoom(Player newPlayer) => RefreshPlayerList();
    public override void OnPlayerLeftRoom(Player other)        => RefreshPlayerList();

    // ── Yardımcılar ──────────────────────────────────────────────

    bool AllPlayersReady()
    {
        foreach (var p in PhotonNetwork.PlayerList)
        {
            bool ready = p.CustomProperties.TryGetValue(READY_KEY, out var r) && (bool)r;
            if (!ready) return false;
        }
        return true;
    }

    void UpdateStatusText()
    {
        int total = PhotonNetwork.CurrentRoom.PlayerCount;
        int ready = 0;
        foreach (var p in PhotonNetwork.PlayerList)
            if (p.CustomProperties.TryGetValue(READY_KEY, out var r) && (bool)r)
                ready++;

        statusText.text = $"{ready}/{total} oyuncu hazır";
    }
}
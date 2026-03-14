// LobbyManager.cs
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public static LobbyManager Instance { get; private set; }

    [Header("Paneller")]
    [SerializeField] GameObject mainMenuPanel;
    [SerializeField] GameObject createRoomPanel;
    [SerializeField] GameObject joinRoomPanel;
    [SerializeField] GameObject lobbyPanel;
    [SerializeField] GameObject connectingPanel; // "Bağlanıyor..." ekranı

    [Header("Lobi")]
    [SerializeField] LobbyPanelUI lobbyPanelUI;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        ShowPanel(connectingPanel);
        NetworkManager.Instance.OnConnected    += OnConnected;
        NetworkManager.Instance.OnRoomJoined   += OnRoomJoined;
        NetworkManager.Instance.OnRoomJoinFailed += OnRoomJoinFailed;
        NetworkManager.Instance.Connect();
    }

    void OnDestroy()
    {
        if (NetworkManager.Instance == null) return;
        NetworkManager.Instance.OnConnected      -= OnConnected;
        NetworkManager.Instance.OnRoomJoined     -= OnRoomJoined;
        NetworkManager.Instance.OnRoomJoinFailed -= OnRoomJoinFailed;
    }

    // ── Panel geçişleri ─────────────────────────────────────────

    void OnConnected()          => ShowPanel(mainMenuPanel);
    void OnRoomJoined()         => ShowPanel(lobbyPanel);
    void OnRoomJoinFailed(string msg) => ShowPanel(joinRoomPanel);

    void ShowPanel(GameObject target)
    {
        mainMenuPanel.SetActive(false);
        createRoomPanel.SetActive(false);
        joinRoomPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        connectingPanel.SetActive(false);
        target.SetActive(true);
    }

    // ── Buton aksiyonları (UI'dan çağrılır) ─────────────────────

    public void GoToCreateRoom() => ShowPanel(createRoomPanel);
    public void GoToJoinRoom()   => ShowPanel(joinRoomPanel);
    public void GoToMainMenu()   => ShowPanel(mainMenuPanel);

    public void CreateRoom(string roomName)
    {
        if (string.IsNullOrWhiteSpace(roomName)) return;
        ShowPanel(connectingPanel);
        NetworkManager.Instance.CreateRoom(roomName.ToUpper().Trim());
    }

    public void JoinRoom(string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode)) return;
        ShowPanel(connectingPanel);
        NetworkManager.Instance.JoinRoom(roomCode.ToUpper().Trim());
    }

    public void LeaveRoom()
    {
        NetworkManager.Instance.LeaveRoom();
        ShowPanel(mainMenuPanel);
    }

    // ── Photon callbacks ────────────────────────────────────────

    public override void OnPlayerEnteredRoom(Player newPlayer)
        => lobbyPanelUI.RefreshPlayerList();

    public override void OnPlayerLeftRoom(Player otherPlayer)
        => lobbyPanelUI.RefreshPlayerList();

    public override void OnMasterClientSwitched(Player newMaster)
        => lobbyPanelUI.RefreshReadyButton();
}
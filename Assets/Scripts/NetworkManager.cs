// NetworkManager.cs
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance { get; private set; }

    [Header("Oda Ayarları")]
    [SerializeField] int maxPlayers = 6;
    [SerializeField] string gameVersion = "1.0";

    public bool IsHost => PhotonNetwork.IsMasterClient;
    public string LocalPlayerId => PhotonNetwork.LocalPlayer.ActorNumber.ToString();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Bağlantı ────────────────────────────────────────────────

    public void Connect()
    {
        PhotonNetwork.AutomaticallySyncScene = true; // ← bunu ekle
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Photon'a bağlandı.");
        OnConnected?.Invoke();
    }

    // ── Oda yönetimi ────────────────────────────────────────────

    public void CreateRoom(string roomName)
    {
        var options = new RoomOptions
        {
            MaxPlayers = (byte)maxPlayers,
            IsVisible  = true,
            IsOpen     = true
        };
        PhotonNetwork.CreateRoom(roomName, options);
    }

    public void JoinRoom(string roomName) =>
        PhotonNetwork.JoinRoom(roomName);

    public void JoinRandomRoom() =>
        PhotonNetwork.JoinRandomRoom();

    public void LeaveRoom() =>
        PhotonNetwork.LeaveRoom();

    // ── Callbacks ───────────────────────────────────────────────

    public override void OnJoinedRoom()
    {
        Debug.Log($"Odaya girildi: {PhotonNetwork.CurrentRoom.Name}");
        OnRoomJoined?.Invoke();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Oyuncu katıldı: {newPlayer.ActorNumber}");
        OnPlayerJoined?.Invoke(newPlayer.ActorNumber.ToString());
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"Oyuncu ayrıldı: {otherPlayer.ActorNumber}");
        OnPlayerLeft?.Invoke(otherPlayer.ActorNumber.ToString());
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Odaya girilemedi: {message}");
        OnRoomJoinFailed?.Invoke(message);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Bağlantı kesildi: {cause}");
        OnDisconnectedFromServer?.Invoke();
    }

    // Odadaki tüm oyuncu ID'lerini sıralı döner
    public List<string> GetPlayerIds()
    {
        var ids = new List<string>();
        foreach (var p in PhotonNetwork.PlayerList)
            ids.Add(p.ActorNumber.ToString());
        ids.Sort();
        return ids;
    }

    // ── Olaylar ─────────────────────────────────────────────────

    public event System.Action          OnConnected;
    public event System.Action          OnRoomJoined;
    public event System.Action<string>  OnPlayerJoined;
    public event System.Action<string>  OnPlayerLeft;
    public event System.Action<string>  OnRoomJoinFailed;
    public event System.Action          OnDisconnectedFromServer;
}
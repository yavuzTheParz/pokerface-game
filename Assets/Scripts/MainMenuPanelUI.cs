// MainMenuPanelUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun; 

public class MainMenuPanelUI : MonoBehaviour
{
    [SerializeField] TMP_InputField nicknameInput;
    [SerializeField] Button createBtn;
    [SerializeField] Button joinBtn;

    void Start()
    {
        // Kaydedilmiş isim varsa doldur
        nicknameInput.text = PlayerPrefs.GetString("Nickname", "");
        createBtn.onClick.AddListener(OnCreateClicked);
        joinBtn.onClick.AddListener(OnJoinClicked);
    }

    void OnCreateClicked()
    {
        SaveNickname();
        LobbyManager.Instance.GoToCreateRoom();
    }

    void OnJoinClicked()
    {
        SaveNickname();
        LobbyManager.Instance.GoToJoinRoom();
    }

    void SaveNickname()
    {
        string nick = nicknameInput.text.Trim();
        if (string.IsNullOrEmpty(nick)) nick = "Oyuncu" + Random.Range(100, 999);
        PhotonNetwork.NickName = nick; // Photon'a kaydet (Pun namespace gerekli)
        PlayerPrefs.SetString("Nickname", nick);
    }
}
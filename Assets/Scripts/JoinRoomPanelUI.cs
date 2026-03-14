// JoinRoomPanelUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class JoinRoomPanelUI : MonoBehaviour
{
    [SerializeField] TMP_InputField roomCodeInput;
    [SerializeField] Button joinBtn;
    [SerializeField] Button backBtn;
    [SerializeField] TextMeshProUGUI errorText;

    void Start()
    {
        joinBtn.onClick.AddListener(OnJoinClicked);
        backBtn.onClick.AddListener(() => LobbyManager.Instance.GoToMainMenu());
        errorText.gameObject.SetActive(false);

        // Mobilde büyük harf klavye
        roomCodeInput.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
    }

    void OnJoinClicked()
    {
        string code = roomCodeInput.text.Trim();
        if (code.Length < 3)
        {
            errorText.text = "Geçerli bir oda kodu gir.";
            errorText.gameObject.SetActive(true);
            return;
        }
        errorText.gameObject.SetActive(false);
        LobbyManager.Instance.JoinRoom(code);
    }

    // Dışarıdan hata göstermek için (LobbyManager'dan çağrılır)
    public void ShowError(string msg)
    {
        errorText.text = msg;
        errorText.gameObject.SetActive(true);
    }
}
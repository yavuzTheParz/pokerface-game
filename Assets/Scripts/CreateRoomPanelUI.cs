// CreateRoomPanelUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CreateRoomPanelUI : MonoBehaviour
{
    [SerializeField] TMP_InputField roomNameInput;
    [SerializeField] Button createBtn;
    [SerializeField] Button backBtn;
    [SerializeField] TextMeshProUGUI errorText;

    void Start()
    {
        // Rastgele oda kodu öner
        roomNameInput.text = GenerateRoomCode();
        createBtn.onClick.AddListener(OnCreateClicked);
        backBtn.onClick.AddListener(() => LobbyManager.Instance.GoToMainMenu());
        errorText.gameObject.SetActive(false);
    }

    void OnCreateClicked()
    {
        string name = roomNameInput.text.Trim();
        if (name.Length < 3)
        {
            errorText.text = "En az 3 karakter gir.";
            errorText.gameObject.SetActive(true);
            return;
        }
        errorText.gameObject.SetActive(false);
        LobbyManager.Instance.CreateRoom(name);
    }

    string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        string code = "";
        for (int i = 0; i < 5; i++)
            code += chars[Random.Range(0, chars.Length)];
        return code;
    }
}
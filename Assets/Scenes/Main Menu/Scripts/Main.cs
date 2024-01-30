using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Photon;
using Photon.Realtime;

public class Main : PunBehaviour
{
    [field: SerializeField] public string PlayerName { get; private set; }
    [field: SerializeField] public string RoomCode { get; private set; }

    const string _gameVersion = "0.0.1";
    byte _maxPlayers = 4;

    public void SetPlayerName(string name) => PlayerName = name;
    public void SetRoomCode(string code) => RoomCode = code;

    [SerializeField] TMP_InputField _roomCodeInput;

    #region Unity Messages

    private void Awake()
    {
        PhotonNetwork.automaticallySyncScene = true;
    }

    #endregion

    #region PUN Callbacks

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Network.");
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"Created room with code {PhotonNetwork.room.Name}.");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"Joined room with code {PhotonNetwork.room.Name} as {PhotonNetwork.player.NickName}");

        if(PhotonNetwork.isMasterClient)
        {
            SceneManager.LoadScene(1);
        }
    }

    #endregion

    public void Connect()
    {
        // Already connected
        if(PhotonNetwork.connected)
        {
            Debug.Log("Attempted to connect but the client is already connected.");
        }
        else
        {
            PhotonNetwork.ConnectUsingSettings(_gameVersion);
        }
    }

    public void Disconnect()
    {
        if(PhotonNetwork.connected)
        {
            PhotonNetwork.Disconnect();
        }
        else
        {
            Debug.Log("Attempted to disconnect but the client is already disconnected.");
        }
    }

    public void JoinRoom()
    {
        if (!IsNameValid())
            return;

        if (!IsCodeValid())
            return;

        PhotonNetwork.player.NickName = PlayerName;
        Debug.Log($"Attempting to join the room with code {RoomCode} as {PlayerName}.");
        if(PhotonNetwork.connected)
        {
            PhotonNetwork.JoinOrCreateRoom(RoomCode, new RoomOptions { MaxPlayers = _maxPlayers }, TypedLobby.Default);
        }
        else
        {
            Debug.LogWarning("Attempted to join room but user is not connected to Photon Network.");
        }
    }

    public void GenerateRoomCode()
    {
        RoomCode = "";
        string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        for(int i = 0; i < 5; i++)
        {
            RoomCode += validChars[Random.Range(0, 26)];
        }

        _roomCodeInput.text = RoomCode;
    }

    private bool IsNameValid()
    {
        PlayerName = PlayerName.Trim();

        if (PlayerName == "")
        {
            Debug.LogWarning("Name is empty. Please enter a name!");
            return false;
        }

        return true;
    }

    private bool IsCodeValid()
    {
        RoomCode = RoomCode.Trim();

        if (RoomCode == "")
        {
            Debug.LogWarning("Code is empty. Please enter a room code!");
            return false;
        }

        if(RoomCode.Length != 5)
        {
            Debug.LogWarning("Code should be 5 characters in length!");
            return false;
        }

        foreach(char c in RoomCode)
        {
            if(!char.IsLetter(c))
            {
                Debug.LogWarning("Code should only contain letters!");
                return false;
            }
        }

        return true;
    }
}
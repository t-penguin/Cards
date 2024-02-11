using Photon;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManager : PunBehaviour
{
    #region Monobehaviour Callbacks

    private void Awake()
    {
        LogPlayersInRoom();
    }

    #endregion

    #region PUN Callbacks

    public override void OnJoinedRoom()
    {
        LogPlayersInRoom();
    }

    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        LogPlayersInRoom();
    }

    public override void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
    {
        LogPlayersInRoom();
    }

    #endregion

    public void LeaveRoom()
    {
        if(PhotonNetwork.connected)
        {
            PhotonNetwork.LeaveRoom();
            SceneManager.LoadScene(0);
        }
        else
        {
            Debug.Log("Attempted to leave the room but the client is not connected to Photon Network.");
        }
    }

    private void LogPlayersInRoom()
    {
        if(!PhotonNetwork.connected)
        {
            Debug.LogWarning("Attempted to log the players in a room," +
                "but the client is not connected to Photon Network.");
        }

        string players = "";
        foreach (PhotonPlayer player in PhotonNetwork.playerList)
        {
            players += $"{player.NickName}\n";
        }

        Debug.Log($"Number of players in room {PhotonNetwork.room.Name} " +
            $"is now {PhotonNetwork.room.PlayerCount}:\n{players}");
    }
}
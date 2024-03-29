using Photon;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class CasinoDebugInfo : PunBehaviour
{
    [SerializeField] TextMeshProUGUI _nameDisplay;
    [SerializeField] TextMeshProUGUI _listDisplay;

    void Start()
    {
        UpdateNameDisplay();
        UpdateListDisplay();
    }

    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        UpdateListDisplay();
    }

    public override void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
    {
        UpdateListDisplay();
    }

    public void UpdateNameDisplay()
    {
        _nameDisplay.text = $"Input Name: {Main.PlayerName}\n" +
                            $"Photon Name: {PhotonNetwork.playerName}";
    }

    public void UpdateListDisplay()
    {
        _listDisplay.text = "In room:\n";
        foreach (PhotonPlayer player in PhotonNetwork.playerList)
        {
            _listDisplay.text += $"{player.NickName}\n";
        }
    }

    public static void LogPlayerHands()
    {
        PhotonPlayer[] players = (PhotonPlayer[])PhotonNetwork.room.CustomProperties[GameManager.TURN_ORDER_KEY];

        foreach (PhotonPlayer player in players)
        {
            string log = $"{player.NickName}:\n";
            Hashtable properties = PhotonNetwork.room.CustomProperties;
            if (properties == null)
            {
                Debug.Log("Empty room properties...");
                continue;
            }

            foreach (string key in properties.Keys)
                Debug.Log($"Key: {key}");

            string[] cardNames = (string[])properties[$"{GameManager.PLAYER_HAND_KEY}: {player.NickName}"];
            if (cardNames == null)
            {
                Debug.Log("Empty cards array...");
                continue;
            }

            foreach (string cardName in cardNames)
                log += $"{cardName}\n";

            Debug.Log(log);
        }
    }

    public static void LogTurnOrder()
    {
        PhotonPlayer[] TurnOrder = (PhotonPlayer[])PhotonNetwork.room.CustomProperties[GameManager.TURN_ORDER_KEY];
        string order = "Turn Order:\n";
        foreach (PhotonPlayer player in TurnOrder)
            order += $"{player.NickName}\n";

        Debug.Log(order);
    }
}

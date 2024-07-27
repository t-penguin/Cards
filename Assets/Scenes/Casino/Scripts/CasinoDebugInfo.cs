using Photon;
using TMPro;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class CasinoDebugInfo : PunBehaviour
{
    [SerializeField] TextMeshProUGUI _setAndRoundDisplay;
    [SerializeField] TextMeshProUGUI _scoreDisplay;

    private void OnEnable()
    {
        PhotonNetwork.OnEventCall += UpdateSetAndRound;
        PhotonNetwork.OnEventCall += UpdatePlayerScores;
    }

    private void OnDisable()
    {
        PhotonNetwork.OnEventCall -= UpdateSetAndRound;
        PhotonNetwork.OnEventCall -= UpdatePlayerScores;
    }

    public void UpdateSetAndRound(byte eventCode, object content, int senderId)
    {
        if (eventCode != EventManager.GAME_DEAL_HAND_EVENT_CODE)
            return;

        int currentSet = (int)PhotonNetwork.room.CustomProperties[GameManager.CURRENT_SET_KEY];
        int currentRound = (int)PhotonNetwork.room.CustomProperties[GameManager.CURRENT_ROUND_KEY];
        int maxRounds = (int)PhotonNetwork.room.CustomProperties[GameManager.MAX_ROUNDS_KEY];

        _setAndRoundDisplay.text = $"Set: {currentSet} | Round: {currentRound} / {maxRounds}";
    }

    public void UpdatePlayerScores(byte eventCode, object content, int senderId)
    {
        if (eventCode != EventManager.START_GAME_EVENT_CODE &&
            eventCode != EventManager.GAME_SET_ENDED_EVENT_CODE)
            return;

        PhotonPlayer[] turnOrder = (PhotonPlayer[])PhotonNetwork.room.CustomProperties[GameManager.TURN_ORDER_KEY];
        int[] scores = (int[])PhotonNetwork.room.CustomProperties[GameManager.TEAM_SCORES_KEY];

        if (!(scores == null))
        {
            foreach (int score in scores)
                Debug.Log($"Score: {score}");
        }

        string txt = "Scores:\n";
        for (int i = 0; i < turnOrder.Length; i++)
        {
            int score = scores == null ? 0 : scores[i];
            txt += $"{turnOrder[i].NickName} - {score}\n";
        }

        Debug.Log(txt);
        _scoreDisplay.text = txt;
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

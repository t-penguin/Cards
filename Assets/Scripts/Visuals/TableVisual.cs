using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon;
using Unity.VisualScripting.Antlr3.Runtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using ExitGames.UtilityScripts;

public class TableVisual : PunBehaviour
{
    #region Temporary Test Purposes

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

    public static void LogPlayerHands(PhotonPlayer[] players)
    {
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

    #endregion

    public const byte TABLE_SET_EVENT_CODE = 2;

    private const int BOTTOM = 0;
    private const int LEFT = 1;
    private const int TOP = 2;
    private const int RIGHT = 3;

    [SerializeField] GameObject _cardVisualPrefab;
    [SerializeField] RectTransform _deck;
    [SerializeField] RectTransform[] _playerHands;
    [SerializeField] GameObject[] _playerPanels;
    [SerializeField] RectTransform _tableHand;
    [SerializeField] GameObject _tablePanel;
    [SerializeField] GameObject _startMenu;

    List<GameObject> _activePlayerPanels;
    List<RectTransform> _activePlayerHands;

    int _tablePlayerOffset;

    #region Monobehaviour Callbacks

    private void OnEnable()
    {
        PhotonNetwork.OnEventCall += OnTurnOrderSet;
        PhotonNetwork.OnEventCall += OnGameSetStarted;
    }

    private void OnDisable()
    {
        PhotonNetwork.OnEventCall -= OnTurnOrderSet;
        PhotonNetwork.OnEventCall -= OnGameSetStarted;
    }

    #endregion

    public IEnumerator DealToHand(Card[] cardsToDeal, List<PlayerManager> players, int playerOffset)
    {
        // Input validation
        int numPlayers = players.Count;
        if (numPlayers == 0)
        {
            Debug.LogError("No players to deal to!");
            yield break;
        }

        int numCards = cardsToDeal.Length;
        if (numCards == 0 || numCards != numPlayers * 4)
        {
            Debug.LogError($"Invalid number of cards to deal. Expected: {numPlayers * 4} - Actual: {numCards}");
            yield break;
        }

        playerOffset %= numPlayers;

        // FIXME: Get here eventually
        for(int i = 0; i < numCards; i++)
        {
            int index = (playerOffset + i) % numPlayers;
            PlayerManager currentPlayer = players[index];
            

        }
        yield break;
    }

    #region PUN Event Callbacks

    private void OnTurnOrderSet(byte eventCode, object content, int senderId)
    {
        if (eventCode != GameManager.TURN_ORDER_SET_EVENT_CODE)
            return;

        HideStartMenu();
        SetUpTable();
    }

    private void OnGameSetStarted(byte eventCode, object content, int senderId)
    {
        if (eventCode != GameManager.GAME_SET_STARTED)
            return;

        Debug.Log("Game Started");
        // Each client should first deal cards to each player
        // Once that is finished, each client should deal cards to the table
        StartCoroutine(StartDealing());
    }

    #endregion

    #region Table Set Up

    private void HideStartMenu() => _startMenu.SetActive(false);

    private void SetUpTable()
    {
        PhotonPlayer[] TurnOrder = 
            (PhotonPlayer[])PhotonNetwork.room.CustomProperties[GameManager.TURN_ORDER_KEY];

        int playerCount = TurnOrder.Length;
        _tablePlayerOffset = Array.IndexOf(TurnOrder, PhotonNetwork.player);

        if (_tablePlayerOffset == -1)
        {
            Debug.LogError("Player not found in the turn order...");
            return;
        }

        _activePlayerHands = new List<RectTransform>();
        _activePlayerPanels = new List<GameObject>();
        foreach (GameObject panel in _playerPanels)
            panel.SetActive(false);

        switch (playerCount)
        {
            default:
                Debug.LogError($"Invalid number of players... Got {playerCount} but expected 2, 3, or 4.");
                return;
            case 2: // Active Panels: Bottom, Top
                AddActive(BOTTOM);
                AddActive(TOP);
                break;
            case 3: // Active Panels: Bottom, Left, Right
                AddActive(BOTTOM);
                AddActive(LEFT);
                AddActive(RIGHT);
                break;
            case 4: // Active Panels: All
                AddActive(BOTTOM);
                AddActive(LEFT);
                AddActive(TOP);
                AddActive(RIGHT);
                break;
        }

        for (int i = 0; i < playerCount; i++)
        {
            PhotonPlayer player = TurnOrder[(i + _tablePlayerOffset) % playerCount];
            // FIXME: Correctly set the player ID image here
            _activePlayerPanels[i].GetComponentInChildren<IDCardVisual>()
                .SetIDCard(player.NickName, null);
        }

        foreach (GameObject panel in _activePlayerPanels)
            panel.SetActive(true);

        _deck.gameObject.SetActive(true);
        _tablePanel.SetActive(true);

        Debug.Log("Setting Table");

        if (PhotonNetwork.isMasterClient)
            PhotonNetwork.RaiseEvent(TABLE_SET_EVENT_CODE, eventContent: null,
                sendReliable: true, GameManager.EventOptions);
    }

    // Waits until the players have been dealt their cards, then deals to the table
    private IEnumerator StartDealing()
    {
        yield return StartCoroutine(DealToPlayers());
        yield return StartCoroutine(DealToTable());
    }

    // Visually deals cards from the deck to the players
    private IEnumerator DealToPlayers()
    {
        PhotonPlayer[] TurnOrder =
            (PhotonPlayer[])PhotonNetwork.room.CustomProperties[GameManager.TURN_ORDER_KEY];

        // Get the player hands from the room properties
        List<string[]> playerHands = new List<string[]>();
        foreach (PhotonPlayer player in TurnOrder)
        {
            string key = $"{GameManager.PLAYER_HAND_KEY}: {player.NickName}";
            playerHands.Add((string[])PhotonNetwork.room.CustomProperties[key]);
        }

        int playerCount = TurnOrder.Length;
        // Outer loop goes 4 times to give each player 4 cards
        for (int i = 0; i < 4; i++)
        {
            // Inner loop goes through each player
            for (int j = 0; j < playerCount; j++)
            {
                PhotonPlayer player = TurnOrder[j];
                int playerIndex = (playerCount - _tablePlayerOffset + j) % playerCount;

                RectTransform target = _activePlayerHands[playerIndex];
                string cardName = playerHands[j][i];
                bool flipCard = player == PhotonNetwork.player;

                yield return StartCoroutine(DealCard(target, cardName, flipCard));
            }
        }
        
        yield break;
    }

    // Visually deals cards from the deck to the table
    private IEnumerator DealToTable()
    {
        string[] cardNames = (string[])PhotonNetwork.room.CustomProperties[GameManager.TABLE_HAND_KEY];

        for (int i = 0; i < 4; i++)
        {
            yield return StartCoroutine(DealCard(_tableHand, cardNames[i], true));
        }
    }

    private IEnumerator DealCard(RectTransform targetHand, string cardName, bool flip)
    {
        Sprite backSprite = GameManager.GetCardSpriteByName($"Back_{GameManager.DeckColor}");

        // Create a card at the deck location face down
        GameObject cardVisual = Instantiate(_cardVisualPrefab, _deck);
        cardVisual.transform.SetParent(_deck.parent.transform);
        cardVisual.SetActive(false);

        RectTransform cardVisualTransform = cardVisual.GetComponent<RectTransform>();
        Image cardVisualImage = cardVisual.GetComponent<Image>();

        cardVisualImage.sprite = backSprite;
        cardVisual.SetActive(true);

        // Move and rotate the card to the target hand location
        Vector2 targetPosition = targetHand.anchoredPosition;
        Vector3 targetRotation = targetHand.eulerAngles;

        cardVisualTransform.anchorMin = targetHand.anchorMin;
        cardVisualTransform.anchorMax = targetHand.anchorMax;

        yield return MoveAndRotateCard(cardVisualTransform, targetPosition, targetRotation);

        cardVisualTransform.anchoredPosition = targetPosition;
        cardVisualTransform.eulerAngles = targetRotation;

        // Parent the card to the target hand
        cardVisualTransform.SetParent(targetHand);

        if (!flip)
            yield break;

        // Flip the card face up
        Sprite cardSprite = GameManager.GetCardSpriteByName(cardName);
        cardVisualImage.sprite = cardSprite;
        cardVisual.SetActive(true);

        yield break;
    }

    private IEnumerator MoveAndRotateCard(RectTransform cardTransform, Vector3 targetPosition, Vector3 targetRotation)
    {
        Debug.Log("Move and Rotate NYI...");
        yield break;
    }

    #endregion

    private void AddActive(int index)
    {
        _activePlayerHands.Add(_playerHands[index]);
        _activePlayerPanels.Add(_playerPanels[index]);
    }
}
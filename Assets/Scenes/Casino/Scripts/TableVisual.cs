using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class TableVisual : PunBehaviour
{
    private const int BOTTOM = 0;
    private const int LEFT = 1;
    private const int TOP = 2;
    private const int RIGHT = 3;

    public static List<CardInteraction> TableHand = new List<CardInteraction>();
    public static TableSize TableSize = TableSize.Default;

    [SerializeField] GameObject _cardVisualPrefab;
    [SerializeField] GameObject _stackVisualPrefab;

    [SerializeField] RectTransform _deck;
    [SerializeField] RectTransform[] _playerHands;
    [SerializeField] GameObject[] _playerPanels;
    [SerializeField] RectTransform _tableHand;
    [SerializeField] GameObject _tablePanel;
    [SerializeField] GameObject _startMenu;

    List<GameObject> _activePlayerPanels;
    List<RectTransform> _activePlayerHands;

    int _playerCount;
    int _tablePlayerOffset;

    Vector2 _defaultTableSize = new Vector2(550, 575);
    Vector2 _mediumTableSize = new Vector2(820, 575);
    Vector2 _largeTableSize = new Vector2(890, 575);

    Vector2 _defaultCellSize = new Vector2(125, 175);
    Vector2 _smallCellSize = new Vector2(100, 140);
    Vector2 _teenyCellSize = new Vector2(75, 105);

    Vector2 _defaultSelectorSize = new Vector2(140, 190);
    Vector2 _smallSelectorSize = new Vector2(115, 155);
    Vector2 _teenySelectorSize = new Vector2(82, 112);

    Vector2 _defaultSpacing = new Vector2(10, 10);
    Vector2 _smallSpacing = new Vector2(5, 10);

    Color _transparent = new Color(0, 0, 0, 0);

    #region Monobehaviour Callbacks

    private void OnEnable()
    {
        PhotonNetwork.OnEventCall += OnTurnOrderSet;
        PhotonNetwork.OnEventCall += OnGameSetStarted;
        PhotonNetwork.OnEventCall += OnDroppedCard;
        PhotonNetwork.OnEventCall += OnStackedCards;
    }

    private void OnDisable()
    {
        PhotonNetwork.OnEventCall -= OnTurnOrderSet;
        PhotonNetwork.OnEventCall -= OnGameSetStarted;
        PhotonNetwork.OnEventCall -= OnDroppedCard;
        PhotonNetwork.OnEventCall -= OnStackedCards;
    }

    #endregion

    #region PUN Event Callbacks

    private void OnTurnOrderSet(byte eventCode, object content, int senderId)
    {
        if (eventCode != EventManager.TURN_ORDER_SET_EVENT_CODE)
            return;

        HideStartMenu();
        SetUpTable();
    }

    private void OnGameSetStarted(byte eventCode, object content, int senderId)
    {
        if (eventCode != EventManager.GAME_SET_STARTED)
            return;

        Debug.Log("Game Started");
        // Each client should first deal cards to each player
        // Once that is finished, each client should deal cards to the table
        StartCoroutine(StartDealing());
    }

    private void OnDroppedCard(byte eventCode, object content, int senderId)
    {
        if (eventCode != EventManager.TURN_DROP_CARD_EVENT_CODE)
            return;

        int[] dropInfo = (int[])content;
        StartCoroutine(DropCard(dropInfo));
    }

    private void OnStackedCards(byte eventCode, object content, int senderId)
    {
        if (eventCode != EventManager.TURN_STACK_CARDS_EVENT_CODE)
            return;

        int[] stackInfo = (int[])content;
        StartCoroutine(StackCards(stackInfo));
    }

    private void OnTookCards(byte eventCode, object content, int senderId)
    {

    }

    #endregion

    #region Table Set Up

    private void HideStartMenu() => _startMenu.SetActive(false);

    private void SetUpTable()
    {
        PhotonPlayer[] TurnOrder = 
            (PhotonPlayer[])PhotonNetwork.room.CustomProperties[GameManager.TURN_ORDER_KEY];

        _playerCount = TurnOrder.Length;
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

        switch (_playerCount)
        {
            default:
                Debug.LogError($"Invalid number of players... Got {_playerCount} but expected 2, 3, or 4.");
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

        for (int i = 0; i < _playerCount; i++)
        {
            PhotonPlayer player = TurnOrder[(i + _tablePlayerOffset) % _playerCount];
            // TODO: [CARDS] Correctly set the player ID image here
            _activePlayerPanels[i].GetComponentInChildren<IDCardVisual>()
                .SetIDCard(player.NickName, null);
        }

        foreach (GameObject panel in _activePlayerPanels)
            panel.SetActive(true);

        _deck.gameObject.SetActive(true);
        _tablePanel.SetActive(true);

        Debug.Log("Setting Table");

        if (PhotonNetwork.isMasterClient)
            EventManager.RaisePhotonEvent(EventManager.TABLE_SET_EVENT_CODE);
    }

    #endregion

    #region Table Animations

    // Waits until the players have been dealt their cards, then deals to the table
    private IEnumerator StartDealing()
    {
        yield return StartCoroutine(DealToPlayers());

        int round = (int)PhotonNetwork.room.CustomProperties[GameManager.CURRENT_ROUND_KEY];
        if (round == 1)
            yield return StartCoroutine(DealToTable());

        if (PhotonNetwork.isMasterClient)
            EventManager.RaisePhotonEvent(EventManager.TURN_STARTED_EVENT_CODE);
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
        SetTableSize(TableSize.Default);

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
        cardVisual.transform.SetParent(_deck.parent);
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

        //cardVisualTransform.anchoredPosition = targetPosition;
        //cardVisualTransform.eulerAngles = targetRotation;

        // Parent the card to the target hand
        cardVisualTransform.SetParent(targetHand);

        SingleCardInteraction cardInteraction = cardVisual.GetComponent<SingleCardInteraction>();
        cardInteraction.SetCard(new Card(cardName));

        if (!flip)
            yield break;

        // Flip the card face up
        Sprite cardSprite = GameManager.GetCardSpriteByName(cardName);
        cardVisualImage.sprite = cardSprite;

        // Cards that are flipped are also interactable
        cardInteraction.CardType = targetHand == _tableHand ? CardType.OnTable : CardType.InHand;

        // Cards on the table should be added to the table hand list
        if(cardInteraction.CardType == CardType.OnTable)
            TableHand.Add(cardInteraction);

        yield break;
    }

    private IEnumerator DropCard(int[] dropInfo)
    {
        // Sender Info
        int senderIndex = dropInfo[0];
        Debug.Log($"Sender index in the turn order is: {senderIndex}");
        int senderTableIndex = (senderIndex - _tablePlayerOffset + _playerCount) % _playerCount;
        RectTransform senderHand = _activePlayerHands[senderTableIndex];
        SingleCardInteraction[] senderCards = senderHand.GetComponentsInChildren<SingleCardInteraction>();

        // Card Info
        int cardIndex = dropInfo[1];
        Debug.Log($"Card index in sender's hand is: {cardIndex}");
        SingleCardInteraction card = senderCards[cardIndex];
        RectTransform cardTransform = card.GetComponent<RectTransform>();

        // Move card and resize if necessary
        Vector3 targetPosition = _tableHand.anchoredPosition;
        Vector3 targetRotation = _tableHand.eulerAngles;

        cardTransform.SetParent(_deck.parent);
        yield return StartCoroutine(MoveAndRotateCard(cardTransform, targetPosition, targetRotation));
        cardTransform.SetParent(_tableHand);
        
        // Turn the card face up and add it to the table hand
        Image cardImage = card.GetComponent<Image>();
        string cardName = card.Card.Name;
        Sprite cardSprite = GameManager.GetCardSpriteByName(cardName);
        cardImage.sprite = cardSprite;
        card.CardType = CardType.OnTable;

        TableHand.Add(card);
        CheckTableSize();

        // End Turn
        if (PhotonNetwork.isMasterClient)
            EventManager.RaisePhotonEvent(EventManager.TURN_ENDED_EVENT_CODE);

        yield break;
    }

    private IEnumerator StackCards(int[] stackInfo)
    {
        // Sender Info
        int senderIndex = stackInfo[0];
        Debug.Log($"Sender index in the turn order is: {senderIndex}");
        int senderTableIndex = (senderIndex - _tablePlayerOffset + _playerCount) % _playerCount;
        RectTransform senderHand = _activePlayerHands[senderTableIndex];
        SingleCardInteraction[] senderCards = senderHand.GetComponentsInChildren<SingleCardInteraction>();

        // Stack Info
        int stackValue = stackInfo[1];
        bool locked = stackInfo[2] == 1;
        Debug.Log($"Stack value: {stackValue}\nLocked: {locked}");

        // Sender's Target Card Info
        SingleCardInteraction stackTargetCard = null;
        if (senderTableIndex == 0)
        {
            int targetCardIndex = stackInfo[3];
            stackTargetCard = senderCards[targetCardIndex];
        }

        // Sender's Hand Card Info
        int senderCardIndex = stackInfo[4];
        Debug.Log($"Card index in sender's hand is: {senderCardIndex}");
        SingleCardInteraction senderCard = senderCards[senderCardIndex];
        RectTransform senderCardTransform = senderCard.GetComponent<RectTransform>();

        // Table Card(s) Info
        int[] tableCardIndexes = stackInfo[5..];
        int numTableCards = tableCardIndexes.Length;

        // DEBUG
        string infoArray = "";
        foreach (int info in stackInfo)
            infoArray += $"{info} ";

        Debug.Log($"Info Array: {infoArray}");
        Debug.Log($"Number of table cards in stack: {numTableCards}");
        // END DEBUG

        CardInteraction[] tableCards = new CardInteraction[numTableCards];
        for (int i = 0; i < numTableCards; i++)
        {
            int cardIndex = tableCardIndexes[i];
            tableCards[i] = TableHand[cardIndex];
        }

        Vector3 targetPosition = tableCards[0].GetComponent<RectTransform>().anchoredPosition;
        List<GameObject> visualCards = new List<GameObject>();

        foreach (CardInteraction tableCard in tableCards)
        {
            // Create a visual card for the current table card
            Vector3 cardPosition = tableCard.GetComponent<RectTransform>().anchoredPosition;
            GameObject cardVisual = Instantiate(_cardVisualPrefab, cardPosition, Quaternion.identity, _deck);
            visualCards.Add(cardVisual);
            RectTransform cardTransform = cardVisual.GetComponent<RectTransform>();

            // Change the actual card's image alpha to 0
            // This keeps all the table cards in their positions until the stack is finished
            tableCard.GetComponent<Image>().color = _transparent;

            // Move the visual card to the target position
            yield return StartCoroutine(MoveCard(cardTransform, targetPosition));
        }

        // Once all visual cards have been moved, move the sender's card to the target position
        senderCardTransform.SetParent(_deck.parent);
        yield return StartCoroutine(MoveCard(senderCardTransform, targetPosition));

        // Create the stack card at the target position
        GameObject stackVisual = Instantiate(_stackVisualPrefab, targetPosition, Quaternion.identity, _deck);
        StackedCardsInteraction stack = stackVisual.GetComponent<StackedCardsInteraction>();
        List<CardInteraction> cardsInStack = new List<CardInteraction>{ senderCard };
        cardsInStack.AddRange(tableCards);
        stack.CreateStack(senderIndex, stackValue, locked, cardsInStack, stackTargetCard);
        stackVisual.SetActive(true);

        // Delete all visual cards
        foreach (GameObject card in visualCards)
            Destroy(card);
        visualCards.Clear();

        // Delete all actual cards and the sender's card
        foreach (CardInteraction card in tableCards)
            DeleteTableCard(card);

        Destroy(senderCard.gameObject);

        // Add the stack to the table hand at the target position
        int siblingIndex = tableCards[0].transform.GetSiblingIndex();
        stackVisual.transform.SetParent(_tableHand);
        stackVisual.transform.SetSiblingIndex(siblingIndex);
        TableHand.Add(stack);

        CheckTableSize();

        // End Turn
        if (PhotonNetwork.isMasterClient)
            EventManager.RaisePhotonEvent(EventManager.TURN_ENDED_EVENT_CODE);

        yield break;
    }

    private IEnumerator MoveCard(RectTransform cardTransform, Vector3 targetPosition)
    {
        Debug.Log("Move Card NYI...");
        cardTransform.anchoredPosition = targetPosition;

        yield break;
    }

    private IEnumerator MoveAndRotateCard(RectTransform cardTransform, Vector3 targetPosition, Vector3 targetRotation)
    {
        Debug.Log("Move and Rotate NYI...");
        cardTransform.anchoredPosition = targetPosition;
        cardTransform.eulerAngles = targetRotation;

        yield break;
    }

    #endregion

    private void AddActive(int index)
    {
        _activePlayerHands.Add(_playerHands[index]);
        _activePlayerPanels.Add(_playerPanels[index]);
    }

    private void CheckTableSize()
    {
        int numCards = TableHand.Count;

        Debug.Log($"There are {numCards} cards on the table");

        TableSize targetSize;
        if (numCards > 24)
            targetSize = TableSize.ExtraLarge;
        else if (numCards > 18)
            targetSize = TableSize.Large;
        else if (numCards > 12)
            targetSize = TableSize.Medium;
        else
            targetSize = TableSize.Default;

        Debug.Log($"Table size: {TableSize} / Target size: {targetSize}");

        if (TableSize != targetSize)
            SetTableSize(targetSize);
    }

    private void SetTableSize(TableSize targetSize)
    {
        GridLayoutGroup layout = _tableHand.GetComponent<GridLayoutGroup>();

        Vector2 selectorSize = _defaultSelectorSize;

        switch (targetSize)
        {
            case TableSize.Default:
                _tableHand.sizeDelta = _defaultTableSize;
                layout.cellSize = _defaultCellSize;
                layout.spacing = _defaultSpacing;
                break;
            case TableSize.Medium:
                _tableHand.sizeDelta = _mediumTableSize;
                layout.cellSize = _defaultCellSize;
                layout.spacing = _defaultSpacing;
                break;
            case TableSize.Large:
                _tableHand.sizeDelta = _largeTableSize;
                selectorSize = _smallSelectorSize;
                layout.cellSize = _smallCellSize;
                layout.spacing = _defaultSpacing;
                break;
            case TableSize.ExtraLarge:
                _tableHand.sizeDelta = _largeTableSize;
                selectorSize = _teenySelectorSize;
                layout.cellSize = _teenyCellSize;
                layout.spacing = _smallSpacing;
                break;
        }

        TableSize = targetSize;

        if (TableHand.Count == 0)
            return;

        foreach (CardInteraction card in TableHand)
        {
            card.ResizeSelector(selectorSize);
            StackedCardsInteraction stack = card as StackedCardsInteraction;
            if (stack == null)
                continue;

            stack.ResizeCard();
        }

        Debug.Log($"Selector size should be {selectorSize.x}x{selectorSize.y}");
    }

    private void DeleteTableCard(CardInteraction card)
    {
        if (card == null) 
            return;

        if (!TableHand.Contains(card))
            return;

        TableHand.Remove(card);
        Destroy(card.gameObject);
    }
}

public enum TableSize
{
    Default,
    Medium,
    Large,
    ExtraLarge
}
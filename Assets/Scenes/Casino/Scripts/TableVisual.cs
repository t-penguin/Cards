using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using TMPro;

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
    [SerializeField] RectTransform[] _pickupPiles;
    [SerializeField] RectTransform _tableHand;
    [SerializeField] GameObject _tablePanel;
    [SerializeField] GameObject _startMenu;

    List<GameObject> _activePlayerPanels = new();
    List<RectTransform> _activePlayerHands = new();
    List<RectTransform> _activePickupPiles = new();

    int _playerCount;
    int _tablePlayerOffset;
    int _lastPickedUpIndex;
    Vector2 _tablePanelOffset;

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

    #region Monobehaviour Callbacks

    private void OnEnable()
    {
        PhotonNetwork.OnEventCall += OnSetTable;
        PhotonNetwork.OnEventCall += OnStartGame;
        PhotonNetwork.OnEventCall += OnDealHand;
        PhotonNetwork.OnEventCall += OnGameSetEnded;
        PhotonNetwork.OnEventCall += OnDroppedCard;
        PhotonNetwork.OnEventCall += OnStackedCards;
        PhotonNetwork.OnEventCall += OnPickedUpCards;
        PhotonNetwork.OnEventCall += OnClearTable;
    }

    private void OnDisable()
    {
        PhotonNetwork.OnEventCall -= OnSetTable;
        PhotonNetwork.OnEventCall -= OnStartGame;
        PhotonNetwork.OnEventCall -= OnDealHand;
        PhotonNetwork.OnEventCall -= OnGameSetEnded;
        PhotonNetwork.OnEventCall -= OnDroppedCard;
        PhotonNetwork.OnEventCall -= OnStackedCards;
        PhotonNetwork.OnEventCall -= OnPickedUpCards;
        PhotonNetwork.OnEventCall -= OnClearTable;
    }

    private void Awake()
    {
        _tablePanelOffset = _tablePanel.GetComponent<RectTransform>().anchoredPosition;
    }

    #endregion

    #region PUN Event Callbacks

    private void OnSetTable(byte eventCode, object content, int senderID)
    {
        if (eventCode != EventManager.SET_TABLE_EVENT_CODE)
            return;

        HideStartMenu();
        SetUpTable();
    }

    private void OnStartGame(byte eventCode, object content, int senderID)
    {
        if (eventCode != EventManager.START_GAME_EVENT_CODE)
            return;

        PhotonPlayer[] TurnOrder =
            (PhotonPlayer[])PhotonNetwork.room.CustomProperties[GameManager.TURN_ORDER_KEY];

        _tablePlayerOffset = Array.IndexOf(TurnOrder, PhotonNetwork.player);
    }

    private void OnDealHand(byte eventCode, object content, int senderID)
    {
        if (eventCode != EventManager.GAME_DEAL_HAND_EVENT_CODE)
            return;

        Debug.Log("Game Started");
        // Each client should first deal cards to each player
        // Once that is finished, each client should deal cards to the table
        StartCoroutine(StartDealing());
    }

    private void OnGameSetEnded(byte eventCode, object content, int senderID)
    {
        if (eventCode != EventManager.GAME_SET_ENDED_EVENT_CODE)
            return;

        ResetDeck();
        ResetPickUpPiles();
    }

    private void OnDroppedCard(byte eventCode, object content, int senderID)
    {
        if (eventCode != EventManager.TURN_DROP_CARD_EVENT_CODE)
            return;

        Debug.Log("Recieved event to drop card");
        int[] dropInfo = (int[])content;
        StartCoroutine(Drop(dropInfo));
    }

    private void OnStackedCards(byte eventCode, object content, int senderID)
    {
        if (eventCode != EventManager.TURN_STACK_CARDS_EVENT_CODE)
            return;

        int[] stackInfo = (int[])content;
        StartCoroutine(Stack(stackInfo));
    }

    private void OnPickedUpCards(byte eventCode, object content, int senderID)
    {
        if (eventCode != EventManager.TURN_PICK_UP_CARDS_EVENT_CODE)
            return;

        int[] pickUpInfo = (int[])content;
        StartCoroutine(PickUp(pickUpInfo));
    }

    private void OnClearTable(byte eventCode, object content, int senderID)
    {
        if (eventCode != EventManager.TABLE_CLEAR_EVENT_CODE)
            return;

        StartCoroutine(ClearTable());
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

        TableHand.Clear();
        _activePlayerHands.Clear();
        _activePlayerPanels.Clear();
        _activePickupPiles.Clear();
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

                _activePickupPiles.Add(_pickupPiles[BOTTOM]);
                _activePickupPiles.Add(_pickupPiles[TOP]);
                break;
            case 3: // Active Panels: Bottom, Left, Right
                AddActive(BOTTOM);
                AddActive(LEFT);
                AddActive(RIGHT);

                _activePickupPiles.Add(_pickupPiles[BOTTOM]);
                _activePickupPiles.Add(_pickupPiles[LEFT]);
                _activePickupPiles.Add(_pickupPiles[RIGHT]);
                break;
            case 4: // Active Panels: All
                AddActive(BOTTOM);
                AddActive(LEFT);
                AddActive(TOP);
                AddActive(RIGHT);

                _activePickupPiles.Add(_pickupPiles[BOTTOM]);
                _activePickupPiles.Add(_pickupPiles[LEFT]);
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

        foreach (RectTransform pile in _activePickupPiles)
            pile.GetComponent<PileVisual>().ResetPile();

        _deck.gameObject.SetActive(true);
        _tablePanel.SetActive(true);

        EventManager.RaisePhotonEvent(EventManager.START_GAME_EVENT_CODE);
    }

    private void ResetPickUpPiles()
    {
        if (_activePickupPiles == null)
            return;

        foreach (RectTransform pile in _activePickupPiles)
            pile.GetComponent<PileVisual>().ResetPile();
    }

    private void ResetDeck() => _deck.GetComponent<DeckVisual>().ResetDeck();

    #endregion

    #region Table Animations

    // Waits until the players have been dealt their cards, then deals to the table
    private IEnumerator StartDealing()
    {
        yield return StartCoroutine(DealToPlayers());

        int round = (int)PhotonNetwork.room.CustomProperties[GameManager.CURRENT_ROUND_KEY];
        if (round == 1)
        {
            yield return StartCoroutine(DealToTable());
            EventManager.RaisePhotonEvent(EventManager.GAME_SET_STARTED_EVENT_CODE);
        }

        EventManager.RaisePhotonEvent(EventManager.GAME_START_TURN_EVENT_CODE);
        EventManager.RaisePhotonEvent(EventManager.GAME_HAND_DEALT_EVENT_CODE);
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

        // Outer loop goes 4 times to give each player 4 cards
        for (int i = 0; i < 4; i++)
        {
            // Inner loop goes through each player
            for (int j = 0; j < _playerCount; j++)
            {
                PhotonPlayer player = TurnOrder[j];
                int playerIndex = (j - _tablePlayerOffset + _playerCount) % _playerCount;

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

        yield return StartCoroutine(MoveAndRotateCard(cardVisualTransform, targetPosition, targetRotation));

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

    private IEnumerator Drop(int[] dropInfo)
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

        // Move card and rotate if necessary
        Vector2 targetPosition = _tableHand.anchoredPosition + _tablePanelOffset;
        Vector3 targetRotation = _tableHand.eulerAngles;

        cardTransform.SetParent(_tablePanel.transform);
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
        EventManager.RaisePhotonEvent(EventManager.GAME_END_TURN_EVENT_CODE);

        yield break;
    }

    private IEnumerator Stack(int[] stackInfo)
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

        string txt = "Table Card Indexes: ";
        CardInteraction[] tableCards = new CardInteraction[numTableCards];
        for (int i = 0; i < numTableCards; i++)
        {
            int cardIndex = tableCardIndexes[i];
            tableCards[i] = TableHand[cardIndex];

            txt += $"{cardIndex}, ";
        }
        Debug.Log(txt);

        Vector2 targetPosition = numTableCards > 0 ? 
            tableCards[0].GetComponent<RectTransform>().anchoredPosition : _tableHand.anchoredPosition;
        List<GameObject> visualCards = new List<GameObject>();

        // Duplicate each card and hide the original
        // Keeps the grid layout in place until the stack is made
        foreach (CardInteraction tableCard in tableCards)
        {
            Vector3 position = tableCard.GetComponent<RectTransform>().anchoredPosition;
            GameObject cardVisual = Instantiate(tableCard.gameObject, _tablePanel.transform);
            RectTransform cardTransform = cardVisual.GetComponent<RectTransform>();
            cardTransform.anchorMin = Vector2.one / 2;
            cardTransform.anchorMax = Vector2.one / 2;
            cardTransform.anchoredPosition = position;

            visualCards.Add(cardVisual);
            tableCard.Hide();

            yield return StartCoroutine(MoveCard(cardTransform, targetPosition));
        }

        // Once all visual cards have been moved, move the sender's card to the target position
        senderCardTransform.SetParent(_tablePanel.transform);
        yield return StartCoroutine(MoveCard(senderCardTransform, targetPosition));

        // Create the stack card at the target position
        GameObject stackVisual = Instantiate(_stackVisualPrefab, targetPosition, Quaternion.identity, _tablePanel.transform);
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
        int siblingIndex = numTableCards > 0 ? tableCards[0].transform.GetSiblingIndex() : TableHand.Count;
        stackVisual.transform.SetParent(_tableHand);
        stackVisual.transform.SetSiblingIndex(siblingIndex);
        TableHand.Insert(siblingIndex, stack);

        CheckTableSize();

        // End Turn
        EventManager.RaisePhotonEvent(EventManager.GAME_END_TURN_EVENT_CODE);

        yield break;
    }

    private IEnumerator PickUp(int[] pickupInfo)
    {
        // Sender Info
        int senderIndex = pickupInfo[0];
        Debug.Log($"Sender index in the turn order is: {senderIndex}");
        int senderTableIndex = (senderIndex - _tablePlayerOffset + _playerCount) % _playerCount;
        _lastPickedUpIndex = senderTableIndex;
        RectTransform senderHand = _activePlayerHands[senderTableIndex];
        SingleCardInteraction[] senderCards = senderHand.GetComponentsInChildren<SingleCardInteraction>();
        RectTransform senderPileTransform = _activePickupPiles[senderTableIndex];
        PickupPile senderPile = senderPileTransform.GetComponent<PickupPile>();

        // Sender's Hand Card Info
        int senderCardIndex = pickupInfo[1];
        Debug.Log($"Card index in sender's hand is: {senderCardIndex}");
        SingleCardInteraction card = senderCards[senderCardIndex];
        RectTransform cardTransform = card.GetComponent<RectTransform>();

        // Table Card(s) Info
        int[] tableCardIndexes = pickupInfo[2..];
        int numTableCards = tableCardIndexes.Length;

        CardInteraction[] tableCards = new CardInteraction[numTableCards];
        for (int i = 0; i < numTableCards; i++)
        {
            int cardIndex = tableCardIndexes[i];
            tableCards[i] = TableHand[cardIndex];
        }

        // Reveal the sender's card
        Image cardImage = card.GetComponent<Image>();
        string cardName = card.Card.Name;
        Sprite cardSprite = GameManager.GetCardSpriteByName(cardName);
        cardImage.sprite = cardSprite;

        // Add cards to the sender's pickup pile
        Vector2 targetPosition = senderPileTransform.anchoredPosition;
        Vector3 targetRotation = senderPileTransform.eulerAngles;
        List <GameObject> visualCards = new List<GameObject>();
        foreach (CardInteraction tableCard in tableCards)
        {
            Vector3 position = tableCard.GetComponent<RectTransform>().anchoredPosition + _tablePanelOffset;
            GameObject cardVisual = Instantiate(tableCard.gameObject, _tablePanel.transform.parent);
            RectTransform visualCardTransform = cardVisual.GetComponent<RectTransform>();
            visualCardTransform.anchorMin = Vector2.one / 2;
            visualCardTransform.anchorMax = Vector2.one / 2;
            visualCardTransform.anchoredPosition = position;

            visualCards.Add(cardVisual);
            tableCard.Hide();

            yield return StartCoroutine(MoveAndRotateCard(visualCardTransform, targetPosition, targetRotation));

            senderPile.AddCard(tableCard);

            // Clear stacks once they're picked up
            if (tableCard.InteractionType == InteractionType.Stack)
            {
                StackedCardsInteraction stack = (StackedCardsInteraction)tableCard;
                TurnManager.ClearStack(stack);
                Debug.Log("Attempting to clear stack...");
            }
        }

        cardTransform.SetParent(_tablePanel.transform.parent);
        yield return StartCoroutine(MoveAndRotateCard(cardTransform, targetPosition, targetRotation));
        senderPile.AddCard(card);


        /* *******
         * TEMP DEBUG INFO
         * ******* */

        string txt = string.Empty;
        foreach (CardInteraction tableCard in tableCards)
            txt += $"{tableCard.Value} ";

        Debug.Log($"Pick up successful.\n" +
            $"Pick up info:\n" +
            $"Sender card value: {card.Value}\n" +
            $"Values of Cards/Stacks picked up: {txt}\n" +
            $"New pile total: {senderPile.Count}\n" +
            $"New pile point count: {senderPile.PointCardTotal}\n" +
            $"New pile spade count: {senderPile.SpadeCardTotal}");

        /* *******
         * END DEBUG
         * ******* */

        // Delete all visual cards
        foreach (GameObject visualCard in visualCards)
            Destroy(visualCard);
        visualCards.Clear();

        // Delete all actual cards and the sender's card
        foreach (CardInteraction tableCard in tableCards)
            DeleteTableCard(tableCard);

        Destroy(card.gameObject);

        UpdatePileProperties();

        // TODO: [CARDS] Check for sweep points
        if (IsSweep())
            UpdateSweepScore(senderIndex);

        // End Turn
        EventManager.RaisePhotonEvent(EventManager.GAME_END_TURN_EVENT_CODE);

        yield break;
    }

    private IEnumerator ClearTable()
    {
        PickupPile pile = _activePickupPiles[_lastPickedUpIndex].GetComponent<PickupPile>();
        CardInteraction[] tableCards = TableHand.ToArray();

        foreach (CardInteraction card in tableCards)
        {
            Debug.Log($"Adding leftover card to pile: {((SingleCardInteraction)card).Card.Name}");
            pile.AddCard(card);
            // TODO: [CARDS] Move cards to the pickup pile before destorying them
            DeleteTableCard(card);
        }

        UpdatePileProperties();

        EventManager.RaisePhotonEvent(EventManager.GAME_END_SET_EVENT_CODE);

        yield break;
    }

    private IEnumerator MoveCard(RectTransform cardTransform, Vector2 targetPosition)
    {
        yield return StartCoroutine(MoveAndRotateCard(cardTransform, targetPosition, cardTransform.eulerAngles));
    }

    private IEnumerator MoveAndRotateCard(RectTransform cardTransform, Vector2 targetPosition, Vector3 targetRotation)
    {
        Vector2 position = cardTransform.anchoredPosition;
        Vector3 rotation = cardTransform.eulerAngles;
        float distance = (targetPosition - position).magnitude;
        float theta = (targetRotation - rotation).magnitude;
        float totalTime = 0.25f;
        int increments = 15;
        float maxDistanceDelta = distance / increments;
        float maxAngleDelta = theta / increments;

        Debug.Log($"Start: {position.x}, {position.y}\n" +
            $"Distance: {distance}\n" +
            $"Epsilon: {maxDistanceDelta / 2}");
        if (distance > 10)
        {
            while (distance > maxDistanceDelta / 2)
            {
                position = Vector2.MoveTowards(position, targetPosition, maxDistanceDelta);
                rotation = Vector3.MoveTowards(rotation, targetRotation, maxAngleDelta);
                cardTransform.anchoredPosition = position;
                cardTransform.eulerAngles = rotation;
                distance = (targetPosition - position).magnitude;
                yield return new WaitForSeconds(totalTime / increments);
            }
        }

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
        TableHandLayoutGroup layout = _tableHand.GetComponent<TableHandLayoutGroup>();

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

    private void UpdatePileProperties()
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        int[] PileTotals = new int[_playerCount];
        int[] PilePoints = new int[_playerCount];
        int[] PileSpades = new int[_playerCount];
        int[] PileTens = new int[_playerCount];
        int[] PileTwos = new int[_playerCount];

        int[] PileAceCount = new int[_playerCount];

        for (int i = 0; i < _playerCount; i++)
        {
            int playerIndex = (i - _tablePlayerOffset + _playerCount) % _playerCount;

            PickupPile pile = _activePickupPiles[playerIndex].GetComponent<PickupPile>();
            PileTotals[playerIndex] = pile.Count;
            PilePoints[playerIndex] = pile.PointCardTotal;
            PileSpades[playerIndex] = pile.SpadeCardTotal;
            PileTens[playerIndex] = pile.HasTenOfDiamonds ? 1 : 0;
            PileTwos[playerIndex] = pile.HasTwoOfSpades ? 1 : 0;

            PileAceCount[playerIndex] = pile.AceTotal;
        }

        PhotonNetwork.room.SetCustomProperties(new Hashtable {
            { GameManager.PILE_TOTALS_KEY, PileTotals },
            { GameManager.PILE_POINTS_KEY, PilePoints },
            { GameManager.PILE_SPADES_KEY, PileSpades },
            { GameManager.PILE_TENS_KEY, PileTens },
            { GameManager.PILE_TWOS_KEY, PileTwos } });

        PhotonNetwork.room.SetCustomProperties(new Hashtable { { GameManager.PILE_ACE_COUNT_KEY, PileAceCount } });
    }

    private bool IsSweep() => TableHand.Count == 0;

    private void UpdateSweepScore(int senderIndex)
    {
        Debug.Log("Sweep scored!");

        if (!PhotonNetwork.isMasterClient)
            return;

        int[] sweepScore = (int[])PhotonNetwork.room.CustomProperties[GameManager.SWEEP_SCORE_KEY];
        if (sweepScore == null)
            sweepScore = new int[2];

        int teamIndex = sweepScore[0];
        int totalSweeps = sweepScore[1];

        if (totalSweeps > 0)
            totalSweeps--;
        else
        {
            teamIndex = senderIndex;
            totalSweeps = 1;
        }

        sweepScore[0] = teamIndex;
        sweepScore[1] = totalSweeps;

        PhotonNetwork.room.SetCustomProperties(new Hashtable { { GameManager.SWEEP_SCORE_KEY, sweepScore} });
    }
}

public enum TableSize
{
    Default,
    Medium,
    Large,
    ExtraLarge
}
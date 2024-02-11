using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class GameManager : MonoBehaviour
{
    public const int MIN_PLAYERS = 2;
    public const int MAX_PLAYERS = 4;

    public const string TURN_ORDER_KEY = "Turn Order";
    public const string DECK_KEY = "Deck";
    public const string TABLE_HAND_KEY = "Table Hand";
    public const string PLAYER_HAND_KEY = "Player Hand";
    public const string CURRENT_ROUND_KEY = "Current Round";
    public const string MAX_ROUNDS_KEY = "Max Rounds";

    public const byte TURN_ORDER_SET_EVENT_CODE = 1;
    public const byte GAME_SET_STARTED = 3;
    public const byte GAME_SET_ADVANCED = 4;
    public const byte GAME_SET_ENDED = 5;

    public static RaiseEventOptions EventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };

    public static event Action<List<List<Card>>, List<Card>, int> HandDealt;
    public static event Action DealFinished;

    public static Dictionary<string, Sprite> CardSprites;
    public static string DeckColor;

    [field: SerializeField] public Deck Deck { get; private set; }
    [field: SerializeField] public DeckType DeckType { get; private set; }
    

    private int _dealerIndex;
    private int _round;
    private int _maxRounds;
    private int _playerCount;
    private Hashtable _roomProperties;
    private Hashtable[] _playerProperties = new Hashtable[4];

    #region Monobehaviour Callbacks

    private void Awake()
    {
        LoadCardSprites();
    }

    private void Start()
    {
        Deck.CreateDeck(DeckType.Star_Blue);
        _roomProperties = new Hashtable
        {
            { TURN_ORDER_KEY, null },
            { DECK_KEY, null },
            { TABLE_HAND_KEY, null },
            { PLAYER_HAND_KEY, null },
            { CURRENT_ROUND_KEY, null },
            { MAX_ROUNDS_KEY, null }
        };

        _dealerIndex = 0;
    }

    private void OnEnable()
    {
        PhotonNetwork.OnEventCall += OnTableSet;
    }

    private void OnDisable()
    {
        PhotonNetwork.OnEventCall -= OnTableSet;
    }

    #endregion

    #region PUN Event Callbacks

    private void OnTableSet(byte eventCode, object content, int senderID)
    {
        if(eventCode != TableVisual.TABLE_SET_EVENT_CODE)
            return;

        Debug.Log("Table Set");
        StartSet();
    }

    #endregion

    public void SetTurnOrder()
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        _playerCount = PhotonNetwork.room.PlayerCount;
        _dealerIndex %= _playerCount;

        if (_playerCount == 4)
        {
            // FIXME: Figure out turn setting for 4 players
            return;
        }

        PhotonPlayer[] TurnOrder = new PhotonPlayer[_playerCount];
        // Shift turn order by dealer index
        for (int i = 0; i < _playerCount; i++)
            TurnOrder[(i + _dealerIndex) % _playerCount] = PhotonNetwork.playerList[i];

        _roomProperties[TURN_ORDER_KEY] = TurnOrder;
        PhotonNetwork.room.SetCustomProperties(_roomProperties);

        PhotonNetwork.RaiseEvent(TURN_ORDER_SET_EVENT_CODE, eventContent: null, 
            sendReliable: true, EventOptions);
    }

    /// <summary>
    /// Begins a set. 
    /// Master client creates a deck, shuffles it, and gets the hands for the players and the table. 
    /// Values are stored in the room properties for other players to access. 
    /// An event is raised at the end so players know when the information is ready.
    /// </summary>
    public void StartSet()
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        PhotonPlayer[] TurnOrder = (PhotonPlayer[])PhotonNetwork.room.CustomProperties[TURN_ORDER_KEY];

        Deck.CreateDeck(DeckType);
        Deck.Shuffle();

        int index = 0;
        Card[,] playerHands = Deck.DealHands(_playerCount);
        foreach (PhotonPlayer player in TurnOrder)
            AddPlayerHandToProperties(player, playerHands, index++);

        AddTableHandToProperties();
        AddDeckToProperties();

        _roomProperties[CURRENT_ROUND_KEY] = 1;
        _roomProperties[MAX_ROUNDS_KEY] = 48 / _playerCount;

        PhotonNetwork.room.SetCustomProperties(_roomProperties);
        TableVisual.LogPlayerHands(TurnOrder);

        Debug.Log("Starting Game");

        PhotonNetwork.RaiseEvent(GAME_SET_STARTED, eventContent: null,
            sendReliable: true, EventOptions);
    }

    /// <summary>
    /// Advances the set to the next round. Ends the set if there is no next round.
    /// </summary>
    public void AdvanceSet()
    {
        if(_round == _maxRounds)
        {
            EndSet();
            return;
        }

        Card[,] playerHands = Deck.DealHands(_playerCount);
        _round++;

        int roundsLeft = _maxRounds - _round;
        // HandDealt?.Invoke(playerHands, null, roundsLeft);
    }

    /// <summary>
    /// Ends a set. Counts up the points for the set and checks if the game is over.
    /// </summary>
    public void EndSet()
    {
        _dealerIndex++;
        _dealerIndex %= _playerCount;
        _round = 0;
    }

    /// <summary>
    /// Stores the player's hand into the room properties using the player's name. 
    /// The hand is stored as an array of strings for serialization.
    /// </summary>
    /// <param name="player">The player whose hand we're sotring.</param>
    /// <param name="hands">The list of hands for this round.</param>
    /// <param name="orderIndex">The index of this player in the turn order.</param>
    private void AddPlayerHandToProperties(PhotonPlayer player, Card[,] hands, int orderIndex)
    {
        string[] hand = new string[4];
        for(int i = 0; i < 4; i++)
            hand[i] = hands[orderIndex, i].Name;

        string propertyKey = $"{PLAYER_HAND_KEY}: {player.NickName}";
        _roomProperties[propertyKey] = hand;
    }

    private void AddTableHandToProperties()
    {
        Card[] tableHand = Deck.DealTable();
        string[] tableHandNames = new string[tableHand.Length];
        for (int i = 0; i < 4; i++)
            tableHandNames[i] = tableHand[i].Name;

        _roomProperties[TABLE_HAND_KEY] = tableHandNames;
    }

    private void AddDeckToProperties()
    {
        Card[] deck = Deck.CardsArray;
        string[] deckNames = new string[deck.Length];
        for (int i = 0;  i < deck.Length; i++)
            deckNames[i] = deck[i].Name;

        _roomProperties[DECK_KEY] = deckNames;
    }

    /// <summary>
    /// Retrieves sprites for all the cards from the Textures folder in Resources using the DeckType. 
    /// Stores these sprites into a dictionary for quick retrieval.
    /// </summary>
    private void LoadCardSprites()
    {
        string deckName = DeckType.ToString().Split("_")[0];
        DeckColor = DeckType.ToString().Split("_")[1];
        Sprite[] sprites = Resources.LoadAll<Sprite>($"Textures/{deckName} Cards");
        CardSprites = new Dictionary<string, Sprite>();

        for(int i = 0; i < sprites.Length; i++)
        {
            string name = sprites[i].name;
            name = name.Replace($"{deckName}_", "");
            CardSprites.Add(name, sprites[i]);
        }
    }

    /// <summary>
    /// Fetches a card sprite from the dictionary.
    /// </summary>
    /// <param name="name">The name of the card to retrieve in the form of {suit}_{faceValue}.</param>
    /// <returns>The sprite of the card with the given name, null if not found.</returns>
    public static Sprite GetCardSpriteByName(string name)
    {
        if (CardSprites.ContainsKey(name))
            return CardSprites[name];
        else
            return null;
    }
}
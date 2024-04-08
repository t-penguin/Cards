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

    public static event Action<List<List<Card>>, List<Card>, int> HandDealt;
    public static event Action DealFinished;

    public static Dictionary<string, Sprite> CardSprites;
    public static string DeckColor;

    [field: SerializeField] public Deck Deck { get; private set; }
    [field: SerializeField] public DeckType DeckType { get; private set; }

    private int _dealerIndex;
    private int _playerCount;

    #region Monobehaviour Callbacks

    private void Awake()
    {
        LoadCardSprites();
    }

    private void Start()
    {
        Deck.CreateDeck(DeckType.Star_Blue);

        _dealerIndex = 0;
    }

    private void OnEnable()
    {
        PhotonNetwork.OnEventCall += OnTableSet;
        PhotonNetwork.OnEventCall += OnTurnEnded;
    }

    private void OnDisable()
    {
        PhotonNetwork.OnEventCall -= OnTableSet;
        PhotonNetwork.OnEventCall -= OnTurnEnded;
    }

    #endregion

    #region PUN Event Callbacks

    private void OnTableSet(byte eventCode, object content, int senderID)
    {
        if (eventCode != EventManager.TABLE_SET_EVENT_CODE)
            return;

        Debug.Log("Table Set");
        StartSet();
    }

    private void OnTurnEnded(byte eventCode, object content, int senderID)
    {
        if (eventCode != EventManager.TURN_ENDED_EVENT_CODE)
            return;

        Debug.Log("Turn ended");
        AdvanceTurn();
    }

    #endregion

    public void SetTurnOrder()
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        _playerCount = PhotonNetwork.room.PlayerCount;
        if (_playerCount < MIN_PLAYERS || _playerCount > MAX_PLAYERS)
        {
            Debug.Log($"Invalid number of players ({_playerCount}), cannot start game");
            return;
        }

        _dealerIndex %= _playerCount;

        if (_playerCount == 4)
        {
            // TODO: [CARDS] Figure out turn setting for 4 players
            return;
        }

        PhotonPlayer[] TurnOrder = new PhotonPlayer[_playerCount];
        // Shift turn order by dealer index
        for (int i = 0; i < _playerCount; i++)
            TurnOrder[(i + _dealerIndex) % _playerCount] = PhotonNetwork.playerList[i];

        PhotonNetwork.room.SetCustomProperties(new Hashtable { { TURN_ORDER_KEY, TurnOrder} });

        CasinoDebugInfo.LogTurnOrder();

        EventManager.RaisePhotonEvent(EventManager.TURN_ORDER_SET_EVENT_CODE);
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

        Deck.CreateDeck(DeckType);
        Deck.Shuffle();

        DealAndSetPlayerProperties();
        AddTableHandToProperties();
        AddDeckToProperties();

        int maxRounds = 48 / _playerCount;
        Hashtable roundProperties = new Hashtable { { CURRENT_ROUND_KEY, 1 }, { MAX_ROUNDS_KEY, maxRounds } };
        PhotonNetwork.room.SetCustomProperties(roundProperties);
        PhotonNetwork.room.SetTurn(0);
        CasinoDebugInfo.LogPlayerHands();

        Debug.Log("Starting Game");

        EventManager.RaisePhotonEvent(EventManager.GAME_SET_STARTED);
    }

    /// <summary>
    /// Advances the set to the next round. Ends the set if there is no next round.
    /// </summary>
    public void AdvanceSet()
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        int round = (int)PhotonNetwork.room.CustomProperties[CURRENT_ROUND_KEY];
        int maxRounds = (int)PhotonNetwork.room.CustomProperties[MAX_ROUNDS_KEY];

        if(round == maxRounds)
        {
            EndSet();
            return;
        }

        DealAndSetPlayerProperties();
        round++;

        PhotonNetwork.room.SetCustomProperties(new Hashtable { { CURRENT_ROUND_KEY, round} });
        PhotonNetwork.room.SetTurn(0);

        Debug.Log($"Advancing set to Round {round}");
        EventManager.RaisePhotonEvent(EventManager.GAME_SET_STARTED);
    }

    /// <summary>
    /// Ends a set. Counts up the points for the set and checks if the game is over.
    /// </summary>
    public void EndSet()
    {
        _dealerIndex++;
        _dealerIndex %= _playerCount;
    }

    private void AdvanceTurn()
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        int currentTurn = PhotonNetwork.room.GetTurn();
        currentTurn++;
        int turnsPerRound = _playerCount * 4;

        if (currentTurn < turnsPerRound)
        {
            Debug.Log($"Advancing to turn {currentTurn}");

            PhotonNetwork.room.SetTurn(currentTurn);
            EventManager.RaisePhotonEvent(EventManager.TURN_STARTED_EVENT_CODE);
            return;
        }

        if (currentTurn == turnsPerRound)
        {
            AdvanceSet();
            return;
        }
    }

    private void DealAndSetPlayerProperties()
    {
        PhotonPlayer[] TurnOrder = (PhotonPlayer[])PhotonNetwork.room.CustomProperties[TURN_ORDER_KEY];

        int index = 0;
        Card[,] playerHands = Deck.DealHands(_playerCount);
        foreach (PhotonPlayer player in TurnOrder)
            AddPlayerHandToProperties(player, playerHands, index++);
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
        PhotonNetwork.room.SetCustomProperties(new Hashtable { { propertyKey, hand } });
    }

    private void AddTableHandToProperties()
    {
        Card[] tableHand = Deck.DealTable();
        string[] tableHandNames = new string[tableHand.Length];
        for (int i = 0; i < 4; i++)
            tableHandNames[i] = tableHand[i].Name;

        PhotonNetwork.room.SetCustomProperties(new Hashtable { { TABLE_HAND_KEY, tableHandNames } });
    }

    private void AddDeckToProperties()
    {
        Card[] deck = Deck.CardsArray;
        string[] deckNames = new string[deck.Length];
        for (int i = 0;  i < deck.Length; i++)
            deckNames[i] = deck[i].Name;

        PhotonNetwork.room.SetCustomProperties(new Hashtable { { DECK_KEY, deckNames } });
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
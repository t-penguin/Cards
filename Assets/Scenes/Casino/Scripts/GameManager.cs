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
    public const string CURRENT_SET_KEY = "Current Set";
    public const string CURRENT_ROUND_KEY = "Current Round";
    public const string MAX_ROUNDS_KEY = "Max Rounds";
    public const string PILE_TOTALS_KEY = "Pile Total Counts";
    public const string PILE_POINTS_KEY = "Pile Point Counts";
    public const string PILE_SPADES_KEY = "Pile Spades Counts";
    public const string PILE_TENS_KEY = "Pile Ten Of Diamonds Flags";
    public const string PILE_TWOS_KEY = "Pile Two Of Spades Flags";
    public const string TEAM_SCORES_KEY = "Team Scores";

    public const string PILE_ACE_COUNT_KEY = "Pile Ace Count";

    public static Dictionary<string, Sprite> CardSprites;
    public static Dictionary<string, Sprite> CardIcons;
    public static string DeckColor;

    [field: SerializeField] public Deck Deck { get; private set; }
    [field: SerializeField] public DeckType DeckType { get; private set; }
    
    private int _playerCount;
    private int _playerTableIndex;
    private int _dealerIndex;
    private int _currentSet;
    private bool _startNewGame;

    #region Monobehaviour Callbacks

    private void Awake()
    {
        CardSprites = new Dictionary<string, Sprite>();
        CardIcons = new Dictionary<string, Sprite>();

        string[] deckType = DeckType.ToString().Split("_");
        string deckName = deckType[0];
        string path = $"Textures/{deckName} Cards";
        LoadSprites(CardSprites, path, deckName);
        path = $"Textures/{deckName} Card Icons";
        LoadSprites(CardIcons, path, deckName);

        DeckColor = deckType[1];
    }

    private void Start()
    {
        Deck.CreateDeck(DeckType.Star_Blue);

        _dealerIndex = 0;
    }

    private void OnEnable()
    {
        PhotonNetwork.OnEventCall += OnGameStarted;
        PhotonNetwork.OnEventCall += OnTurnEnded;
        PhotonNetwork.OnEventCall += OnEndSet;
    }

    private void OnDisable()
    {
        PhotonNetwork.OnEventCall -= OnGameStarted;
        PhotonNetwork.OnEventCall -= OnTurnEnded;
        PhotonNetwork.OnEventCall -= OnEndSet;
    }

    #endregion

    #region PUN Event Callbacks

    private void OnGameStarted(byte eventCode, object content, int senderID)
    {
        if (eventCode != EventManager.START_GAME_EVENT_CODE)
            return;

        _currentSet++;
        StartSet();
    }

    private void OnTurnEnded(byte eventCode, object content, int senderID)
    {
        if (eventCode != EventManager.GAME_END_TURN_EVENT_CODE)
            return;

        Debug.Log("Turn ended");
        AdvanceTurn();
    }

    private void OnEndSet(byte eventCode, object content, int senderID)
    {
        if (eventCode != EventManager.GAME_END_SET_EVENT_CODE)
            return;

        Debug.Log("Ending Set");
        EndSet();
    }

    #endregion

    public void StartNewGame()
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        _playerCount = PhotonNetwork.room.PlayerCount;
        if (_playerCount < MIN_PLAYERS || _playerCount > MAX_PLAYERS)
        {
            Debug.Log($"Invalid number of players ({_playerCount}), cannot start game");
            return;
        }

        _startNewGame = true;
        SetTurnOrder();
    }

    private void SetTurnOrder()
    {
        if (!PhotonNetwork.isMasterClient)
            return;

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
        _playerTableIndex = Array.IndexOf(TurnOrder, PhotonNetwork.player);

        CasinoDebugInfo.LogTurnOrder();

        if (_currentSet == 0)
            EventManager.RaisePhotonEvent(EventManager.SET_TABLE_EVENT_CODE);
        else
            EventManager.RaisePhotonEvent(EventManager.START_GAME_EVENT_CODE);
    }

    /// <summary>
    /// Begins a set. 
    /// Master client creates a deck, shuffles it, and gets the hands for the players and the table. 
    /// Values are stored in the room properties for other players to access. 
    /// An event is raised at the end so players know when the information is ready.
    /// </summary>
    private void StartSet()
    {
        Deck.CreateDeck(DeckType);

        if (!PhotonNetwork.isMasterClient)
            return;

        Deck.Shuffle();

        DealAndSetPlayerProperties();
        AddTableHandToProperties();
        AddDeckToProperties();
        if (_startNewGame)
        {
            PhotonNetwork.room.SetCustomProperties(new Hashtable { { TEAM_SCORES_KEY, new int[_playerCount] } });
            _startNewGame = false;
        }

        PhotonNetwork.room.SetCustomProperties(new Hashtable {
            { CURRENT_SET_KEY, _currentSet },
            { CURRENT_ROUND_KEY, 1 }, 
            { MAX_ROUNDS_KEY, 48 / (_playerCount * 4) } });
        PhotonNetwork.room.SetTurn(0);
        CasinoDebugInfo.LogPlayerHands();

        Debug.Log("Starting Game");

        EventManager.RaisePhotonEvent(EventManager.GAME_DEAL_HAND_EVENT_CODE);
    }

    /// <summary>
    /// Advances the set to the next round. Ends the set if there is no next round.
    /// </summary>
    private void AdvanceSet()
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        int round = (int)PhotonNetwork.room.CustomProperties[CURRENT_ROUND_KEY];
        int maxRounds = (int)PhotonNetwork.room.CustomProperties[MAX_ROUNDS_KEY];

        round++;
        Debug.Log($"Round {round}/{maxRounds}");

        if(round > maxRounds)
        {
            EventManager.RaisePhotonEvent(EventManager.TABLE_CLEAR_EVENT_CODE);
            return;
        }

        DealAndSetPlayerProperties();

        PhotonNetwork.room.SetCustomProperties(new Hashtable { { CURRENT_ROUND_KEY, round} });
        PhotonNetwork.room.SetTurn(0);

        Debug.Log($"Advancing set to Round {round}");
        EventManager.RaisePhotonEvent(EventManager.GAME_SET_ADVANCED_EVENT_CODE);
        EventManager.RaisePhotonEvent(EventManager.GAME_DEAL_HAND_EVENT_CODE);
    }

    /// <summary>
    /// Ends a set. Counts up the points for the set and checks if the game is over.
    /// </summary>
    private void EndSet()
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        UpdateScores();
        int winnerIndex;
        bool gameEnded = CheckForWin(out winnerIndex);

        // DEBUG
        string txt = "Score Update:\n";
        PhotonPlayer[] TurnOrder = (PhotonPlayer[])PhotonNetwork.room.CustomProperties[TURN_ORDER_KEY];
        int[] TeamScores = (int[])PhotonNetwork.room.CustomProperties[TEAM_SCORES_KEY];

        for (int i = 0; i < _playerCount; i++)
            txt += $"{TurnOrder[i].NickName}: {TeamScores[i]}\n";

        txt += $"Winner? {gameEnded} | Index: {winnerIndex}";
        Debug.Log(txt);
        // END DEBUG

        if (!gameEnded)
        {
            Debug.Log("No winner. Advancing game to next set...");

            _dealerIndex++;

            // TODO: [CARDS] Add a way to show that the set is being restarted
            EventManager.RaisePhotonEvent(EventManager.GAME_SET_ENDED_EVENT_CODE);
            SetTurnOrder();
            return;
        }
    }

    /// <summary>
    /// Updates each player's score based on their current scores and what they have in their
    /// pickup piles. There are special conditions when a player or team's current points are at
    /// certain values. To advance to 21 at: 17, they need to have the most cards and most spades;
    /// 18, they need to have the most cards; 19, they need the ten of diamonds; 20, they need the
    /// two of spades. If they fail to achieve these conditions, then they can only advance their
    /// score if they don't reach or surpass 21.
    /// </summary>
    private void UpdateScores()
    {
        int[] currentPoints = (int[])PhotonNetwork.room.CustomProperties[TEAM_SCORES_KEY];

        int[] pickupTotals = (int[])PhotonNetwork.room.CustomProperties[PILE_TOTALS_KEY];
        int[] pickupPoints = (int[])PhotonNetwork.room.CustomProperties[PILE_POINTS_KEY];
        int[] pickupSpades = (int[])PhotonNetwork.room.CustomProperties[PILE_SPADES_KEY];
        int[] pickupTens = (int[])PhotonNetwork.room.CustomProperties[PILE_TENS_KEY];
        int[] pickupTwos = (int[])PhotonNetwork.room.CustomProperties[PILE_TWOS_KEY];

        int[] pickupAces = (int[])PhotonNetwork.room.CustomProperties[PILE_ACE_COUNT_KEY];

        int mostCardsIndex = GetMaxIndex(pickupTotals);
        int mostSpadesIndex = GetMaxIndex(pickupSpades);

        for (int i = 0; i < _playerCount; i++)
        {
            int index = (i - _playerTableIndex + _playerCount) % _playerCount;
            int points = currentPoints[index];
            int mostCardsPoints = index == mostCardsIndex ? 3 : 0;
            int mostSpadesPoints = index == mostSpadesIndex ? 1 : 0;
            int totalPoints = points + mostCardsPoints + mostSpadesPoints + pickupPoints[index];
            Debug.Log($"Points gained:\n" +
                $"Ace Count: {pickupAces[index]}\n" +
                $"10 of Diamonds: {pickupTens[index] == 1}\n" +
                $"2 of Spades: {pickupTwos[index] == 1}\n" +
                $"Cards: {mostCardsPoints}\n" +
                $"Spades: {mostSpadesPoints}");
            switch (points)
            {
                default:
                    points = totalPoints;
                    break;
                case 17: // Needs to have the most cards and the most spades
                    if ((mostCardsIndex != index || mostSpadesIndex != index) && totalPoints >= 21)
                        break;

                    goto default;
                case 18: // Needs to have the most cards
                    if (mostCardsIndex != index && totalPoints >= 21)
                        break;

                    goto default;
                case 19: // Needs to have the 10 of diamonds
                    if (pickupTens[index] != 1 && totalPoints >= 21)
                        break;

                    goto default;
                case 20: // Needs to have the 2 of spades
                    if (pickupTwos[index] != 1 && totalPoints >= 21)
                        break;

                    goto default;
            }
            currentPoints[index] = points;
        }

        PhotonNetwork.room.SetCustomProperties(new Hashtable { { TEAM_SCORES_KEY, currentPoints } });
    }

    /// <summary>
    /// Checks if any team or player has reached or surpassed 21. Returns true if one team or
    /// player has the highest score at or above 21. Returns false if no team or player has reached
    /// or surpassed 21 or if there is a tie.
    /// </summary>
    /// <param name="winnerIndex">Index of the winner in the turn order. -1 if no winner.</param>
    private bool CheckForWin(out int winnerIndex)
    {
        winnerIndex = -1;
        int[] scores = (int[])PhotonNetwork.room.CustomProperties[TEAM_SCORES_KEY];
        foreach (int score in scores)
        {
            if (score >= 21)
            {
                winnerIndex = GetMaxIndex(scores);
                break;
            }
        }

        if (winnerIndex == -1)
            return false;

        PhotonPlayer[] TurnOrder = (PhotonPlayer[])PhotonNetwork.room.CustomProperties[TURN_ORDER_KEY];
        Debug.Log($"Winner: {TurnOrder[winnerIndex].NickName}");

        return true;
    }

    /// <summary>
    /// Returns the index of the maximum value in this array. If there is are multiple
    /// instances of the max value then this returns -1 instead.
    /// </summary>
    /// <param name="array">The array to check through</param>
    private int GetMaxIndex(int[] array)
    {
        int max = 0;
        int index = -1;

        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] > max)
            {
                max = array[i];
                index = i;
            }
            else if (array[i] == max)
                index = -1;
        }

        return index;
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
            EventManager.RaisePhotonEvent(EventManager.GAME_START_TURN_EVENT_CODE);
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
        Card[] deck = Deck.Cards;
        string[] deckNames = new string[deck.Length];
        for (int i = 0;  i < deck.Length; i++)
            deckNames[i] = deck[i].Name;

        PhotonNetwork.room.SetCustomProperties(new Hashtable { { DECK_KEY, deckNames } });
    }

    /// <summary>
    /// Retrieves sprites and icons for all the cards from the Textures folder in Resources
    /// using the DeckType. Stores these sprites into a dictionary for quick retrieval.
    /// </summary>
    private void LoadSprites(Dictionary<string, Sprite> dictionary, string path, string deckName)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(path);

        foreach (Sprite sprite in sprites)
            dictionary.Add(sprite.name.Replace($"{deckName}_", ""), sprite);
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

    /// <summary>
    /// Fetches a card icon from the dictionary.
    /// </summary>
    /// <param name="name">The name of the card icon to retrieve in the form of {suit}_{faceValue}.</param>
    /// <returns>The sprite of the card icon with the given name, null if not found.</returns>
    public static Sprite GetCardIconByName(string name)
    {
        if (CardIcons.ContainsKey(name))
            return CardIcons[name];
        else
            return null;
    }
}
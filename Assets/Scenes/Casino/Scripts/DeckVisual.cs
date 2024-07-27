using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Deck))]
public class DeckVisual : MonoBehaviour
{
    [SerializeField] GameManager _gameManager;
    [SerializeField] List<Image> _deckBackVisual;
    
    private int _numPlayers;
    private int _maxRounds;
    private int _roundsRemaining;

    #region Monobehaviour Callbacks

    // Subscribe to events
    private void OnEnable()
    {
        Deck.DeckCreated += OnDeckCreated;
        PhotonNetwork.OnEventCall += OnHandDealt;
    }

    // Unsubscribe to events
    private void OnDisable()
    {
        Deck.DeckCreated -= OnDeckCreated;
        PhotonNetwork.OnEventCall -= OnHandDealt;
    }

    // Set up and validate game objects for visuals
    void Awake()
    {
        if(_deckBackVisual.Count == 0)
        {
            Debug.LogWarning("Deck Visual not set up! Setting it up now...");
            GetComponentsInChildren(true, _deckBackVisual);
        }

        if(_deckBackVisual.Count != 6)
            Debug.LogWarning($"Invalid number of visuals in deck! Expected: 6, Actual: {_deckBackVisual.Count}");

        if(_gameManager == null)
        {
            Debug.LogWarning("Game Manager not set! Setting number of players to 1...");
            _numPlayers = 1;
        }
    }

    #endregion

    #region Event Callbacks

    // Modifies the deck visual when a deck is created
    private void OnDeckCreated(DeckType type)
    {
        // Sets the sprite for the back of the cards of the deck
        foreach(Image card in _deckBackVisual)
            card.sprite = GameManager.GetCardSpriteByName($"Back_{GameManager.DeckColor}");

        _numPlayers = ((PhotonPlayer[])PhotonNetwork.room.CustomProperties[GameManager.TURN_ORDER_KEY]).Length;
        _maxRounds = _numPlayers == 1 ? 6 : 52 / (_numPlayers * 4);
        _roundsRemaining = _maxRounds;
        ResetDeck();
    }

    private void OnHandDealt(byte eventCode, object content, int senderId)
    {
        if (eventCode != EventManager.GAME_HAND_DEALT_EVENT_CODE)
            return;

        _roundsRemaining -= _roundsRemaining > 0 ? 1 : 0;
        UpdateDeck(_roundsRemaining);
    }

    #endregion

    // Modifies the deck visual depending on the number of rounds remaining
    private void ModifyDeckVisual(int roundsLeft)
    {
        for (int i = 0; i < 6; i++)
            _deckBackVisual[i].gameObject.SetActive(i < roundsLeft);
    }

    public void ResetDeck()
    {
        _roundsRemaining = _maxRounds;
        ModifyDeckVisual(_maxRounds);
    }

    public void UpdateDeck(int remainingRounds) => ModifyDeckVisual(remainingRounds);
}
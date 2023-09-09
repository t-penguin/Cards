using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Deck))]
public class DeckVisual : MonoBehaviour
{
    [SerializeField] GameManager _gameManager;
    [SerializeField] List<Image> _deckBackVisual;
    
    private int numPlayers;

    #region Monobehaviour Callbacks

    // Subscribe to events
    private void OnEnable()
    {
        Deck.DeckCreated += OnDeckCreated;
        GameManager.HandDealt += OnHandDealt;
    }

    // Unsubscribe to events
    private void OnDisable()
    {
        Deck.DeckCreated -= OnDeckCreated;
        GameManager.HandDealt -= OnHandDealt;
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
            numPlayers = 1;
        }

        numPlayers = _gameManager.Players.Count;
    }

    #endregion

    #region Event Callbacks

    // Modifies the deck visual when a deck is created
    void OnDeckCreated(DeckType type)
    {
        // Sets the sprite for the back of the cards of the deck
        string backName = $"Back_{type.ToString().Split("_")[1]}";
        foreach(Image card in _deckBackVisual)
            card.sprite = _gameManager.GetCardSpriteByName(backName);

        // Sets the visual size of the deck based on the number of players
        Debug.Log($"numPlayers: {numPlayers}");
        int handsRemaining = numPlayers == 1 ? 6 : 52 / (numPlayers * 4);
        ModifyDeckVisual(handsRemaining);
    }

    // Modifies the deck visual when a hand is dealt
    void OnHandDealt(int deckOffset)
    {
        int handsRemaining = numPlayers == 1 ? 6 : (52 - deckOffset) / (numPlayers * 4);
        Debug.Log($"Hand dealt. Remaining: {handsRemaining}\nCards Left: {52 - deckOffset} / Cards per Hand: {numPlayers * 4}");
        ModifyDeckVisual(handsRemaining);
    }

    #endregion

    // Modifies the deck visual depending on the number of hands remaining
    private void ModifyDeckVisual(int handsRemaining)
    {
        for (int i = 0; i < 6; i++)
            _deckBackVisual[i].gameObject.SetActive(i < handsRemaining);
    }
}
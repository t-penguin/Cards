using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public const int MIN_PLAYERS = 2;
    public const int MAX_PLAYERS = 4;

    public static event Action<int> HandDealt;

    [field: SerializeField] public List<Player> Players { get; private set; }
    [field: SerializeField] public Deck Deck { get; private set; }

    private int dealerIndex;
    private int deckIndex;

    private void Start()
    {
        Deck.CreateDeck(DeckType.Star_Red);
    }

    public void Deal()
    {
        Deck.Deal(Players, dealerIndex + 1, deckIndex);
        dealerIndex++;
        deckIndex += Players.Count * 4;

        // Deal 4 to table
        if(deckIndex == Players.Count * 4)
        {
            deckIndex += 4;
        }
        HandDealt?.Invoke(deckIndex);
    }
}
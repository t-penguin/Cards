using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Deck : MonoBehaviour
{
    public static event Action<DeckType> DeckCreated;

    
    public Card[] Cards { get { return _cards.ToArray(); } }
    [SerializeField] private List<Card> _cards;

    [SerializeField] GameManager _gameManager;

    // Creates an array of cards (the deck) using the suit and face value enums
    public void CreateDeck(DeckType type)
    {
        _cards = new List<Card>();
        DeckCreated?.Invoke(type);

        int index = 0;
        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            foreach (FaceValue fValue in Enum.GetValues(typeof(FaceValue)))
            {
                _cards.Add(new Card(suit, fValue));
                index++;
            }
        }

        Debug.Log($"Created a new deck of size {_cards.Count}.");
    }

    // Fisher-Yates shuffle
    public void Shuffle()
    {
        if(_cards == null)
        {
            Debug.LogError("Attempted to shuffle but the deck has not been created!");
            return;
        }

        for(int i = 0; i < 51; i++)
        {
            int j = UnityEngine.Random.Range(i, 52);
            Card tempCard = _cards[i];
            _cards[i] = _cards[j];
            _cards[j] = tempCard;
        }

        Debug.Log("Deck shuffled!");
    }

    public Card[,] DealHands(int numPlayers)
    {
        // Error handling

        // Invalid number of Players
        if(numPlayers < GameManager.MIN_PLAYERS || numPlayers > GameManager.MAX_PLAYERS)
        {
            Debug.LogError($"Attempted to deal but the number of players ({numPlayers}) is invalid!");
            return null;
        }

        // Deck hasn't been created
        if(_cards == null)
        {
            Debug.LogWarning("Attempted to deal but the deck has not been created yet.");
            Debug.Log("Creating a new deck, shuffling it, then dealing...");
            CreateDeck(_gameManager.DeckType);
            Shuffle();
        }

        // No cards left
        int cardsLeft = _cards.Count;
        if (cardsLeft == 0)
        {
            Debug.LogError("Attempted to deal but there were no cards left. Round should end.");
            return null;
        }

        // Not enough cards left
        int cardsToGive = numPlayers * 4;
        if(cardsToGive > cardsLeft)
        {
            Debug.LogError($"Attempted to deal but there weren't enough cards ({cardsLeft} remain)." +
                $"Something went wrong somewhere...");
            return null;
        }

        Card[,] hands = new Card[numPlayers, 4];

        /* Deal four cards to each hand
         * i represents the card index in the player's hand
         * j represents the player hand's index in the list of hands */
        for(int i = 0; i < 4; i++)
        {
            for(int j = 0; j < numPlayers; j++)
            {
                hands[j,i] = _cards[0];
                _cards.RemoveAt(0);
            }
        }

        return hands;
    }

    public Card[] DealTable()
    {
        // Error Handling

        // Deck hasn't been created
        if (_cards == null)
        {
            Debug.LogWarning("Attempted to deal but the deck has not been created yet.");
            Debug.Log("Creating a new deck, shuffling it, then dealing...");
            CreateDeck(_gameManager.DeckType);
            Shuffle();
        }

        // No cards left
        int cardsLeft = _cards.Count;
        if (cardsLeft == 0)
        {
            Debug.LogError("Attempted to deal but there were no cards left. Round should end.");
            return null;
        }

        // Not enough cards left
        if (cardsLeft < 4)
        {
            Debug.LogError($"Attempted to deal but there weren't enough cards ({cardsLeft} remain)." +
                $"Something went wrong somewhere...");
            return null;
        }


        // Deal to table hand list
        Card[] tableHand = new Card[4];

        for(int i = 0; i < 4; i++)
        {
            tableHand[i] = _cards[0];
            _cards.RemoveAt(0);
        }

        return tableHand;
    }
}

public enum DeckType
{
    Star_Red,
    Star_Blue
}
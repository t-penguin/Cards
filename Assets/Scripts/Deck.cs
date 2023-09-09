using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Deck : MonoBehaviour
{
    public static event Action<DeckType> DeckCreated;

    [field: SerializeField] public Card[] Cards { get; private set; }

    // Creates an array of cards (the deck) using the suit and face value enums
    public void CreateDeck(DeckType type)
    {
        Cards = new Card[52];
        DeckCreated?.Invoke(type);

        int index = 0;
        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            foreach (FaceValue fValue in Enum.GetValues(typeof(FaceValue)))
            {
                Cards[index] = new Card(suit, fValue);
                index++;
            }
        }
    }

    // Fisher-Yates shuffle
    public void Shuffle()
    {
        if(Cards == null)
        {
            Debug.LogError("Attempted to shuffle but the deck has not been created!");
            return;
        }

        for(int i = 0; i < 51; i++)
        {
            int j = UnityEngine.Random.Range(i, 52);
            Card tempCard = Cards[i];
            Cards[i] = Cards[j];
            Cards[j] = tempCard;
        }

        Debug.Log("Deck shuffled!");
    }

    /// <summary>
    /// Deals cards to a set of players
    /// </summary>
    /// <param name="players">The list of players currently in the game.</param>
    /// <param name="playerOffset">The offset used to determine who gets dealt to first.</param>
    /// <param name="deckOffset">The offset used to determine where in the deck to deal from.</param>
    public void Deal(List<Player> players, int playerOffset, int deckOffset)
    {
        if(Cards == null)
        {
            Debug.LogError("Attempted to deal but the deck has not been created!");
            return;
        }

        int numPlayers = players.Count;
        if (numPlayers < 2 || numPlayers > 4)
        {
            Debug.LogError($"Attempted to deal but the number of players ({numPlayers}) is invalid!");
            return;
        }

        int cardsToGive = numPlayers * 4;
        int remainingCards = 52 - deckOffset;
        if(cardsToGive > remainingCards || remainingCards % cardsToGive != 0)
        {
            Debug.LogError($"Attempted to deal but the number of remaining cards ({remainingCards}) is invalid!");
            return;
        }

        for(int i = 0; i < cardsToGive; i++)
        {
            int playerIndex = (playerOffset + i) % numPlayers;
            players[playerIndex].ReceiveCard(Cards[deckOffset + i]);
        }
    }
}

public enum DeckType
{
    Star_Red,
    Star_Blue
}
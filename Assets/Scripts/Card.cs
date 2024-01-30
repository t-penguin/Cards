using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Card
{
    [field: SerializeField] public Suit Suit { get; private set; }
    [field: SerializeField] public FaceValue FaceValue { get; private set; }
    [field: SerializeField] public int NumberValue { get; private set; }

    public Card(Suit suit, FaceValue fValue)
    {
        Suit = suit;
        FaceValue = fValue;
        NumberValue = (int)FaceValue;
    }

    public override string ToString()
    {
        return $"{FaceValue} of {Suit}";
    }
}

public enum Suit
{
    Hearts,
    Diamonds,
    Spades,
    Clubs
}

public enum FaceValue
{
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13,
    Ace = 14
}
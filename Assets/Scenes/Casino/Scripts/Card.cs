using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Card
{
    #region Static Properties

    public static Dictionary<string, Suit> SuitDictionary = 
        new()
        {
            { "Hearts", Suit.Hearts },
            { "Diamonds", Suit.Diamonds },
            { "Spades", Suit.Spades },
            { "Clubs", Suit.Clubs }
        };

    public static Dictionary<string, FaceValue> FaceValueDictionary =
        new()
        {
            { "Two", FaceValue.Two },
            { "Three", FaceValue.Three },
            { "Four", FaceValue.Four },
            { "Five", FaceValue.Five },
            { "Six", FaceValue.Six },
            { "Seven", FaceValue.Seven },
            { "Eight", FaceValue.Eight },
            { "Nine", FaceValue.Nine },
            { "Ten", FaceValue.Ten },
            { "Jack", FaceValue.Jack },
            { "Queen", FaceValue.Queen },
            { "King", FaceValue.King },
            { "Ace", FaceValue.Ace}
        };

    #endregion

    [field: SerializeField] public Suit Suit { get; private set; }
    [field: SerializeField] public FaceValue FaceValue { get; private set; }
    public int NumberValue { get { return (int)FaceValue; } }
    public string Name { get { return $"{Suit}_{FaceValue}"; } }

    public Card(Suit suit, FaceValue fValue)
    {
        Suit = suit;
        FaceValue = fValue;
    }

    public Card(string name)
    {
        string suitName = name.Split('_')[0];
        string faceValue = name.Split("_")[1];

        Suit = SuitDictionary[suitName];
        FaceValue = FaceValueDictionary[faceValue];
    }

    public int GetPointValue()
    {
        if (Suit == Suit.Diamonds && FaceValue == FaceValue.Ten) return 2;
        if (Suit == Suit.Spades && FaceValue == FaceValue.Two) return 1;
        if (FaceValue == FaceValue.Ace) return 1;

        return 0;
    }

    public bool IsSpade() => Suit == Suit.Spades;

    public override string ToString()
    {
        return $"{FaceValue} of {Suit}";
    }
}

[System.Serializable]
public enum Suit
{
    Hearts,
    Diamonds,
    Spades,
    Clubs
}

[System.Serializable]
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
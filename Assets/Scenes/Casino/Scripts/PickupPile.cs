using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public class PickupPile : MonoBehaviour
{
    public static event Action<int> AddedCard;

    [field: SerializeField] public int PileID { get; private set; }
    [field: SerializeField] public int PointCardTotal { get; private set; }
    [field: SerializeField] public int SpadeCardTotal { get; private set; }
    [field: SerializeField] public bool HasTenOfDiamonds { get; private set; }
    [field: SerializeField] public bool HasTwoOfSpades { get; private set; }
    public int Count { get { return _cards.Count; } }

    public int AceTotal { get; private set; }

    public ReadOnlyCollection<Card> Cards { get { return _cards.AsReadOnly(); } }
    private List<Card> _cards;

    private void Awake()
    {
        _cards = new List<Card>();
    }

    public void AddCard(CardInteraction cardInteraction)
    {
        // If attempting to add a stack to this pile, add each card individually
        if (cardInteraction.InteractionType == InteractionType.Stack)
        {
            StackedCardsInteraction stack = (StackedCardsInteraction)cardInteraction;
            foreach (SingleCardInteraction singleCard in stack.Cards)
                AddCard(singleCard);

            return;
        }

        Card card = ((SingleCardInteraction)cardInteraction).Card;
        PointCardTotal += card.GetPointValue();
        SpadeCardTotal += card.IsSpade() ? 1 : 0;

        if (card.FaceValue == FaceValue.Ten && card.Suit == Suit.Diamonds)
            HasTenOfDiamonds = true;
        if (card.FaceValue == FaceValue.Two && card.IsSpade())
            HasTwoOfSpades = true;
        if (card.FaceValue == FaceValue.Ace)
            AceTotal++;

        _cards.Add(card);
        AddedCard?.Invoke(PileID);
    }

    public void AddCards(CardInteraction[] cards)
    {
        foreach (CardInteraction card in cards)
            AddCard(card);
    }

    public void ResetPile()
    {
        _cards.Clear();
        PointCardTotal = 0;
        SpadeCardTotal = 0;
        HasTenOfDiamonds = false;
        HasTwoOfSpades = false;

        AceTotal = 0;
    }
}
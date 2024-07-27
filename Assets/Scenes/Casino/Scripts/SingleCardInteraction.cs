using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SingleCardInteraction : CardInteraction, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [field: SerializeField] public CardType CardType { get; set; }

    public Card Card { get; private set; }

    private void Awake()
    {
        Selected = false;
        InteractionType = InteractionType.Single;
    }

    #region Pointer Callbacks

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (CardType == CardType.OtherPlayer)
            return;

        base.OnPointerEnter(eventData);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (CardType == CardType.OtherPlayer)
            return;

        base.OnPointerExit(eventData);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (CardType == CardType.OtherPlayer)
            return;

        base.OnPointerClick(eventData);
    }

    #endregion

    public void SetCard(Card card)
    {
        Card = card;
        Value = card.NumberValue;
    }
}

public enum CardType
{
    InHand,
    OnTable,
    OtherPlayer
}
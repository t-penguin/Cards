using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public static event Action<CardType, bool, RectTransform> HoverCard;
    public static event Action<CardType, RectTransform, PointerEventData> SelectCard;

    [SerializeField] CardType _cardType;
    RectTransform _rect;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    #region Pointer Callbacks

    public void OnPointerEnter(PointerEventData eventData)
    {
        HoverCard?.Invoke(_cardType, true, _rect);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HoverCard?.Invoke(_cardType, false, _rect);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        SelectCard?.Invoke(_cardType, _rect, eventData);
    }

    #endregion
}

public enum CardType
{
    InHand,
    InPlay
}
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public static event Action<CardInteraction, PointerEventData> ClickedCard;

    [field: SerializeField] public CardType CardType { get; set; }
    [SerializeField] GameObject _selector;

    public Card Card { get; set; }
    public bool IsSelected { get; set; }

    private void Awake()
    {
        IsSelected = false;
    }

    #region Pointer Callbacks

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CardType == CardType.OtherPlayer)
            return;

        if (IsSelected)
            return;

        _selector.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (CardType == CardType.OtherPlayer)
            return;

        if (IsSelected)
            return;

        _selector.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (CardType == CardType.OtherPlayer)
            return;

        ClickedCard?.Invoke(this, eventData); 
    }

    #endregion

    public void HideSelector() => _selector.SetActive(false);

    public Image GetSelectorImage() => _selector.GetComponent<Image>();

    public void ResizeSelector(Vector2 size) => _selector.GetComponent<RectTransform>().sizeDelta = size;
}

public enum CardType
{
    InHand,
    OnTable,
    OtherPlayer
}
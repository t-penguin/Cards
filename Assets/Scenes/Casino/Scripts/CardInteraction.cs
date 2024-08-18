using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class CardInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public static event Action<CardInteraction, InteractionType, PointerEventData> ClickedCard;

    [SerializeField] GameObject _selector;

    public bool Selected { get; set; }
    public int Value { get; protected set; }
    public InteractionType InteractionType { get; protected set; }

    private Color _transparent = new Color(0, 0, 0, 0);

    #region Pointer Callbacks

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if (Selected)
            return;

        ShowSelector();
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        if (Selected)
            return;

        HideSelector();
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        ClickedCard?.Invoke(this, InteractionType, eventData);
    }

    #endregion

    public virtual void Hide()
    {
        GetComponent<Image>().color = _transparent;
        HideSelector();
    }

    public void ShowSelector() => _selector.SetActive(true);
    public void HideSelector() => _selector.SetActive(false);
    public void SetSelectorColor(Color color) => _selector.GetComponent<Image>().color = color;
    public void ResizeSelector(Vector2 size) => _selector.GetComponent<RectTransform>().sizeDelta = size;
}

public enum InteractionType
{
    Single,
    Stack
}
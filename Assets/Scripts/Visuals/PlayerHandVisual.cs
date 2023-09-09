using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayerHandVisual : MonoBehaviour
{
    [SerializeField] GameManager _gameManager;
    [SerializeField] List<Image> _handVisual;
    [SerializeField] Player _player;
    [SerializeField] RectTransform _hoverOutline;
    [SerializeField] RectTransform _selectionOutline;

    private int _handIndex;

    #region Monobehaviour Callbacks

    // Subscribe to events
    private void OnEnable()
    {
        Player.ReceivedCard += OnReceivedCard;
        CardInteraction.HoverCard += OnHoverCard;
        CardInteraction.SelectCard += OnSelectCard;
    }

    // Unsubscribe from events
    private void OnDisable()
    {
        Player.ReceivedCard -= OnReceivedCard;
        CardInteraction.HoverCard -= OnHoverCard;
        CardInteraction.SelectCard -= OnSelectCard;
    }

    // Set up and validate game objects for visuals.
    private void Awake()
    {
        if(_handVisual.Count == 0)
        {
            Debug.LogWarning("Hand Visual not set up! Setting it up now!");
            GetComponentsInChildren(true, _handVisual);
        }

        if(_handVisual.Count != 4)
            Debug.LogError($"Invalid number of visuals in hand. Expected: 4, Actual: {_handVisual.Count}");

        _handIndex = 0;
    }

    #endregion

    #region Event Callbacks

    // Displays the card in this player's hand if the player receiving the card matches this player
    private void OnReceivedCard(Player player, Card card)
    {
        if (player != _player)
            return;

        _handIndex %= 4;
        string cardName = $"{card.Suit}_{card.FaceValue}";
        _handVisual[_handIndex].sprite = _gameManager.GetCardSpriteByName(cardName);
        _handVisual[_handIndex].gameObject.SetActive(true);
        _handIndex++;
    }

    // Displays an outline when hovering over a card in this player's hand
    private void OnHoverCard(CardType type, bool hovering, RectTransform card)
    {
        if (type != CardType.InHand)
            return;

        if(!hovering)
        {
            _hoverOutline.gameObject.SetActive(false);
            return;
        }

        _hoverOutline.anchoredPosition = card.anchoredPosition;
        _hoverOutline.gameObject.SetActive(true);
    }

    // Displays (or removes) an outline around the selected card in hand regardless of where the player is hovering
    private void OnSelectCard(CardType type, RectTransform card, PointerEventData eventData)
    {
        if (type != CardType.InHand)
            return;

        // Left-click to select
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            _selectionOutline.anchoredPosition = card.anchoredPosition;
            _selectionOutline.gameObject.SetActive(true);
            return;
        }
        
        // Right-click to deselect
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            _selectionOutline.gameObject.SetActive(false);
            return;
        }
    }

    #endregion
}
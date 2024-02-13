using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class TurnManager : MonoBehaviour
{
    // TODO: [CARDS] Figure out turn actions and events

    private const string TARGET_VALUE_TEXT = "Target Value:";

    [SerializeField] GameObject _buttonsPanel;
    [SerializeField] TextMeshProUGUI _targetValueText;

    List<CardInteraction> _selectedTableCards;
    CardInteraction _selectedHandCard;
    CardInteraction _targetValueCard;
    int _targetValue;

    #region Monobehaviour Callbacks

    private void Awake()
    {
        _selectedTableCards = new List<CardInteraction>();

        // TODO: [CARDS] Remove this once the events are set up
        OnTurnStarted();
    }

    private void OnEnable()
    {
        CardInteraction.ClickedCard += OnClickedCard; 
    }

    private void OnDisable()
    {
        CardInteraction.ClickedCard -= OnClickedCard;
    }

    #endregion

    #region PUN Event Callbacks

    private void OnTurnStarted()
    {
        _targetValueText.text = TARGET_VALUE_TEXT;
        _targetValueText.gameObject.SetActive(true);
    }

    private void OnTurnEnded()
    {
        _targetValueText.gameObject.SetActive(false );
    }

    #endregion

    #region Event Callbacks

    private void OnClickedCard(CardInteraction cardInteraction, PointerEventData eventData)
    {
        CardType cardType = cardInteraction.CardType;

        if (cardType == CardType.InHand)
        {
            HandleHandClick(cardInteraction, eventData);
        }
        else if (cardType == CardType.OnTable)
        {
            HandleTableClick(cardInteraction, eventData);
        }
    }

    private void HandleHandClick(CardInteraction cardInteraction, PointerEventData eventData)
    {
        bool alreadySelected = cardInteraction.IsSelected;

        if (alreadySelected)
        {
            // Deselect this card
            _selectedHandCard = null;
            cardInteraction.IsSelected = false;

            _buttonsPanel.SetActive(false);
            return;
        }

        // Deselect the previous card if one has been selected
        if (_selectedHandCard != null)
        {
            _selectedHandCard.IsSelected = false;
            _selectedHandCard.HideSelector();
        }

        // Select this card
        cardInteraction.IsSelected = true;
        _selectedHandCard = cardInteraction;

        /* Selecting a card in hand should:
         * - Bring up a button to set this as the target value for stacks or pick ups
         * - Bring up a button for just dropping the card on the table
         * */
        _buttonsPanel.SetActive(true);
    }

    private void HandleTableClick(CardInteraction cardInteraction, PointerEventData eventData)
    {
        bool alreadySelected = cardInteraction.IsSelected;

        if (alreadySelected)
        {
            // Deselect this card
            _selectedTableCards.Remove(cardInteraction);
            cardInteraction.IsSelected = false;
            return;
        }

        _selectedTableCards.Add(cardInteraction);
        cardInteraction.IsSelected = true;
    }

    #endregion

    #region Button Actions

    public void DropCard()
    {
        // TODO: [CARDS] Send Drop Card event to the game manager that sends event over PUN
        string cardName = _selectedHandCard.Card.Name;
        Debug.Log($"Selected to drop the {_selectedHandCard.Card}");
    }

    // Sets the selected card as the target value and updates the text
    public void SetCardAsTargetValue()
    {
        // TODO: [CARDS] Set the color of the selector to red if it's the target value
        _targetValueCard = _selectedHandCard;
        _targetValue = _targetValueCard.Card.NumberValue;
        _targetValueText.text = $"{TARGET_VALUE_TEXT} {_targetValue}";
    }

    #endregion
}
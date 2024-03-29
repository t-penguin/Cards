using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Linq;

public class TurnManager : MonoBehaviour
{
    // TODO: [CARDS] Figure out turn actions and events
    /* Possible Turn Actions:
     * X Drop Card
     *   Stack Cards
     *   Take Card(s)
     * */
    // TODO: [CARDS] Add clear stacks button

    private const string TARGET_VALUE_TEXT = "Target Value:";

    [SerializeField] GameObject _playerHand;
    [SerializeField] GameObject _buttonsPanel;
    [SerializeField] TextMeshProUGUI _targetValueText;

    List<CardInteraction> _playerCards;
    List<CardInteraction> _selectedTableCards;
    List<CardInteraction> _stackedCards;
    CardInteraction _selectedHandCard;
    CardInteraction _targetValueCard;
    int _targetValue;
    bool _handCardInStack;

    PhotonPlayer[] TurnOrder;
    int _playerTurnIndex;
    int _currentTurnIndex;

    Color _redOutline = new Color(0.5f, 0f, 0f);
    Color _blueOutline = new Color(0f, 0f, 0.5f);
    Color _blackOutline = new Color(0f, 0f, 0f);

    #region Monobehaviour Callbacks

    private void Awake()
    {
        _selectedTableCards = new List<CardInteraction>();
        _stackedCards = new List<CardInteraction>();
    }

    private void OnEnable()
    {
        CardInteraction.ClickedCard += OnClickedCard;
        PhotonNetwork.OnEventCall += OnSetStarted;
        PhotonNetwork.OnEventCall += OnTurnStarted;
        PhotonNetwork.OnEventCall += OnTurnEnded;
    }

    private void OnDisable()
    {
        CardInteraction.ClickedCard -= OnClickedCard;
        PhotonNetwork.OnEventCall -= OnSetStarted;
        PhotonNetwork.OnEventCall -= OnTurnStarted;
        PhotonNetwork.OnEventCall -= OnTurnEnded;
    }

    #endregion

    #region PUN Event Callbacks

    private void OnSetStarted(byte eventCode, object content, int senderID)
    {
        if (eventCode != EventManager.GAME_SET_STARTED)
            return;

        TurnOrder = (PhotonPlayer[])PhotonNetwork.room.CustomProperties[GameManager.TURN_ORDER_KEY];
        _playerTurnIndex = Array.IndexOf(TurnOrder, PhotonNetwork.player);
    }

    private void OnTurnStarted(byte eventCode, object content, int senderId)
    {
        if (eventCode != EventManager.TURN_STARTED_EVENT_CODE)
            return;

        if (_playerCards == null || _playerCards.Count == 0)
            _playerCards = _playerHand.GetComponentsInChildren<CardInteraction>().ToList();

        int currentTurn = PhotonNetwork.room.GetTurn();
        _currentTurnIndex = currentTurn % TurnOrder.Length;

        if (_currentTurnIndex != _playerTurnIndex)
            return;

        _targetValueText.text = TARGET_VALUE_TEXT;
        _targetValueText.gameObject.SetActive(true);
    }

    private void OnTurnEnded(byte eventCode, object content, int senderId)
    {
        if(eventCode != EventManager.TURN_ENDED_EVENT_CODE)
            return;

        if (_currentTurnIndex != _playerTurnIndex)
            return;

        _targetValueText.gameObject.SetActive(false);
    }

    #endregion

    #region Event Callbacks

    private void OnClickedCard(CardInteraction cardInteraction, PointerEventData eventData)
    {
        if(_currentTurnIndex != _playerTurnIndex)
            return;

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
        if (_handCardInStack)
            return;

        bool alreadySelected = cardInteraction.IsSelected;
        bool handCardIsTarget = _selectedHandCard == _targetValueCard;

        if (alreadySelected && !handCardIsTarget)
        {
            // Deselect this card
            _selectedHandCard = null;
            cardInteraction.IsSelected = false;

            _buttonsPanel.SetActive(false);
            return;
        }

        // Deselect the previous card if one has been selected unless it's the target value
        if (_selectedHandCard != null && !handCardIsTarget)
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
        bool cardInStack = false;
        if(_stackedCards != null)
            cardInStack = _stackedCards.Contains(cardInteraction);

        if (cardInStack)
            return;

        bool alreadySelected = cardInteraction.IsSelected;

        if (alreadySelected)
        {
            // Deselect this card
            DeselectTableCard(cardInteraction);
            return;
        }

        SelectTableCard(cardInteraction);
    }

    #endregion

    #region Button Actions

    // Drops the selected card from the player's hand
    public void DropCard()
    {
        string cardName = _selectedHandCard.Card.Name;
        Debug.Log($"Selected to drop the {_selectedHandCard.Card}");

        _selectedHandCard.IsSelected = false;
        _selectedHandCard.HideSelector();
        SetSelectorColor(_selectedHandCard, _blackOutline);

        if(_targetValueCard != null)
        {
            _targetValueCard.IsSelected = false;
            _targetValueCard.HideSelector();
            SetSelectorColor(_targetValueCard, _blackOutline);
            _targetValueCard = null;
        }

        foreach(CardInteraction card in _selectedTableCards)
        {
            card.IsSelected = false;
            card.HideSelector();
        }
        _selectedTableCards.Clear();

        int cardIndex = _playerCards.IndexOf(_selectedHandCard);
        int[] dropInfo = { _playerTurnIndex, cardIndex };
        _playerCards.Remove(_selectedHandCard);

        _buttonsPanel.SetActive(false);

        EventManager.RaisePhotonEvent(EventManager.TURN_DROP_CARD_EVENT_CODE, dropInfo);
    }

    // Sets the selected card as the target value and updates the text
    public void SetCardAsTargetValue()
    {
        if(_targetValueCard != null)
        {
            _targetValueCard.IsSelected = false;
            _targetValueCard.HideSelector();
            SetSelectorColor(_targetValueCard, _blackOutline);
        }

        _targetValueCard = _selectedHandCard;
        _targetValue = _targetValueCard.Card.NumberValue;
        _targetValueText.text = $"{TARGET_VALUE_TEXT} {_targetValue}";

        SetSelectorColor(_targetValueCard, _redOutline);
    }

    // Attempts to add the selected cards to the stack
    public void StackCards()
    {
        // Total up the value of all the selected cards
        int stackValue = 0;

        foreach (CardInteraction cardInteraction in _selectedTableCards)
        {
            stackValue += cardInteraction.Card.NumberValue;
        }

        // Do NOT include the target value card,
        // but do include the selected card in hand if it's not the target value
        bool selectedCardIsTarget = _selectedHandCard == _targetValueCard;
        if (!selectedCardIsTarget)
        {
            stackValue += _selectedHandCard.Card.NumberValue;
        }
        
        // Invalid selection due to stack value
        if (stackValue != _targetValue)
        {
            Debug.Log($"Stack value of {stackValue} does not match the target value of {_targetValue}");
            return;
        }

        // Outline stacked cards in blue to show that they're part of the stack
        foreach (CardInteraction cardInteraction in _selectedTableCards)
        {
            SetSelectorColor(cardInteraction, _blueOutline);
            _stackedCards.Add(cardInteraction);
        }

        if(!selectedCardIsTarget)
        {
            SetSelectorColor(_selectedHandCard, _blueOutline);
            _stackedCards.Add(_selectedHandCard);
        }

         _selectedTableCards.Clear();
    }

    #endregion

    private void SelectTableCard(CardInteraction card)
    {
        _selectedTableCards.Add(card);
        card.IsSelected = true;
    }

    private void DeselectTableCard(CardInteraction card)
    {
        _selectedTableCards.Remove(card);
        card.IsSelected = false;
    }

    private void SetSelectorColor(CardInteraction card, Color color)
    {
        Image selectorImage = card.GetSelectorImage();
        selectorImage.color = color;
    }
}
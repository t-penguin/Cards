using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

public class TurnManager : MonoBehaviour
{
    // TODO: [CARDS] Figure out turn actions and events
    /* Possible Turn Actions:
     * X Drop Card
     * X Stack Cards
     *   Take Card(s)
     * */
    // TODO: [CARDS] Add clear stacks button

    public static event Action<StackedCardsInteraction> StackCleared; 
    public static void ClearStack(StackedCardsInteraction stack) => StackCleared?.Invoke(stack);

    private const string TARGET_VALUE_TEXT = "Target Value:";

    [SerializeField] GameObject _playerHand;
    [SerializeField] GameObject _buttonsPanel;
    [SerializeField] TextMeshProUGUI _targetValueText;

    List<SingleCardInteraction> _playerCards;
    List<CardInteraction> _selectedTableCards;
    List<CardInteraction> _stackedCards;
    SingleCardInteraction _selectedHandCard;
    SingleCardInteraction _targetValueCard;
    List<SingleCardInteraction> _stackTargetCards;
    int _targetValue;
    bool _handCardInStack;
    bool _stackIsLocked;

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
        _stackTargetCards = new List<SingleCardInteraction>();
    }

    private void OnEnable()
    {
        CardInteraction.ClickedCard += OnClickedCard;
        PhotonNetwork.OnEventCall += OnSetStarted;
        PhotonNetwork.OnEventCall += OnTurnStarted;
        PhotonNetwork.OnEventCall += OnTurnEnded;

        StackCleared += OnStackCleared;
    }

    private void OnDisable()
    {
        CardInteraction.ClickedCard -= OnClickedCard;
        PhotonNetwork.OnEventCall -= OnSetStarted;
        PhotonNetwork.OnEventCall -= OnTurnStarted;
        PhotonNetwork.OnEventCall -= OnTurnEnded;

        StackCleared -= OnStackCleared;
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
            _playerCards = _playerHand.GetComponentsInChildren<SingleCardInteraction>().ToList();

        int currentTurn = PhotonNetwork.room.GetTurn();
        _currentTurnIndex = currentTurn % TurnOrder.Length;

        if (_currentTurnIndex != _playerTurnIndex)
            return;

        _targetValueText.text = TARGET_VALUE_TEXT;
        _targetValueText.gameObject.SetActive(true);

        _buttonsPanel.SetActive(true);
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

    private void OnClickedCard(CardInteraction cardInteraction, InteractionType interactionType,
                                PointerEventData eventData)
    {
        if (_currentTurnIndex != _playerTurnIndex)
            return;
        
        if (interactionType == InteractionType.Stack)
        {
            StackedCardsInteraction stack = cardInteraction as StackedCardsInteraction;
            HandleTableClick(stack, eventData);
            return;
        }

        SingleCardInteraction card = cardInteraction as SingleCardInteraction;

        CardType cardType = card.CardType;

        if (cardType == CardType.InHand)
            HandleHandClick(card, eventData);
        else if (cardType == CardType.OnTable)
            HandleTableClick(card, eventData);
    }

    private void HandleHandClick(SingleCardInteraction card, PointerEventData eventData)
    {
        if (card == null)
            return;

        if (_handCardInStack)
            return;

        if (card == _targetValueCard)
            return;

        bool alreadySelected = card.Selected;
        DeselectHandCard(_selectedHandCard);

        // Deselecting a card that was already selected
        if (alreadySelected)
            return;

        // Selecting a new card
        SelectHandCard(card);
    }

    private void HandleTableClick(CardInteraction card, PointerEventData eventData)
    {
        bool alreadyInStack = false;
        if(_stackedCards != null)
            alreadyInStack = _stackedCards.Contains(card);

        if (alreadyInStack)
            return;

        bool alreadySelected = card.Selected;

        if (alreadySelected)
        {
            // Deselect this card
            DeselectTableCard(card);
            return;
        }

        SelectTableCard(card);
    }

    private void OnStackCleared(StackedCardsInteraction stack)
    {
        SingleCardInteraction card = stack.TargetCard;
        if (_stackTargetCards.Contains(card))
            _stackTargetCards.Remove(card);
    }

    #endregion

    #region Button Actions

    public void PlayTurnAction(TurnAction turnAction)
    {
        byte eventCode = 0;
        List<int> turnInfo = new List<int> { _playerTurnIndex };

        int handCardIndex = _playerCards.IndexOf(_selectedHandCard);
        int targetCardIndex = _playerCards.IndexOf(_targetValueCard);

        if (handCardIndex == -1)
        {
            Debug.Log("Cannot do this action. You must play a card from your hand.");
            return;
        }

        switch (turnAction)
        {
            case TurnAction.DropCard:
                eventCode = EventManager.TURN_DROP_CARD_EVENT_CODE;

                if (!ValidDrop())
                    return;

                turnInfo.Add(handCardIndex);
                break;
            case TurnAction.StackCards:
                eventCode = EventManager.TURN_STACK_CARDS_EVENT_CODE;

                if (!ValidStack())
                    return;

                /* The target value card is bound to this stack and cannot
                 * be played unless the stack is picked up or changed, or
                 * the target value card is added to the stack. */
                _stackTargetCards.Add(_targetValueCard);

                int locked = _stackIsLocked ? 1 : 0;
                turnInfo.Add(_targetValue);
                turnInfo.Add(locked);
                turnInfo.Add(targetCardIndex);
                turnInfo.Add(handCardIndex);

                List<int> tableIndexes = new List<int>();
                foreach (CardInteraction card in _stackedCards)
                {
                    int tableIndex = TableVisual.TableHand.IndexOf(card);

                    /* The card from the sender's hand will be in _stackedCards
                     * but not in TableHand. It's index is already included in
                     * the stackInfo before this loop */
                    if (tableIndex != -1)
                        tableIndexes.Add(tableIndex);
                }
                tableIndexes.Sort();
                turnInfo.AddRange(tableIndexes);
                break;
            case TurnAction.PickUpCards:
                

                break;
        }

        // Deselect all cards
        DeselectCard(_selectedHandCard);
        _playerCards.Remove(_selectedHandCard);
        _handCardInStack = false;
        DeselectCard(_targetValueCard);
        _targetValueCard = null;
        _targetValue = 0;
        DeselectCards(_selectedTableCards);
        DeselectCards(_stackedCards);

        _stackIsLocked = false;
        _buttonsPanel.SetActive(false);

        EventManager.RaisePhotonEvent(eventCode, turnInfo.ToArray());
    }

    public void DropCard() => PlayTurnAction(TurnAction.DropCard);
    public void StackCards() => PlayTurnAction(TurnAction.StackCards);
    public void PickUpCards() => PlayTurnAction(TurnAction.PickUpCards);

    // Sets the selected card as the target value and updates the text
    public void SetCardAsTargetValue()
    {
        if (_playerCards.Count < 2)
            return;

        DeselectCard(_targetValueCard);
        _targetValueCard = _selectedHandCard;

        int targetValue = _targetValueCard.Value;
        if (_targetValue != targetValue)
        {
            foreach (CardInteraction card in _stackedCards)
                DeselectCard(card);
            _stackedCards.Clear();
            _stackIsLocked = false;
            _handCardInStack = false;
            _targetValue = targetValue;
        }
        _targetValueText.text = $"{TARGET_VALUE_TEXT} {_targetValue}";
        _targetValueCard.SetSelectorColor(_redOutline);
        _targetValueCard.ShowSelector();

        if (!_handCardInStack)
            _selectedHandCard = null;
    }

    // Attempts to add the selected cards to the stack
    public void AddSelectedCardsToStack()
    {
        // Total up the value of all the selected cards
        int stackValue = 0;
        int totalSelectedCards = _selectedTableCards.Count;

        // Do NOT include the target value card,
        // but do include the selected card in hand if it's not the target value
        // and it is not already in the stack.
        bool handCardIsTarget = _selectedHandCard == _targetValueCard;
        bool addSelectedHandCard = _selectedHandCard != null && !handCardIsTarget && !_handCardInStack;
        if (addSelectedHandCard)
        {
            totalSelectedCards++;
            if (_selectedHandCard.Value == 14 && totalSelectedCards > 1)
                stackValue++;
            else
                stackValue += _selectedHandCard.Value;
        }

        foreach (CardInteraction card in _selectedTableCards)
        {
            if (card.InteractionType == InteractionType.Stack)
            {
                StackedCardsInteraction stack = card as StackedCardsInteraction;
                // Locked stacks cannot be built upon unless they match the target value
                if (stack.Locked && stack.Value != _targetValue)
                {
                    Debug.Log($"The selected stack has a value of {stack.Value}. " +
                        $"Cannot change it to {_targetValue}");
                    return;
                }
            }

            // If Aces are alone they have a value of 14, otherwise they have a value of 1
            if (card.Value == 14 && totalSelectedCards > 1)
                stackValue++;
            else
                stackValue += card.Value;
        }
        
        // Invalid selection due to stack value
        if (stackValue != _targetValue)
        {
            Debug.Log($"Stack value of {stackValue} does not match the target value of {_targetValue}");
            return;
        }

        if (!_stackIsLocked && _stackedCards.Count > 0)
            _stackIsLocked = true;

        // Outline stacked cards in blue to show that they're part of the stack
        foreach (CardInteraction card in _selectedTableCards)
            AddCardToStack(card);
        _selectedTableCards.Clear();

        if (!handCardIsTarget)
            AddCardToStack(_selectedHandCard);
    }

    #endregion

    #region Card Selection

    private void DeselectCard(CardInteraction card)
    {
        if (card == null)
            return;

        if (!card.Selected)
            return;

        card.Selected = false;
        card.HideSelector();
        card.SetSelectorColor(_blackOutline);
    }

    private void DeselectCards(List<CardInteraction> cards)
    {
        foreach (CardInteraction card in cards)
            DeselectCard(card);
        cards.Clear();
    }

    private void SelectHandCard(SingleCardInteraction card)
    {
        if (card == null)
            return;

        _selectedHandCard = card;
        card.Selected = true;
        card.ShowSelector();
    }

    private void DeselectHandCard(SingleCardInteraction card)
    {
        if (card == null)
            return;

        DeselectCard(card);
        _selectedHandCard = null;
    }

    private void SelectTableCard(CardInteraction card)
    {
        if (card == null)
            return;

        _selectedTableCards.Add(card);
        card.Selected = true;
        card.ShowSelector();
    }

    private void DeselectTableCard(CardInteraction card)
    {
        if (card == null)
            return;

        DeselectCard(card);
        _selectedTableCards.Remove(card);
    }

    #endregion

    #region Action Validation

    bool ValidDrop()
    {
        // Make sure the player isn't trying to drop a stack's target card
        if (_stackTargetCards.Contains(_selectedHandCard))
        {
            Debug.Log("Cannot drop this card, it is tied to a stack on the table.");
            return false;
        }

        return true;
    }

    bool ValidStack()
    {
        // Make sure the player is adding a card from their hand to the stack
        if (!_handCardInStack)
        {
            Debug.Log("Cannot stack these cards: Missing a card from your hand.");
            return false;
        }

        // Make sure the player is not using a stack's target card in a new stack
        if (_stackTargetCards.Contains(_selectedHandCard))
        {
            bool targetStackInStack = false;
            foreach (CardInteraction card in _stackedCards)
            {
                StackedCardsInteraction stack = card as StackedCardsInteraction;
                if (stack == null)
                    continue;

                if (stack.TargetCard == _selectedHandCard)
                    targetStackInStack = true;
            }

            if (!targetStackInStack)
            {
                Debug.Log("Cannot add the card in hand to this stack: It is bound to another stack.");
                return false;
            }
        }

        return true;
    }

    bool ValidPickUp()
    {
        return true;
    }

    #endregion

    private void AddCardToStack(CardInteraction card)
    {
        if (card == null)
            return;

        if (card == _selectedHandCard)
            _handCardInStack = true;

        _stackedCards.Add(card);
        card.SetSelectorColor(_blueOutline);
    }
}

public enum TurnAction
{
    DropCard,
    StackCards,
    PickUpCards
}

class StackTargetPair
{
    public StackedCardsInteraction Stack { get; private set; }
    public SingleCardInteraction TargetCard { get; private set; }

    public StackTargetPair(StackedCardsInteraction stack, SingleCardInteraction targetCard)
    {
        Stack = stack;
        TargetCard = targetCard;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Linq;

public class TurnManager : MonoBehaviour
{
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
    int _currentStackValue;
    bool _handCardInStack;
    bool _lockStack;

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
        if (eventCode != EventManager.GAME_DEAL_HAND_EVENT_CODE)
            return;

        TurnOrder = (PhotonPlayer[])PhotonNetwork.room.CustomProperties[GameManager.TURN_ORDER_KEY];
        _playerTurnIndex = Array.IndexOf(TurnOrder, PhotonNetwork.player);
    }

    private void OnTurnStarted(byte eventCode, object content, int senderId)
    {
        if (eventCode != EventManager.GAME_START_TURN_EVENT_CODE)
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
        if(eventCode != EventManager.GAME_END_TURN_EVENT_CODE)
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
        {
            if (_selectedHandCard == null)
                _selectedHandCard = card;

            return;
        }

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

    // Removes the link between a card and stack when the stack is cleared
    private void OnStackCleared(StackedCardsInteraction stack)
    {
        Debug.Log("Clearing stack...");
        SingleCardInteraction card = stack.TargetCard;
        if (_stackTargetCards.Contains(card))
        {
            _stackTargetCards.Remove(card);
            Debug.Log("Stack cleared.");
            return;
        }

        Debug.Log("Stack clear failed.\n" +
            $"Could not clear stack with value {stack.Value}." +
            $"Stack Target Card: {(card == null ? "N/A" : card.Card.Name)}");
    }

    #endregion

    #region Button Actions

    public void Drop() => PlayTurnAction(TurnAction.Drop);
    public void Group() => AddToStack(_targetValue, true);
    public void Stack() => PlayTurnAction(TurnAction.Stack);
    public void PickUp() => PlayTurnAction(TurnAction.PickUp);

    // Sets the selected card as the target value and updates the text
    public void SetTargetValue()
    {
        DeselectCard(_targetValueCard);
        _targetValueCard = _selectedHandCard;

        int targetValue = _targetValueCard.Value;
        if (_targetValue != targetValue)
        {
            ClearStackedCards();
            _lockStack = false;
            _handCardInStack = false;
            _targetValue = targetValue;
        }
        _targetValueText.text = $"{TARGET_VALUE_TEXT} {_targetValue}";
        _targetValueCard.SetSelectorColor(_redOutline);
        _targetValueCard.ShowSelector();
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

    private void ClearStackedCards()
    {
        DeselectCards(_stackedCards);
        _currentStackValue = 0;
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
        if (card == null || card == _targetValueCard)
            return;

        DeselectCard(card);
        _selectedHandCard = null;
    }

    private void SelectTableCard(CardInteraction card)
    {
        if (card == null)
            return;

        // Prevents duplicate selection
        if(!_selectedTableCards.Contains(card))
            _selectedTableCards.Add(card);

        card.Selected = true;
        card.ShowSelector();
    }

    private void DeselectTableCard(CardInteraction card)
    {
        if (card == null || !_selectedTableCards.Contains(card))
            return;

        DeselectCard(card);
        _selectedTableCards.Remove(card);
    }

    #endregion

    #region Action Validation

    bool IsValidDrop()
    {
        /* If the player has a stack, or multiple, on the table, they cannot drop
         * a card until all of their stacks have been picked up */
        if (_stackTargetCards.Count > 0)
        {
            Debug.Log("Cannot drop a card on this turn. Cards must be either picked up " +
                "or stacked on this turn.");
            return false;
        }

        return true;
    }

    bool IsValidStack()
    {
        // The player cannot attempt to stack cards if they haven't chosen any cards to stack
        if (_stackedCards.Count == 0)
        {
            Debug.Log("No cards selected to stack!");
            return false;
        }

        /* The player can only stack cards if they will have the stack's target
         * card in their hand after this turn. Thus, they must have at least 2
         * cards in hand. */
        if (_playerCards.Count < 2)
        {
            Debug.Log("Cannot stack cards during this turn, not enough cards in hand.");
            return false;
        }

        // The player cannot use an existing stack's target card in a new stack
        if (_stackTargetCards.Contains(_selectedHandCard))
        {
            string msg = "Cannot add the card in hand to this stack: It is bound to another stack.";
            if (!IsTargetStackInStack(msg))
                return false;
        }

        // The player cannot put a single card into a stack unless it is an Ace
        if (_stackedCards.Count == 1)
        {
            SingleCardInteraction card = (SingleCardInteraction)_stackedCards[0];
            if (card.Card.FaceValue != FaceValue.Ace)
            {
                Debug.Log("Cannot stack a single card unless it's an Ace.");
                return false;
            }
        }

        return true;
    }

    bool IsValidPickUp()
    {
        /* Prevent the player from trying to pick up cards using a card that
         * is a part of a stack they've attempted to make. */
        if (_handCardInStack)
        {
            Debug.Log("Cannot play this action because this card was stacked.");
            return false;
        }

        // The player can only pick up cards that add up to value of their selected card.
        int handValue = _selectedHandCard.Value;
        if (_currentStackValue != 0 && handValue != _currentStackValue)
        {
            Debug.Log("Cannot play this action because this card's value does " +
                "not matched the value of the stacked cards.");
            return false;
        }

        // Check for any remaining selected table cards not in a stack
        // If there are any, add them to the stacked cards
        switch(_selectedTableCards.Count)
        {
            case 0:
                if (_stackedCards.Count == 0)
                {
                    Debug.Log("No cards selected to pick up.");
                    return false;
                }
                break;
            case 1:
                int tableValue = _selectedTableCards[0].Value;
                if (handValue != tableValue)
                {
                    Debug.Log($"Cannot pick up the selected table card. Hand card value of " +
                        $"{handValue} does not match the table card value of {tableValue}.");
                    return false;
                }
                _stackedCards.Add(_selectedTableCards[0]);
                break;
            default:
                Debug.Log($"Attempting to add the selected table cards to a stack with value {handValue}.");
                if(!AddToStack(handValue, false))
                {
                    Debug.Log($"Cannot pick up the selected table cards.");
                    return false;
                }
                break;
        }

        /* The player cannot use an existing stack's target card
         * to pick up cards without picking up the stack. */
        if (_stackTargetCards.Contains(_selectedHandCard))
        {
            string msg = "You must pick up the target stack if you wish to play this card.";
            if (!IsTargetStackInStack(msg))
                return false;
        }

        return true;
    }

    bool IsTargetStackInStack(string failMessage)
    {
        foreach (CardInteraction card in _stackedCards)
        {
            StackedCardsInteraction stack = card as StackedCardsInteraction;
            if (stack == null)
                continue;

            if (stack.TargetCard == _selectedHandCard)
                return true;
        }

        Debug.Log(failMessage);
        return false;
    }

    #endregion

    #region Actions

    private bool AddToStack(int targetValue, bool includeHandCard)
    {
        if (_currentStackValue != 0 && targetValue != _currentStackValue)
        {
            Debug.Log($"Target value ({targetValue}) does not match " +
                $"the current stack value ({_currentStackValue}).");
            return false;
        }

        // Total up the value of all the selected cards
        int stackValue = 0;
        int totalSelectedCards = _selectedTableCards.Count;

        bool handCardIsTarget = _selectedHandCard == _targetValueCard;
        if (includeHandCard)
        {
            // Do NOT include the target value card,
            // but do include the selected card in hand if it's not the target value
            // and it is not already in the stack.
            bool addSelectedHandCard = _selectedHandCard != null && !handCardIsTarget && !_handCardInStack;
            if (addSelectedHandCard)
            {
                totalSelectedCards++;
                if (_selectedHandCard.Value == 14 && totalSelectedCards > 1)
                    stackValue++;
                else
                    stackValue += _selectedHandCard.Value;
            }
        }

        foreach (CardInteraction card in _selectedTableCards)
        {
            if (card.InteractionType == InteractionType.Stack)
            {
                StackedCardsInteraction stack = card as StackedCardsInteraction;
                // Locked stacks cannot be built upon unless they match the target value
                if (stack.Locked && stack.Value != targetValue)
                {
                    Debug.Log($"The selected stack has a value of {stack.Value}. " +
                        $"Cannot change it to {targetValue}");
                    return false;
                }
            }

            // If Aces are alone they have a value of 14, otherwise they have a value of 1
            if (card.Value == 14 && totalSelectedCards > 1)
                stackValue++;
            else
                stackValue += card.Value;
        }

        // Invalid selection due to stack value
        if (stackValue != targetValue)
        {
            Debug.Log($"Stack value of {stackValue} does not match the target value of {targetValue}");
            return false;
        }

        // Set the current stack value
        // These values are already equal unless the current stack value is 0
        _currentStackValue = targetValue;

        // Set flag to lock stack if player is stacking cards as their turn action
        if (!_lockStack && _stackedCards.Count > 0)
            _lockStack = true;

        // Outline stacked cards in blue to show that they're part of the stack
        foreach (CardInteraction card in _selectedTableCards)
            AddCardToStack(card);
        _selectedTableCards.Clear();

        if (!handCardIsTarget && includeHandCard)
            AddCardToStack(_selectedHandCard);

        return true;
    }

    private void PlayTurnAction(TurnAction turnAction)
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
            case TurnAction.Drop:
                eventCode = EventManager.TURN_DROP_CARD_EVENT_CODE;

                if (!IsValidDrop())
                    return;

                turnInfo.Add(handCardIndex);
                break;
            case TurnAction.Stack:
                eventCode = EventManager.TURN_STACK_CARDS_EVENT_CODE;

                if (!IsValidStack())
                    return;

                /* The target value card is bound to this stack and cannot
                 * be played unless the stack is picked up or changed, or
                 * the target value card is added to the stack. */
                _stackTargetCards.Add(_targetValueCard);

                int locked = _lockStack ? 1 : 0;
                turnInfo.Add(_targetValue);
                turnInfo.Add(locked);
                turnInfo.Add(targetCardIndex);
                AddStackedCardsToInfo(handCardIndex, turnInfo);
                break;
            case TurnAction.PickUp:
                eventCode = EventManager.TURN_PICK_UP_CARDS_EVENT_CODE;

                if (!IsValidPickUp())
                    return;

                AddStackedCardsToInfo(handCardIndex, turnInfo);
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
        ClearStackedCards();

        _lockStack = false;
        _buttonsPanel.SetActive(false);

        EventManager.RaisePhotonEvent(eventCode, turnInfo.ToArray());
    }

    private void AddStackedCardsToInfo(int handCardIndex, List<int> turnInfo)
    {
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
    Drop,
    Stack,
    PickUp
}
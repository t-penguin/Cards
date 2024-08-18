using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.ObjectModel;

public class StackedCardsInteraction : CardInteraction
{
    public int OwnerIndex { get; private set; }
    public SingleCardInteraction TargetCard { get; private set; }
    public bool Locked { get; private set; }
    public ReadOnlyCollection<SingleCardInteraction> Cards { get { return _cards.AsReadOnly(); } }
    private List<SingleCardInteraction> _cards;

    [SerializeField] GameObject _infoContainer;
    [SerializeField] GameObject _stackVisualizer;
    [SerializeField] RectTransform _cardIcons;
    [SerializeField] TextMeshProUGUI _ownerText;
    bool _showingVisualizer;

    [SerializeField] GameObject _cardIconPrefab;

    private const string SHOW_TEXT = "Show Cards";
    private const string SMALL_SHOW_TEXT = "Show";
    private const string HIDE_TEXT = "Hide Cards";
    private const string SMALL_HIDE_TEXT = "Hide";

    #region Table Size Values

    // Header
    [SerializeField] RectTransform _header;
    private Vector2 _defaultHeaderSize = new Vector2(125, 25);
    private Vector2 _smallHeaderSize = new Vector2(100, 20);
    private Vector2 _teenyHeaderSize = new Vector2(75, 15);
    private Vector2 _defaultHeaderPos = new Vector2(0, -20);
    private Vector2 _teenyHeaderPos = new Vector2(0, -15);
    [SerializeField] TextMeshProUGUI _headerText;
    private int _defaultHeaderFontSize = 25;
    private int _smallHeaderFontSize = 22;
    private int _teenyHeaderFontSize = 17;

    // Lock
    [SerializeField] RectTransform _lock;
    private Vector2 _defaultLockSize = new Vector2(25, 25);
    private Vector2 _smallLockSize = new Vector2(22, 22);
    private Vector2 _teenyLockSize = new Vector2(18, 18);
    private Vector2 _defaultLockPos = new Vector2(-5, -50);
    private Vector2 _teenyLockPos = new Vector2(-3, -35);

    // Value
    [SerializeField] RectTransform _value;
    private Vector2 _defaultValueSize = new Vector2(125, 50);
    private Vector2 _smallValueSize = new Vector2(100, 40);
    private Vector2 _teenyValueSize = new Vector2(75, 25);
    private Vector2 _defaultValuePos = new Vector2(0, -60);
    private Vector2 _teenyValuePos = new Vector2(0, -45);
    [SerializeField] TextMeshProUGUI _valueText;
    private int _defaultValueFontSize = 50;
    private int _smallValueFontSize = 40;
    private int _teenyValueFontSize = 30;

    // Show/Hide Button
    [SerializeField] RectTransform _button;
    private Vector2 _defaultButtonSize = new Vector2(100, 60);
    private Vector2 _smallButtonSize = new Vector2(75, 30);
    private Vector2 _teenyButtonSize = new Vector2(60, 20);
    [SerializeField] TextMeshProUGUI _showHideText;
    private int _defaultButtonFontSize = 22;
    private int _smallButtonFontSize = 20;
    private int _teenyButtonFontSize = 15;

    #endregion

    private void Awake()
    {
        Selected = false;
        InteractionType = InteractionType.Stack;
    }

    public override void Hide()
    {
        _infoContainer.SetActive(false);
        base.Hide();
    }

    public void CreateStack(int ownderIndex, int value, bool locked, List<CardInteraction> cards,
                            SingleCardInteraction targetCard)
    {
        OwnerIndex = ownderIndex;
        TargetCard = targetCard;
        Value = value;
        Locked = locked;
        _lock.gameObject.SetActive(locked);

        _cards = new List<SingleCardInteraction>();
        foreach (CardInteraction card in cards)
        {
            if (card.InteractionType == InteractionType.Stack)
            {
                StackedCardsInteraction stack = (StackedCardsInteraction)card;
                TurnManager.ClearStack(stack);
                _cards.AddRange(stack._cards);
            }
            else
                _cards.Add((SingleCardInteraction)card);
        }
        _valueText.text = $"{Value}";

        PhotonPlayer[] TurnOrder =
            (PhotonPlayer[])PhotonNetwork.room.CustomProperties[GameManager.TURN_ORDER_KEY];
        _ownerText.text = TurnOrder[ownderIndex].NickName;
        ResizeVisualizer();
        ResizeCard();
        SetCardIcons();
    }

    public void SetValue(int value) => Value = value;
    public void SetLocked(bool locked) => Locked = locked;

    public void ShowStack() => _stackVisualizer.SetActive(true);
    public void HideStack() => _stackVisualizer.SetActive(false);

    public void ToggleVisualizer()
    {
        _showingVisualizer = !_showingVisualizer;

        _stackVisualizer.SetActive(_showingVisualizer);

        TableSize tableSize = TableVisual.TableSize;
        if (tableSize == TableSize.Default || tableSize == TableSize.Medium)
            _showHideText.text = _showingVisualizer ? HIDE_TEXT : SHOW_TEXT;
        else
            _showHideText.text = _showingVisualizer ? SMALL_HIDE_TEXT : SMALL_SHOW_TEXT;
    }

    public void ResizeVisualizer()
    {
        GridLayoutGroup gridLayout = _cardIcons.GetComponent<GridLayoutGroup>();
        int columnSize = (int)gridLayout.cellSize.x;
        int rowSize = (int)(gridLayout.cellSize.y);

        int numCards = _cards.Count;
        int columns = numCards > 24 ? 8 : 4;

        int rows = Mathf.CeilToInt((float)numCards / columns);

        RectTransform rect = _stackVisualizer.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2
        {
            x = (columns * columnSize) + ((columns - 1) * 7) + 20,
            y = (rows * rowSize) + ((rows - 1) * 7) + 55
        };
    }

    public void SetCardIcons()
    {
        foreach (SingleCardInteraction card in _cards)
        {
            GameObject cardIcon = Instantiate(_cardIconPrefab, _cardIcons.transform);
            cardIcon.GetComponent<Image>().sprite = GameManager.GetCardIconByName(card.Card.Name);
        }
    }

    public void ResizeCard()
    {
        switch (TableVisual.TableSize)
        {
            default:
                _header.sizeDelta = _defaultHeaderSize;
                _lock.sizeDelta = _defaultLockSize;
                _value.sizeDelta = _defaultValueSize;
                _button.sizeDelta = _defaultButtonSize;

                _showHideText.text = _showingVisualizer ? HIDE_TEXT : SHOW_TEXT;

                _headerText.fontSize = _defaultHeaderFontSize;
                _valueText.fontSize = _defaultValueFontSize;
                _showHideText.fontSize = _defaultButtonFontSize;

                _header.anchoredPosition = _defaultHeaderPos;
                _value.anchoredPosition = _defaultValuePos;
                _lock.anchoredPosition = _defaultLockPos;
                break;
            case TableSize.Large:
                _header.sizeDelta = _smallHeaderSize;
                _lock.sizeDelta = _smallLockSize;
                _value.sizeDelta = _smallValueSize;
                _button.sizeDelta = _smallButtonSize;

                _showHideText.text = _showingVisualizer ? SMALL_HIDE_TEXT : SMALL_SHOW_TEXT;

                _headerText.fontSize = _smallHeaderFontSize;
                _valueText.fontSize = _smallValueFontSize;
                _showHideText.fontSize = _smallButtonFontSize;

                _header.anchoredPosition = _defaultHeaderPos;
                _value.anchoredPosition = _defaultValuePos;
                _lock.anchoredPosition = _defaultLockPos;
                break;
            case TableSize.ExtraLarge:
                _header.sizeDelta = _teenyHeaderSize;
                _lock.sizeDelta = _teenyLockSize;
                _value.sizeDelta = _teenyValueSize;
                _button.sizeDelta = _teenyButtonSize;

                _showHideText.text = _showingVisualizer ? SMALL_HIDE_TEXT : SMALL_SHOW_TEXT;

                _headerText.fontSize = _teenyHeaderFontSize;
                _valueText.fontSize = _teenyValueFontSize;
                _showHideText.fontSize = _teenyButtonFontSize;

                _header.anchoredPosition = _teenyHeaderPos;
                _value.anchoredPosition = _teenyValuePos;
                _lock.anchoredPosition = _teenyLockPos;
                break;
        }
    }
}
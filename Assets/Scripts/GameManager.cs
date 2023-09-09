using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public const int MIN_PLAYERS = 2;
    public const int MAX_PLAYERS = 4;

    public static event Action<int> HandDealt;

    [field: SerializeField] public List<Player> Players { get; private set; }
    [field: SerializeField] public Deck Deck { get; private set; }
    [field: SerializeField] public DeckType DeckType { get; private set; }
    public Dictionary<string, Sprite> CardSprites;

    private int _dealerIndex;
    private int _deckIndex;

    #region Monobehaviour Callbacks

    private void Awake()
    {
        LoadCardSprites();
    }

    private void Start()
    {
        Deck.CreateDeck(DeckType.Star_Blue);
    }

    #endregion

    public void Deal()
    {
        // Deal to table on first turn
        if(_deckIndex == 0)
        {
            _deckIndex += 4;
        }

        Deck.Deal(Players, _dealerIndex + 1, _deckIndex);
        _deckIndex += Players.Count * 4;
        
        HandDealt?.Invoke(_deckIndex);
    }

    private void LoadCardSprites()
    {
        string deckName = DeckType.ToString().Split("_")[0];
        Sprite[] sprites = Resources.LoadAll<Sprite>($"Textures/{deckName} Cards");
        CardSprites = new Dictionary<string, Sprite>();

        for(int i = 0; i < sprites.Length; i++)
        {
            string name = sprites[i].name;
            name = name.Replace($"{deckName}_", "");
            CardSprites.Add(name, sprites[i]);
        }
    }

    public Sprite GetCardSpriteByName(string name)
    {
        if (CardSprites.ContainsKey(name))
            return CardSprites[name];
        else
            return null;
    }
}
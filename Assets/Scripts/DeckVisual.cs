using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Deck))]
public class DeckVisual : MonoBehaviour
{
    [SerializeField] GameManager gameManager;

    [field: SerializeField] public DeckType DeckType { get; private set; }
    [SerializeField] List<Image> DeckBackVisual;
    

    private int numPlayers;

    #region Monobehaviour Callbacks

    private void OnEnable()
    {
        Deck.DeckCreated += OnDeckCreated;
        GameManager.HandDealt += OnHandDealt;
    }

    private void OnDisable()
    {
        Deck.DeckCreated -= OnDeckCreated;
        GameManager.HandDealt -= OnHandDealt;
    }

    // Start is called before the first frame update
    void Start()
    {
        if(DeckBackVisual.Count == 0)
        {
            Debug.LogWarning("Deck Visual not set up! Setting it up now...");
            GetComponentsInChildren(true, DeckBackVisual);
        }

        if(DeckBackVisual.Count != 6)
            Debug.LogWarning($"Extra or missing deck images. Count: {DeckBackVisual.Count}");

        if(gameManager == null)
        {
            Debug.LogWarning("Game Manager not set! Setting number of players to 0...");
            numPlayers = 0;
        }

        numPlayers = gameManager.Players.Count;
    }

    #endregion

    #region Event Callbacks

    void OnDeckCreated(DeckType type)
    {
        DeckType = type;
        int handsRemaining = numPlayers == 0 ? 6 : 52 % (numPlayers * 4);
        ModifyDeckVisual(handsRemaining);
    }

    void OnHandDealt(int deckOffset)
    {
        int handsRemaining = numPlayers == 0 ? 6 : (52 - deckOffset) / (numPlayers * 4);
        Debug.Log($"Hand dealt. Remaining: {handsRemaining}\nCards Left: {52 - deckOffset} / Cards per Hand: {numPlayers * 4}");
        ModifyDeckVisual(handsRemaining);
    }

    #endregion

    private void ModifyDeckVisual(int handsRemaining)
    {
        for (int i = 0; i < 6; i++)
            DeckBackVisual[i].gameObject.SetActive(i < handsRemaining);
    }
}
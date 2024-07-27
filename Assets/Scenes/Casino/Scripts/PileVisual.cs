using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PickupPile))]
public class PileVisual : MonoBehaviour
{
    [SerializeField] List<GameObject> _pileBackVisuals;

    private PickupPile _pile;
    private int _pileCount;
    private int _threshold;
    private int _pileID;

    private const int INCREMENT = 5;

    private void Awake()
    {
        if (_pileBackVisuals.Count == 0)
        {
            Debug.LogWarning("Deck Visual not set up! Setting it up now...");
            GetComponentsInChildren(true, _pileBackVisuals);
        }

        if (_pileBackVisuals.Count != 6)
            Debug.LogWarning($"Invalid number of visuals in deck! Expected: 6, Actual: {_pileBackVisuals.Count}");

        foreach(GameObject visual in _pileBackVisuals)
            visual.SetActive(false);

        _pileCount = 0;
        _threshold = 0;
        _pile = GetComponent<PickupPile>();
        _pileID = _pile.PileID;
    }

    private void OnEnable()
    {
        PickupPile.AddedCard += OnAddedToPile;
    }

    private void OnDisable()
    {
        PickupPile.AddedCard -= OnAddedToPile;
    }

    private void OnAddedToPile(int pileID)
    {
        if (pileID != _pileID || ++_pileCount > 26 || _pileCount <= _threshold)
            return;

        _threshold += INCREMENT;
        int index = _threshold / INCREMENT - 1;
        _pileBackVisuals[index].gameObject.SetActive(true);
    }

    public void ResetPile()
    {
        _pileCount = 0;
        _threshold = 0;
        foreach(GameObject visual in _pileBackVisuals)
            visual.SetActive(false);

        _pile.ResetPile();
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [field: SerializeField] public string Name { get; private set; }
    [field: SerializeField] public List<Card> Hand { get; private set; }


    private void Start()
    {
        Hand = new List<Card>();
    }

    public void ReceiveCard(Card card)
    {
        if(Hand.Count > 3)
        {
            Debug.LogError($"{Name} cannot receive any more cards!");
            return;
        }

        Hand.Add(card);
    }
}
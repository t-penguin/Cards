using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static event Action<Player, Card> ReceivedCard;

    [field: SerializeField] public string Name { get; private set; }
    [field: SerializeField] public int ID { get; set; }
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
        ReceivedCard?.Invoke(this, card);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EventManager
{
    public const byte TURN_ORDER_SET_EVENT_CODE = 1;
    public const byte TABLE_SET_EVENT_CODE = 2;
    public const byte GAME_SET_STARTED = 3;
    public const byte GAME_SET_ADVANCED = 4;
    public const byte GAME_SET_ENDED = 5;
    public const byte TURN_STARTED_EVENT_CODE = 6;
    public const byte TURN_ENDED_EVENT_CODE = 7;
    public const byte TURN_DROP_CARD_EVENT_CODE = 8;
    public const byte TURN_STACK_CARDS_EVENT_CODE = 9;

    public static RaiseEventOptions DefaultEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };

    public static void RaisePhotonEvent(byte eventCode, object eventContent = null, bool sendReliable = true)
    {
        PhotonNetwork.RaiseEvent(eventCode, eventContent, sendReliable, DefaultEventOptions);
    }
}
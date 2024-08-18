public static class EventManager
{
    public const byte SET_TABLE_EVENT_CODE = 1;
    public const byte START_GAME_EVENT_CODE = 2;
    public const byte TABLE_CLEAR_EVENT_CODE = 3;
    public const byte GAME_DEAL_HAND_EVENT_CODE = 4;
    public const byte GAME_START_SET_EVENT_CODE = 5; // Not currently used
    public const byte GAME_SET_STARTED_EVENT_CODE = 6; // No current listeners
    public const byte GAME_ADVANCE_SET_EVENT_CODE = 7; // Not currently used
    public const byte GAME_SET_ADVANCED_EVENT_CODE = 8; // No current listeners
    public const byte GAME_END_SET_EVENT_CODE = 9;
    public const byte GAME_SET_ENDED_EVENT_CODE = 10;
    public const byte GAME_HAND_DEALT_EVENT_CODE = 11;
    public const byte GAME_START_TURN_EVENT_CODE = 12;
    public const byte TURN_DROP_CARD_EVENT_CODE = 13;
    public const byte TURN_STACK_CARDS_EVENT_CODE = 14;
    public const byte TURN_PICK_UP_CARDS_EVENT_CODE = 15;
    public const byte GAME_END_TURN_EVENT_CODE = 16;

    public static RaiseEventOptions DefaultEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };

    public static void RaisePhotonEvent(byte eventCode, bool onlySendIfMaster = true,
        object eventContent = null, bool sendReliable = true)
    {
        if(onlySendIfMaster && !PhotonNetwork.isMasterClient)
            return;

        PhotonNetwork.RaiseEvent(eventCode, eventContent, sendReliable, DefaultEventOptions);
    }
}
using Fantasy;
using UnityEngine;

namespace BestGame;

public struct StartMove
{
    public Unit Unit;
    public MoveInfo MoveInfo;
    public long MoveEndTime;
    public NoticeClientType Notice;
}

public struct StopMove
{
    public Unit Unit;
    public MoveInfo MoveInfo;
    public NoticeClientType NoticeClientType;
}


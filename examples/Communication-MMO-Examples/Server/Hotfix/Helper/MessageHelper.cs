namespace Fantasy;
public enum NoticeClientType
{
    NoNotice = 0,
    Self = 1,
    Aoi = 2,
}

public static class HotfixMessageHelper
{
    /// <summary>
    /// 广播给客户端网络协议
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="noticeClientType"></param>
    /// <param name="message"></param>
    public static void NoticeClient(Unit unit, NoticeClientType noticeClientType, IRouteMessage message)
    {
        if (unit == null)
        {
            return;
        }
        
        var scene = unit.Scene;
        
        switch (noticeClientType)
        {
            case NoticeClientType.Self:
            {
                MessageHelper.SendInnerRoute(scene, unit.GateRouteId, message);
                break;
            }
            case NoticeClientType.Aoi:
            {
                var aoi = unit.GetComponent<AOIEntity>();

                if (aoi == null)
                {
                    return;
                }

                var beSeePlayers = aoi.BeSeePlayers;

                foreach (var (_, aoiEntity) in beSeePlayers)
                {
                    MessageHelper.SendInnerRoute(scene, aoiEntity.Unit.GateRouteId, message);
                }

                break;
            }
        }
    }
}
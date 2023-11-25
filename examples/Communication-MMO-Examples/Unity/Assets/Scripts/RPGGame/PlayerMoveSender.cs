using Fantasy;
using UnityEngine;
using BestGame;

public class PlayerMoveSender : MonoBehaviour
{
    private long _timerId;

    void Start()
    {
        // 100毫秒发送一次，可设置为15-100之间，减少服务器处理数据密度
        // 可以增加逻辑，当角色完全不动时，停止发送
        _timerId = TimerScheduler.Instance.Unity.RepeatedTimer(100, () => RepeatedSend().Coroutine());
    }

    private async FTask RepeatedSend()
    {
        // 发送位置信息
        await GameManager.sender.Send(new C2M_MoveMessage{
            MoveInfo = MessageInfoHelper.MoveInfo(transform.position,transform.rotation)
        });
    }

    void OnDestroy()
    {
        if (_timerId == 0) return; 
        
        TimerScheduler.Instance?.Unity.RemoveByRef(ref _timerId);
    }
}

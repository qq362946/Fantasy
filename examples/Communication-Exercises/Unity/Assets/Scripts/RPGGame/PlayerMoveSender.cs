using Fantasy;
using UnityEngine;

public class PlayerMoveSender : MonoBehaviour
{
    private ISend sender;

    private long _timerId;

    void Start()
    {
        sender = GameObject.Find("Network").GetComponent<ISend>();

        // 100毫秒发送一次，可设置为15-100之间，减少服务器处理数据密度
        // 可以增加逻辑，当角色完全不动时，停止发送
        _timerId = TimerScheduler.Instance.Unity.RepeatedTimer(100, () => RepeatedSend().Coroutine());
    }

    private async FTask RepeatedSend()
    {
        sender.MoveSend(transform.position,transform.rotation);

        await FTask.CompletedTask;
    }

    void OnDestroy()
    {
        if (_timerId == 0) return; 
        
        TimerScheduler.Instance?.Unity.RemoveByRef(ref _timerId);
    }
}

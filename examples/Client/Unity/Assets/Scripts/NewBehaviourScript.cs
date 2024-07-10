using System.Collections;
using System.Collections.Generic;
using Fantasy;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartAsync().Coroutine();
    }
    
    async FTask StartAsync()
    {
        var scene = await Fantasy.Entry.Initialize(GetType().Assembly);
        var session = scene.Connect("127.0.0.1:20000", NetworkProtocolType.KCP, () =>
        {
            Log.Debug("连接成功");
        }, () =>
        {
            Log.Debug("连接失败");
        }, () =>
        {
            Log.Debug("连接断开");
        });

        var response = (A2C_TestResponse)await session.Call(new C2A_TestRequest());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

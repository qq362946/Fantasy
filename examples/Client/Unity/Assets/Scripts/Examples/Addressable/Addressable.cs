using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Addressable : MonoBehaviour
{
    public Text Text;
    public Button ConnectAddressable;
    public Button SendAddressableMessage;
    public Button SendAddressableRPC;
    private void Start()
    {
        ConnectAddressable.onClick.RemoveAllListeners();
        ConnectAddressable.onClick.AddListener(OnConnectAddressableClick);
    }

    private void OnConnectAddressableClick()
    {
        throw new System.NotImplementedException();
    }
}

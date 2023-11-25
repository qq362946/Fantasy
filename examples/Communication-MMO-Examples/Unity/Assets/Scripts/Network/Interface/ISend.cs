using UnityEngine;
using System;
using System.Collections.Generic;
using Fantasy;
public interface ISend
{
    FTask<IResponse> Login(IRequest request);
    FTask<IResponse> Register(IRequest request);
    FTask Send(IMessage message);
    FTask Send(IAddressableRouteMessage message);
    FTask<IResponse> Call(IRequest request);
    FTask<IAddressableRouteResponse> Call(IAddressableRouteRequest request);
}
using System;

namespace Fantasy.Network
{
    /// <summary>
    /// 
    /// </summary>
    public static class InnerNetworkProtocolRegistrar
    {
        /// <summary>
        /// 框架内部OpCode总数
        /// </summary>
#if FANTASY_NET
        public const int OpCodeCount = 37;
#endif
#if FANTASY_UNITY
        public const int OpCodeCount = 17;
#endif
        /// <summary>
        /// 框架内部CustomRouteType总数
        /// </summary>
        public const int CustomRouteTypeCount = 0;

        /// <summary>
        /// 框架内部Request总数
        /// </summary>
#if FANTASY_NET
        public const int GetRequestCount = 17;
#endif
#if FANTASY_UNITY
        public const int GetRequestCount = 7;
#endif
        /// <summary>
        /// 添加框架内部OpCode到数组中
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="values"></param>
        /// <param name="offset"></param>
        public static void FillOpCode(uint[] keys, Type[] values, int offset)
        {
            keys[offset] = 142606335;
            values[offset] = typeof(InnerMessage.BenchmarkMessage);
            keys[offset + 1] = 276824063;
            values[offset + 1] = typeof(InnerMessage.BenchmarkRequest);
            keys[offset + 2] = 411041791;
            values[offset + 2] = typeof(InnerMessage.BenchmarkResponse);
            keys[offset + 3] = 805306369;
            values[offset + 3] = typeof(InnerMessage.Response);
            keys[offset + 4] = 1207959559;
            values[offset + 4] = typeof(InnerMessage.RouteResponse);
            keys[offset + 5] = 4026531841;
            values[offset + 5] = typeof(InnerMessage.PingRequest);
            keys[offset + 6] = 4160749569;
            values[offset + 6] = typeof(InnerMessage.PingResponse);
            keys[offset + 7] = 1073741825;
            values[offset + 7] = typeof(InnerMessage.I_AddressableAdd_Request);
            keys[offset + 8] = 1207959553;
            values[offset + 8] = typeof(InnerMessage.I_AddressableAdd_Response);
            keys[offset + 9] = 1073741826;
            values[offset + 9] = typeof(InnerMessage.I_AddressableGet_Request);
            keys[offset + 10] = 1207959554;
            values[offset + 10] = typeof(InnerMessage.I_AddressableGet_Response);
            keys[offset + 11] = 1073741827;
            values[offset + 11] = typeof(InnerMessage.I_AddressableRemove_Request);
            keys[offset + 12] = 1207959555;
            values[offset + 12] = typeof(InnerMessage.I_AddressableRemove_Response);
            keys[offset + 13] = 1073741828;
            values[offset + 13] = typeof(InnerMessage.I_AddressableLock_Request);
            keys[offset + 14] = 1207959556;
            values[offset + 14] = typeof(InnerMessage.I_AddressableLock_Response);
            keys[offset + 15] = 1073741829;
            values[offset + 15] = typeof(InnerMessage.I_AddressableUnLock_Request);
            keys[offset + 16] = 1207959557;
            values[offset + 16] = typeof(InnerMessage.I_AddressableUnLock_Response);
#if FANTASY_NET
        keys[offset + 17] = 1073741830;
        values[offset + 17] = typeof(InnerMessage.I_LinkRoamingRequest);
        keys[offset + 18] = 1207959558;
        values[offset + 18] = typeof(InnerMessage.I_LinkRoamingResponse);
        keys[offset + 19] = 1073741832;
        values[offset + 19] = typeof(InnerMessage.I_UnLinkRoamingRequest);
        keys[offset + 20] = 1207959560;
        values[offset + 20] = typeof(InnerMessage.I_UnLinkRoamingResponse);
        keys[offset + 21] = 1073741833;
        values[offset + 21] = typeof(InnerMessage.I_LockTerminusIdRequest);
        keys[offset + 22] = 1207959561;
        values[offset + 22] = typeof(InnerMessage.I_LockTerminusIdResponse);
        keys[offset + 23] = 1073741834;
        values[offset + 23] = typeof(InnerMessage.I_UnLockTerminusIdRequest);
        keys[offset + 24] = 1207959562;
        values[offset + 24] = typeof(InnerMessage.I_UnLockTerminusIdResponse);
        keys[offset + 25] = 1082130433;
        values[offset + 25] = typeof(InnerMessage.I_TransferTerminusRequest);
        keys[offset + 26] = 1216348161;
        values[offset + 26] = typeof(InnerMessage.I_TransferTerminusResponse);
        keys[offset + 27] = 1073741835;
        values[offset + 27] = typeof(InnerMessage.I_GetTerminusIdRequest);
        keys[offset + 28] = 1207959563;
        values[offset + 28] = typeof(InnerMessage.I_GetTerminusIdResponse);
        keys[offset + 29] = 1082130434;
        values[offset + 29] = typeof(InnerMessage.I_SubscribeSphereEventRequest);
        keys[offset + 30] = 1216348162;
        values[offset + 30] = typeof(InnerMessage.I_SubscribeSphereEventResponse);
        keys[offset + 31] = 1082130435;
        values[offset + 31] = typeof(InnerMessage.I_UnsubscribeSphereEventRequest);
        keys[offset + 32] = 1216348163;
        values[offset + 32] = typeof(InnerMessage.I_UnsubscribeSphereEventResponse);
        keys[offset + 33] = 1082130436;
        values[offset + 33] = typeof(InnerMessage.I_RevokeRemoteSubscriberRequest);
        keys[offset + 34] = 1216348164;
        values[offset + 34] = typeof(InnerMessage.I_RevokeRemoteSubscriberResponse);
        keys[offset + 35] = 1082130437;
        values[offset + 35] = typeof(InnerMessage.I_PublishSphereEventRequest);
        keys[offset + 36] = 1216348165;
        values[offset + 36] = typeof(InnerMessage.I_PublishSphereEventResponse);
#endif
        }

        /// <summary>
        /// 添加框架内部CustomRouteType到数组中
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="values"></param>
        /// <param name="offset"></param>
        public static void FillCustomRouteType(uint[] keys, int[] values, int offset)
        {

        }

        /// <summary>
        /// 添加框架内部ResponseType到数组中
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="values"></param>
        /// <param name="offset"></param>
        public static void FillResponseType(uint[] keys, Type[] values, int offset)
        {
            keys[offset] = 276824063;
            values[offset] = typeof(InnerMessage.BenchmarkResponse);
            keys[offset + 1] = 4026531841;
            values[offset + 1] = typeof(InnerMessage.PingResponse);
            keys[offset + 2] = 1073741825;
            values[offset + 2] = typeof(InnerMessage.I_AddressableAdd_Response);
            keys[offset + 3] = 1073741826;
            values[offset + 3] = typeof(InnerMessage.I_AddressableGet_Response);
            keys[offset + 4] = 1073741827;
            values[offset + 4] = typeof(InnerMessage.I_AddressableRemove_Response);
            keys[offset + 5] = 1073741828;
            values[offset + 5] = typeof(InnerMessage.I_AddressableLock_Response);
            keys[offset + 6] = 1073741829;
            values[offset + 6] = typeof(InnerMessage.I_AddressableUnLock_Response);
#if FANTASY_NET
            keys[offset + 7] = 1073741830;
            values[offset + 7] = typeof(InnerMessage.I_LinkRoamingResponse);
            keys[offset + 8] = 1073741832;
            values[offset + 8] = typeof(InnerMessage.I_UnLinkRoamingResponse);
            keys[offset + 9] = 1073741833;
            values[offset + 9] = typeof(InnerMessage.I_LockTerminusIdResponse);
            keys[offset + 10] = 1073741834;
            values[offset + 10] = typeof(InnerMessage.I_UnLockTerminusIdResponse);
            keys[offset + 11] = 1082130433;
            values[offset + 11] = typeof(InnerMessage.I_TransferTerminusResponse);
            keys[offset + 12] = 1073741835;
            values[offset + 12] = typeof(InnerMessage.I_GetTerminusIdResponse);
            keys[offset + 13] = 1082130434;
            values[offset + 13] = typeof(InnerMessage.I_SubscribeSphereEventResponse);
            keys[offset + 14] = 1082130435;
            values[offset + 14] = typeof(InnerMessage.I_UnsubscribeSphereEventResponse);
            keys[offset + 15] = 1082130436;
            values[offset + 15] = typeof(InnerMessage.I_RevokeRemoteSubscriberResponse);
            keys[offset + 16] = 1082130437;
            values[offset + 16] = typeof(InnerMessage.I_PublishSphereEventResponse);
#endif
        }
    }
}

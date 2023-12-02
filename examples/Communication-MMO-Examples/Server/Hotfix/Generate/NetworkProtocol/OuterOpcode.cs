namespace Fantasy
{
	public static partial class OuterOpcode
	{
		 public const int C2R_GetZoneList = 110000001;
		 public const int R2C_GetZoneList = 160000001;
		 public const int C2R_RegisterRequest = 110000002;
		 public const int R2C_RegisterResponse = 160000002;
		 public const int C2R_LoginRequest = 110000003;
		 public const int R2C_LoginResponse = 160000003;
		 public const int C2G_LoginGateRequest = 110000004;
		 public const int G2C_LoginGateResponse = 160000004;
		 public const int C2G_CreateRoleRequest = 110000005;
		 public const int G2C_CreateRoleResponse = 160000005;
		 public const int C2G_RoleDeleteRequest = 110000006;
		 public const int G2C_RoleDeleteResponse = 160000006;
		 public const int C2G_RoleListRequest = 110000007;
		 public const int G2C_RoleListResponse = 160000007;
		 public const int C2G_EnterMapRequest = 110000008;
		 public const int G2C_EnterMapResponse = 160000008;
		 public const int G2C_ForceDisconnected = 100000001;
		 public const int C2M_ExitRequest = 200000001;
		 public const int M2C_ExitResponse = 250000001;
		 public const int C2M_StartMoveMessage = 190000001;
		 public const int C2M_StopMoveMessage = 190000002;
		 public const int M2C_MoveBroadcast = 190000003;
		 public const int M2C_UnitCreate = 190000004;
		 public const int M2C_UnitRemove = 190000005;
	}
}

namespace Fantasy
{
	public static partial class InnerOpcode
	{
		 public const int S2S_ConnectRequest = 130001001;
		 public const int S2S_ConnectResponse = 170001001;
		 public const int R2G_GetLoginKeyRequest = 220001001;
		 public const int G2R_GetLoginKeyResponse = 260001001;
		 public const int G2M_CreateUnitRequest = 220001002;
		 public const int M2G_CreateUnitResponse = 260001002;
		 public const int G2M_Return2MapMsg = 210001001;
		 public const int M2G_QuitMapMsg = 210001002;
		 public const int S2Mgr_ServerStartComplete = 210001003;
		 public const int Mgr2R_MachineStartFinished = 210001004;
	}
}

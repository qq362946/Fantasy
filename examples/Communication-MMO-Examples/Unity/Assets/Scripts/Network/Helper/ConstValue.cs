using UnityEngine;

namespace Fantasy
{
	public class ConstValue
	{
		// # 基本参数
		public const string Version = "v20230129"; // 游戏版本号
		public const int MINUTE_MS = 60*1000; // 分钟毫秒值
		public const int HOUR_MS = 60*60*1000; // 小时毫秒值
		public const int DAY_MS = 24*60*60*1000; // 天毫秒值
		// # 网络参数
		public const int ClientHeartbeat = 2000; // 客户端连接服务器的心跳发送频率（毫秒）
		// # 验证服参数
		public const int OutHeatbeatTimeoutInterval = 60000; // 外网连接超时时间 (毫秒)
		// # 角色相关
		public const int CharacterCountLimit = 5; // 可持有角色上限数量
		public const int UnitMaxSpeedAttr = 3000; // 脚力属性最大值
		public const float ModelMaxSpeed = 7.5f; // 模型移动速度最大值
		// #目标选择
		public const int FilterUnitMax = 8; // 筛选目标的最大数量
		public const int FilterMaxDistance = 10; // 筛选目标的最大距离
		// #注册账户
		public static readonly int[] Login_Account = new int[] {2,16}; // 账户最小位数，最大位数
		public static readonly int[] Login_Password = new int[] {6,16}; // 密码最小位数，最大位数
		public const int Login_Untie_Cycle = 0; // 账户解绑周期0=无线；若有时间，填写毫秒单位
		public const int Login_Untie = 1; // 账户解绑手机号次数
		// #目标锁定
		public const int TargetLockedMonsterAutoTimes = 5*1000; // 锁定怪物列表自动更新时间（毫秒）
		public const int TargetLockedPlayerAutoTimes = 5*1000; // 锁定玩家列表自动更新时间（毫秒）
		// #摄像机远近视角的参数
		public const int CameraDistance = 89000; // 摄像机距离默认参数（小数，除以10000后使用）
		public const int CameraHeight = 107300; // 摄像机高度默认参数（小数，除以10000后使用）
		public const int CameraRotation = 2076600; // 摄像机角度默认参数（小数，除以10000后使用）

		// # AOI相关
		public const int CellUnitLen = 10000; // 格子单位长度
		public const int CellSize = 10*CellUnitLen; // 单位格子视野(米)
		public const int AoiSeeUnitsCount = 20; // 可视的玩家数量
	}
}




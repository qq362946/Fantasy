public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	// Fantasy.Core.dll
	// mscorlib.dll
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// Fantasy.AsyncEventSystem<Fantasy.OnAppStart>
	// Fantasy.AsyncEventSystem<Fantasy.OnCreateScene>
	// Fantasy.EventSystem<Fantasy.OnAppClosed>
	// Fantasy.FTask<object>
	// Fantasy.Helper.Singleton<object>
	// System.Action<object>
	// System.Collections.Generic.Dictionary<int,object>
	// System.Collections.Generic.List<object>
	// }}

	public void RefMethods()
	{
		// System.Void Fantasy.AsyncFTaskMethodBuilder.AwaitOnCompleted<Fantasy.FTaskCompleted,Fantasy.Hotfix.Core.OnCreateScene_Configuration.<Handler>d__0>(Fantasy.FTaskCompleted&,Fantasy.Hotfix.Core.OnCreateScene_Configuration.<Handler>d__0&)
		// System.Void Fantasy.AsyncFTaskMethodBuilder.AwaitUnsafeOnCompleted<object,Fantasy.Hotfix.OnAppStart_Init.<Handler>d__0>(object&,Fantasy.Hotfix.OnAppStart_Init.<Handler>d__0&)
		// System.Void Fantasy.AsyncFTaskMethodBuilder.Start<Fantasy.Hotfix.OnAppStart_Init.<Handler>d__0>(Fantasy.Hotfix.OnAppStart_Init.<Handler>d__0&)
		// System.Void Fantasy.AsyncFTaskMethodBuilder.Start<Fantasy.Hotfix.Core.OnCreateScene_Configuration.<Handler>d__0>(Fantasy.Hotfix.Core.OnCreateScene_Configuration.<Handler>d__0&)
		// object Fantasy.Core.ConfigTableManage.Load<object>()
		// object Fantasy.Entity.AddComponent<object>()
		// System.Void Fantasy.EventSystem.Publish<Fantasy.OnConnectFail>(Fantasy.OnConnectFail)
	}
}
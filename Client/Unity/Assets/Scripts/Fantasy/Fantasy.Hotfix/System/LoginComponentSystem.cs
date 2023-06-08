using Fantasy.Core;
using Fantasy.Model;

namespace Fantasy.Hotfix.System
{
    public class LoginComponentAwakeSystem : AwakeSystem<LoginComponent>
    {
        protected override void Awake(LoginComponent loginComponent)
        {
            var loginComponentScene = loginComponent.Scene;
            loginComponent.PlayButton.onClick.Set(() =>
            {
                Log.Debug("PlayButton");
                Call(loginComponentScene).Coroutine();
                loginComponent.Dispose();
                var mainComponent = UIComponent.Create<MainComponent>(loginComponentScene);
            });
        }

        private async FTask Call(Scene scene)
        {
            var response = (H_A2C_RegisterAccount)await scene.Session.Call(new H_C2A_RegisterAccount()
            {
                Account = "111111"
            });

            if (response.ErrorCode != 0)
            {
                Log.Error($"response.ErrorCode:{response.ErrorCode}");
            }
            
            Log.Debug(response.Account);
        }
    }
    
    public class LoginComponentUpdateSystem : UpdateSystem<LoginComponent>
    {
        protected override void Update(LoginComponent self)
        {
            Log.Debug("Update LoginComponent");
        }
    }

    public class LoginComponentDestroySystem : DestroySystem<LoginComponent>
    {
        protected override void Destroy(LoginComponent self)
        {
            Log.Debug("LoginComponent Destroy");
            // throw new NotImplementedException();
        }
    }

    public static class LoginComponentSystem
    {
        
    }
}
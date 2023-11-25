using UnityEngine;
using UnityEngine.UI;
using Fantasy;

/// <summary>
/// 注册面板
/// </summary>
public class RegisterPanel : BasePanel
{
    public InputField account;
    public InputField password;
    public InputField rePass;
    public Text prompt;
    public Button btn_submit;
    public Button btn_back;
    
    protected override void Awake()
    {
        base.Awake();
        backToPanel = StringManager.LoginPanel;
    }

    void Start(){
        btn_submit.onClick.SetListener(async () => {
            // 前端验证
            if(account.text==""||password.text==""){
                prompt.text = StringManager.AccNull;
                return;
            }
            if(rePass.text==""||password.text!=rePass.text){
                prompt.text = StringManager.RePassNull;
                return;
            }

            // >>process account 网络层请求注册
            await Register(account.text,password.text,rePass.text);
        });

        btn_back.onClick.SetListener(() => {
            // >>process account 从注册返回登录界面
            base.BackToPanel();
        });
    }

    public async FTask Register(string name,string password,string rePass){
        // >>process account 网络层请求注册
        var register = (R2C_RegisterResponse) await GameManager.sender.Register(new C2R_RegisterRequest(){
            AuthName = name,Pw = password,Pw2 = rePass, 
            ZoneId = (uint)GameManager.Ins.selectZone.ZoneId, Version = ConstValue.Version
        });
        Log.Info(register.ErrorCode.ToJson());

        // 注册结果
        GameManager.response.RegisterResponse(register.ErrorCode);
    }

    public override void ResetPanel(){
        // 重围输入区内容
        account.text = "";
        password.text = "";
        rePass.text = "";
        prompt.text = "";
    }

}
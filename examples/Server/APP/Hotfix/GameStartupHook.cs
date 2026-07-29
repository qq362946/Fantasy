namespace Fantasy;

public class GameStartupHook : IEntryInitializeHook
{
    public async Task OnInitialize()
    {
        if (ProgramDefine.ProcessType == "Game")
        {
            Log.Info("执行游戏启动前检查...");
            // 你的自定义逻辑
            await CheckDatabaseConnection();
            await ValidateConfiguration();
        }
    }
    
    private Task CheckDatabaseConnection()
    {
        // 检查数据库连接等
        return Task.CompletedTask;
    }

    private Task ValidateConfiguration()
    {
        // 检查验证配置文件等
        return Task.CompletedTask;
    }
}
using Fantasy;

namespace BestGame;

/// 记录区服服务器起服完成数
public class ServerMgr : Entity
{
    public int Number;


    // Mgr服不用重启，关服或维护时调用Reset
    public void Reset(){
        Number = 0;
    }

}

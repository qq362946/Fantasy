using Fantasy;

namespace Hotfix;

public class TestServerMessageHandler : Message<TestServerMessage>
{
    protected override async FTask Run(Session session, TestServerMessage message)
    {
        // Log.Debug("TestServerMessage");
        await FTask.CompletedTask;
    }
}
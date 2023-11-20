using Fantasy;

namespace BestGame;

public class GateKey : Entity
{
    public string AuthName;
    public long AccountId;
}
public class SessionKeyComponent : Entity
{
	private readonly Dictionary<long, GateKey> dic = new Dictionary<long, GateKey>();
	
	public void Add(long key, GateKey gateKey)
	{
		this.dic.Add(key, gateKey);
		this.TimeoutRemoveKey(key);
	}

	public GateKey Get(long key)
	{
		GateKey gateKey;
		this.dic.TryGetValue(key, out gateKey);
		return gateKey;
	}

	public void Remove(long key)
	{
		this.dic.Remove(key);
	}

	private async void TimeoutRemoveKey(long key)
	{
		await TimerScheduler.Instance.Core.WaitAsync(20000);
		this.dic.Remove(key);
	}
}

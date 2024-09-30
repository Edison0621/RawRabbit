using System.Threading;

namespace RawRabbit.Enrichers.MessageContext.Dependencies;

public interface IMessageContextRepository
{
	object Get();
	void Set(object context);
}

public class MessageContextRepository : IMessageContextRepository
{

	private readonly AsyncLocal<object> _msgContext;

	public MessageContextRepository()
	{
		this._msgContext = new AsyncLocal<object>();
	}
	public object Get()
	{
		return this._msgContext?.Value;
	}

	public void Set(object context)
	{
		this._msgContext.Value = context;
	}
}
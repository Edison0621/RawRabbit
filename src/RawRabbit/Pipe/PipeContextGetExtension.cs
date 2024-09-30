namespace RawRabbit.Pipe;

public static class PipeContextGetExtension
{
	public static TType Get<TType>(this IPipeContext context, string key, TType fallback = default)
	{
		if (context?.Properties == null)
		{
			return fallback;
		}

		if (context.Properties.TryGetValue(key, out object result))
		{
			return result is TType type ? type : fallback;
		}
		return fallback;
	}
}

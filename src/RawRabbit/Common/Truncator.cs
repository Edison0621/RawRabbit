namespace RawRabbit.Common;

public class Truncation
{
	public static void Truncate(ref string name)
	{
		if (name.Length > 254)
		{
			name = string.Concat("...", name[^250..]);
		}
	}
}

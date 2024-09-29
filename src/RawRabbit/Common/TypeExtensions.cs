using System;
using System.Reflection;

namespace RawRabbit.Common
{
	public static class TypeExtensions
	{
		public static string GetUserFriendlyName(this Type type)
		{
			string name = $"{type.Namespace}.{type.Name}";
			if (type.GenericTypeArguments.Length > 0)
			{
				bool shouldInsertComma = false;
				name += '[';
				foreach (Type genericType in type.GenericTypeArguments)
				{
					if (shouldInsertComma)
						name += ",";
					name += $"[{GetUserFriendlyName(genericType)}]";
					shouldInsertComma = true;
				}
				name += ']';
			}
			name += $", {type.GetTypeInfo().Assembly.GetName().Name}";
			return name;
		}
	}
}

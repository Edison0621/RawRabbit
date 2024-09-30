using System;
using System.IO;
using Newtonsoft.Json;

namespace RawRabbit.Serialization;

public class JsonSerializer : StringSerializerBase
{
	private readonly Newtonsoft.Json.JsonSerializer _json;
	private const string ApplicationJson = "application/json";
	public override string ContentType => ApplicationJson;

	public JsonSerializer(Newtonsoft.Json.JsonSerializer json)
	{
		this._json = json;
	}

	public override string SerializeToString(object obj)
	{
		switch (obj)
		{
			case null:
				return string.Empty;
			case string str:
				return str;
		}

		using StringWriter sw = new();
		this._json.Serialize(sw, obj);
		string serialized = sw.GetStringBuilder().ToString();
		return serialized;
	}

	public override object Deserialize(Type type, string str)
	{
		if (type == typeof(string))
		{
			return str;
		}

		using JsonTextReader jsonReader = new(new StringReader(str));
		object obj = this._json.Deserialize(jsonReader, type);
		return obj;
	}
}

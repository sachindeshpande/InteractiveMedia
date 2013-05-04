using Newtonsoft.Json;

namespace SocketIoClient
{
	[JsonObject(MemberSerialization.OptIn)]
	public class Part
	{
		[JsonProperty]
        public float X { get; set; }

		[JsonProperty]
		public float Y { get; set; }

		[JsonProperty]
		public float Z { get; set; }

		public Part()
		{
		}

		public string ToJsonString()
		{
			return JsonConvert.SerializeObject(this);
		}
		public static Part Deserialize(string jsonString)
		{
			return JsonConvert.DeserializeObject<Part>(jsonString);
		}
	}
}

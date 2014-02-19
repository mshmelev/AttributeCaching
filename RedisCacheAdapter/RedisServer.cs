using System;

namespace AttributeCaching.CacheAdapters
{
	internal class RedisServer
	{
		public RedisServer (string server)
		{
			string[] parts = server.Split (':');
			Host = parts[0];
				
			if (parts.Length > 1)
				Port = Convert.ToInt32 (parts[1]);
			if (Port == 0)
				Port = 6379;
		}

		public string Host
		{
			get;
			private set;
		}

		public int Port
		{
			get;
			private set;
		}
	}
}
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Threading;
using System.Xml;
using BookSleeve;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RedisCacheAdapter.Tests.Helpers;

namespace RedisCacheAdapter.Tests
{
	[TestClass]
	public class CacheTests
	{
		private static RedisConnection redisDb;

		private AttributeCaching.CacheAdapters.RedisCacheAdapter cache;
		private Exception backgroundException;



		[ClassInitialize]
		public static void ClassInit(TestContext context)
		{
			var conBuilder = new DbConnectionStringBuilder();
			conBuilder.ConnectionString = ConfigurationManager.ConnectionStrings["RedisDB"].ConnectionString;
			string[] server = ((string) conBuilder["Server"]).Split (':');
			redisDb = new RedisConnection(server[0], Convert.ToInt32 (server[1]));
			redisDb.Open().Wait();
		}

		[ClassCleanup]
		public static void ClassCleanup()
		{
			CleanRedis();
		}


		[TestInitialize]
		public void Init()
		{
			cache = new AttributeCaching.CacheAdapters.RedisCacheAdapter(ConfigurationManager.ConnectionStrings["RedisDB"].ConnectionString);
			cache.OnError += OnCacheError;
		}


		[TestCleanup]
		public void Cleanup()
		{
			cache.Dispose();
			if (backgroundException != null)
			{
				var ex = backgroundException;
				backgroundException = null;
				throw ex;
			}

			CleanRedis();
		}


		private static void CleanRedis()
		{
			var keys = redisDb.Keys.Find (0, "_~*").Result;
			if (keys.Length > 0)
				redisDb.Keys.Remove (0, keys).Wait();
			keys = redisDb.Keys.Find (1, "_~*").Result;
			if (keys.Length > 0)
				redisDb.Keys.Remove (1, keys).Wait();
		}


		private void OnCacheError(object sender, Exception ex)
		{
			backgroundException = ex;
		}




		[TestMethod]
		public void GetSetTest()
		{
			cache.SetAsync("_~k1", "v1", TimeSpan.FromMinutes(1), null).Wait();
			Assert.AreEqual("v1", cache.Get("_~k1", null));
		}


		[TestMethod]
		public void SerializeDateTimeOffset()
		{
			DateTimeOffset d = new DateTimeOffset(2014, 2, 26, 13, 15, 48, TimeSpan.FromHours (-11));

			cache.SetAsync ("_~k1", d, TimeSpan.FromMinutes(1), null).Wait();
			cache.MemoryCache.Remove("_~k1");
	
			DateTimeOffset d2 = (DateTimeOffset)cache.Get ("_~k1", null);
			Assert.AreEqual (d, d2);
			Assert.AreEqual (d.Offset, d2.Offset);
		}


		[TestMethod]
		public void SerializeXmlDocument()
		{
			var xml = new XmlDocument();
			xml.LoadXml ("<?xml version=\"1.0\"?><a><b>1</b></a>");

			cache.SetAsync ("_~k1", xml, TimeSpan.FromMinutes(1), null).Wait();
			cache.MemoryCache.Remove("_~k1");

			var xml2 = (XmlDocument)cache.Get ("_~k1", null);
			Assert.AreEqual (xml.OuterXml, xml2.OuterXml);
		}


		[TestMethod]
		public void SerializeCustomClass()
		{
			var c = new ComplexClass
			{
				P1 = "s1",
				P2 = new [] {"ps1", "ps2", "ps3"},
				arr = new[] { 4,5,6,7},
				f1 = 77,
				f2 = null,
				f3= 92,
				subs = new[] { new ComplexClass {P1 = "sub_p1"} },
				fb = "bse"
			};

			cache.SetAsync ("_~k1", c, TimeSpan.FromMinutes(1), null).Wait();
			cache.MemoryCache.Remove("_~k1");

			var c2= (ComplexClass)cache.Get ("_~k1", null);
			
			Assert.AreEqual (c.P1, c2.P1);
			CollectionAssert.AreEqual(c.P2, c2.P2);
			CollectionAssert.AreEqual(c.arr, c2.arr);
			Assert.AreEqual (c.f1, c2.f1);
			Assert.AreEqual (c.f2, c2.f2);
			Assert.AreEqual (c.f3, c2.f3);
			Assert.AreEqual(c.subs.Length, c2.subs.Length);
			Assert.AreEqual(c.subs[0].P1, c2.subs[0].P1);
			Assert.AreEqual(c.fb, c2.fb);
		}

		

		[TestMethod]
		public void RemoveTest()
		{
			cache.SetAsync("_~k1", "v1", TimeSpan.FromMinutes(1), null).Wait();
			Assert.AreEqual("v1", cache.Get("_~k1", null));
			Assert.IsTrue(cache.Remove("_~k1", null));
			Assert.IsNull(cache.Get("_~k1", null));
		}


		[TestMethod]
		public void ExpireTest()
		{
			cache.SetAsync("_~k1", "v1", TimeSpan.FromSeconds(1), null).Wait();
			Assert.AreEqual("v1", cache.Get("_~k1", null));
			Thread.Sleep (1100);
			Assert.IsNull(cache.Get("_~k1", null));
		}


		[TestMethod]
		public void MissingMemCacheTest()
		{
			redisDb.Strings.Set (0, "_~k1", AttributeCaching.CacheAdapters.ProtoBuf.ProtoBufHelper.Serialize ("v1")).Wait();
			Assert.IsFalse (cache.MemoryCache.Contains ("_~k1"));
			cache.Get ("_~k1", null);
			Assert.IsTrue (cache.MemoryCache.Contains ("_~k1"));
		}

		[TestMethod]
		public void MissingMemCacheWithExpirationTest()
		{
			redisDb.Strings.Set (0, "_~k1", AttributeCaching.CacheAdapters.ProtoBuf.ProtoBufHelper.Serialize ("v1"), 1).Wait();
			Assert.IsFalse (cache.MemoryCache.Contains ("_~k1"));
			cache.Get ("_~k1", null);
			Assert.IsTrue (cache.MemoryCache.Contains ("_~k1"));
			Thread.Sleep (1100);
			Assert.IsFalse (cache.MemoryCache.Contains("_~k1"));
		}


		[TestMethod]
		public void RemoteChangeKeyTest()
		{
			cache.SetAsync ("_~k1", "v1", TimeSpan.FromSeconds (10), null).Wait();
			Assert.AreEqual("v1", cache.Get("_~k1", null));

			redisDb.Strings.Set(0, "_~k1", AttributeCaching.CacheAdapters.ProtoBuf.ProtoBufHelper.Serialize("v2"), 10).Wait();
			Thread.Sleep (200);
			Assert.AreEqual("v2", cache.Get("_~k1", null));
		}


		[TestMethod]
		public void RemoteDeleteKeyTest()
		{
			cache.SetAsync ("_~k1", "v1", TimeSpan.FromSeconds (10), null).Wait();
			Assert.AreEqual("v1", cache.Get("_~k1", null));

			redisDb.Keys.Remove (0, "_~k1").Wait();
			Thread.Sleep (200);
			Assert.IsNull(cache.Get("_~k1", null));
		}


		[TestMethod]
		public void FlushNotificationTest()
		{
			cache.SetAsync ("_~k1", "v1", TimeSpan.FromSeconds (10), null).Wait();
			Assert.IsTrue(cache.MemoryCache.Contains("_~k1"));

			redisDb.Publish (AttributeCaching.CacheAdapters.RedisCacheAdapter.FlushedEventName, "1").Wait();
			Thread.Sleep(200);
			Assert.IsFalse(cache.MemoryCache.Contains("_~k1"));
		}


		[TestMethod]
		public void EvictAllTest()
		{
			cache.SetAsync("_~k1", "v1", TimeSpan.FromSeconds(10), new[] { "_~d1", "_~d3" }).Wait();
			cache.SetAsync("_~k2", "v2", TimeSpan.FromSeconds(10), new[] { "_~d1", "_~d2", "_~d3" }).Wait();
			cache.SetAsync("_~k3", "v3", TimeSpan.FromSeconds(10), new[] { "_~d2", "_~d3" }).Wait();
			cache.SetAsync("_~k4", "v4", TimeSpan.FromSeconds(10), new[] { "_~d3" }).Wait();
			cache.SetAsync("_~k5", "v5", TimeSpan.FromSeconds(10), new string[0]).Wait();

			cache.EvictAll (null, "_~d1", "_~d2", "_~d3");
			Thread.Sleep(200);

			Assert.AreEqual("v1", cache.Get("_~k1", null));
			Assert.IsNull(cache.Get("_~k2", null));
			Assert.AreEqual("v3", cache.Get("_~k3", null));
			Assert.AreEqual("v4", cache.Get("_~k4", null));
			Assert.AreEqual("v5", cache.Get("_~k5", null));
		}


		[TestMethod]
		public void EvictAnyTest()
		{
			cache.SetAsync("_~k1", "v1", TimeSpan.FromSeconds(10), new[] { "_~d1", "_~d3" }).Wait();
			cache.SetAsync("_~k2", "v2", TimeSpan.FromSeconds(10), new[] { "_~d1", "_~d2", "_~d3" }).Wait();
			cache.SetAsync("_~k3", "v3", TimeSpan.FromSeconds(10), new[] { "_~d2", "_~d3" }).Wait();
			cache.SetAsync("_~k4", "v4", TimeSpan.FromSeconds(10), new[] { "_~d3" }).Wait();
			cache.SetAsync("_~k5", "v5", TimeSpan.FromSeconds(10), new[] { "_~d4", "_~d5", "_~d6" }).Wait();
			cache.SetAsync("_~k6", "v6", TimeSpan.FromSeconds(10), new string[0]).Wait();

			cache.EvictAny( null, "_~d1", "_~d2", "_~d3");
			Thread.Sleep(200);

			Assert.IsNull(cache.Get("_~k1", null));
			Assert.IsNull(cache.Get("_~k2", null));
			Assert.IsNull(cache.Get("_~k3", null));
			Assert.IsNull(cache.Get("_~k4", null));
			Assert.AreEqual("v5", cache.Get("_~k5", null));
			Assert.AreEqual("v6", cache.Get("_~k6", null));
		}


		[TestMethod]
		public void FailoverTest()
		{
			cache.OnError -= OnCacheError;
			cache.Dispose();
			cache = new AttributeCaching.CacheAdapters.RedisCacheAdapter(ConfigurationManager.ConnectionStrings["RedisFailoverDB"].ConnectionString);
			cache.OnError += OnCacheError;

			cache.SetAsync("_~k1", "v1", TimeSpan.FromSeconds(10), null).Wait();
			Assert.AreEqual("v1", cache.Get("_~k1", null));

			cache.RedisConnection.Close (false);		// emulate failure
			Thread.Sleep (100);

			cache.SetAsync("_~k1", "v1", TimeSpan.FromSeconds(10), null).Wait();
			Assert.AreEqual("v1", cache.Get("_~k1", null));
		}


		[TestMethod]
		public void ListIsOverwritten()
		{
			var c = new ClassWithList();
			c.Items = new List<int> {1, 3, 5};

			cache.SetAsync("_~k1", c, TimeSpan.FromMinutes(1), null).Wait();
			cache.MemoryCache.Remove("_~k1");

			var c2 = (ClassWithList)cache.Get("_~k1", null);
			CollectionAssert.AreEqual (new[] {1, 3, 5}, c2.Items);
		}


		[TestMethod]
		public void ReadonlyProperty()
		{
			var c = new ClassWithGetProp();
			c.PublicProp = "aaa";

			cache.SetAsync("_~k1", c, TimeSpan.FromMinutes(1), null).Wait();
			cache.MemoryCache.Remove("_~k1");

			var c2 = (ClassWithGetProp)cache.Get("_~k1", null);
			Assert.AreEqual ("aaa", c2.PublicProp);
			Assert.AreEqual ("aaa_", c2.ReadonlyProp);
			Assert.AreEqual ("aaa_", c2.PrivateSetProp);

			c2.PublicProp = "bbb";
			Assert.AreEqual("bbb_", c2.ReadonlyProp);
			Assert.AreEqual("bbb_", c2.PrivateSetProp);
		}


		[TestMethod]
		public void DataContractWithoutProtoContractSerialization()
		{
			var c = new DataContractClass();
			c.P1 = "ppp1";
			c.f1 = 42;

			cache.SetAsync("_~k1", c, TimeSpan.FromMinutes(1), null).Wait();
			cache.MemoryCache.Remove("_~k1");
			var c2 = (DataContractClass)cache.Get("_~k1", null);

			Assert.AreEqual ("ppp1", c2.P1);
			Assert.AreEqual (42, c2.f1);
		}


		[TestMethod]
		public void XmlSerializableWithoutProtoContractSerialization()
		{
			var c = new SerializableClass();
			c.P1 = "ppp1";
			c.f1 = 42;

			cache.SetAsync("_~k1", c, TimeSpan.FromMinutes(1), null).Wait();
			cache.MemoryCache.Remove("_~k1");
			var c2 = (SerializableClass)cache.Get("_~k1", null);

			Assert.AreEqual ("ppp1", c2.P1);
			Assert.AreEqual (42, c2.f1);
		}


		[TestMethod]
		public void ProtoContractSerialization()
		{
			var c = new ProtoContractClass();
			c.P1 = "ppp1";
			c.f1 = 42;

			cache.SetAsync("_~k1", c, TimeSpan.FromMinutes(1), null).Wait();
			cache.MemoryCache.Remove("_~k1");
			var c2 = (ProtoContractClass)cache.Get("_~k1", null);

			Assert.IsNull (c2.P1);
			Assert.AreEqual (42, c2.f1);
		}
		
		
		
		[TestMethod]
		public void TestCollectionSerialization()
		{
			int[] arr = {1, 2, 3, 4, 5};
			cache.SetAsync("_~k1", arr, TimeSpan.FromMinutes(1), null).Wait();
			cache.MemoryCache.Remove("_~k1");
			int[] arr2 = (int[])cache.Get("_~k1", null);
			CollectionAssert.AreEqual (arr, arr2);

			var lst= new List<int> {1, 2, 3, 4, 5, 6};
			cache.SetAsync("_~k1", lst, TimeSpan.FromMinutes(1), null).Wait();
			cache.MemoryCache.Remove("_~k1");
			var lst2= (List<int>)cache.Get("_~k1", null);
			CollectionAssert.AreEqual (lst, lst2);

			var set = new HashSet<string> { "1", "2", "3", "4", "5", "6", "7" };
			cache.SetAsync("_~k1", set, TimeSpan.FromMinutes(1), null).Wait();
			cache.MemoryCache.Remove("_~k1");
			var set2 = (HashSet<string>)cache.Get("_~k1", null);
			Assert.IsTrue (set.IsSubsetOf (set2) && set2.IsSubsetOf (set));
		}

		// TODO: Test empty & null collections


		[TestMethod]
		[Ignore]
		public void TestDictionarySerialization()
		{
			var dic = new Dictionary<string, DicClass> { { "a", new DicClass { Prop = "p1" } }, { "b", new DicClass { Prop = "p1" } } };
			cache.SetAsync("_~k1", dic, TimeSpan.FromMinutes(1), null).Wait();
			cache.MemoryCache.Remove("_~k1");
			var dic2 = (Dictionary<string, DicClass>)cache.Get("_~k1", null);
			CollectionAssert.AreEqual (dic, dic2);
		}


	}


}

﻿using System;
using System.Linq;
using System.Runtime.Caching;

namespace AttributeCaching.Tests.Helpers
{
	public class TestDependeciesClass
	{
		private string[] cars = {"carA", "carB", "carC"};
		private readonly IVisitor visitor;

		public TestDependeciesClass(IVisitor visitor)
		{
			this.visitor = visitor;
		}


		[Cacheable("cars")]
		public string GetCars()
		{
			visitor.Visit();
			return String.Join(",", cars);
		}

		[Cacheable("cars", "car_0", "car_1", "car_2")]
		public string GetCar(int car)
		{
			visitor.Visit();
			return cars[car];
		}

		[Cacheable]
		public string GetCarChangingDependency(int car)
		{
			visitor.Visit();
			CacheScope.CurrentContext.DependencyTags.Add("car_"+car);
			return cars[car];
		}

		public void Update (int car, string newName)
		{
			cars[car] = newName;
			CacheFactory.Cache.EvictAll("cars");
		}
	}
}
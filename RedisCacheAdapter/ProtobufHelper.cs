﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ProtoBuf;
using ProtoBuf.Meta;

namespace AttributeCaching.CacheAdapters
{
	public static class ProtoBufHelper
	{
		private static readonly RuntimeTypeModel runtimeTypeModel = TypeModel.Create();
		private static readonly HashSet<Type> knownTypes = new HashSet<Type>();


		static ProtoBufHelper()
		{
			// types supported by protobuf-net out of the box
			knownTypes.Add(typeof(Boolean));
			knownTypes.Add(typeof(Char));
			knownTypes.Add(typeof(SByte));
			knownTypes.Add(typeof(Byte));
			knownTypes.Add(typeof(Int16));
			knownTypes.Add(typeof(UInt16));
			knownTypes.Add(typeof(Int32));
			knownTypes.Add(typeof(UInt32));
			knownTypes.Add(typeof(Int64));
			knownTypes.Add(typeof(UInt64));
			knownTypes.Add(typeof(Single));
			knownTypes.Add(typeof(Double));
			knownTypes.Add(typeof(Decimal));
			knownTypes.Add(typeof(DateTime));
			knownTypes.Add(typeof(String));
			knownTypes.Add(typeof(TimeSpan));
			knownTypes.Add(typeof(Guid));
			knownTypes.Add(typeof(Uri));
			knownTypes.Add(typeof(byte[]));
			knownTypes.Add(typeof(Type));
		}



		/// <summary>
		/// Serializes object with its type
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static byte[] Serialize(object obj)
		{
			if (obj == null)
				throw new ArgumentNullException("obj");

			var objType = obj.GetType();
			PrepareType (objType);

			using (var ms = new MemoryStream())
			{
				runtimeTypeModel.SerializeWithLengthPrefix (ms, objType.AssemblyQualifiedName, typeof (string), PrefixStyle.Base128, 1);
				runtimeTypeModel.SerializeWithLengthPrefix (ms, obj, objType, PrefixStyle.Base128, 1);

				return ms.ToArray();
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static object Deserialize (byte[] data)
		{
			if (data == null)
				return null;

			using (var ms = new MemoryStream (data))
			{
				string typeName = (string) runtimeTypeModel.DeserializeWithLengthPrefix (ms, null, typeof (string), PrefixStyle.Base128, 1);
				var objType = Type.GetType (typeName);
				PrepareType (objType);

				return runtimeTypeModel.DeserializeWithLengthPrefix (ms, null, objType, PrefixStyle.Base128, 1);
			}
		}



		private static void PrepareType(Type type)
		{
			if (IsKnownType (type))
				return;
			AddTypeAsSerializable (type);
		}



		/// <summary>
		/// Adds type as serializable with all it's public fields/properties
		/// </summary>
		/// <param name="type"></param>
		private static void AddTypeAsSerializable(Type type)
		{
			type = GetEnumerableElementType(type);

			var metaType = runtimeTypeModel.Add(type, false);
			var fields = type.GetMembers(BindingFlags.Instance | BindingFlags.Public);
			foreach (var fieldInfo in fields)
			{
				if (fieldInfo.MemberType == MemberTypes.Field || fieldInfo.MemberType == MemberTypes.Property)
				{
					Type memberType = (fieldInfo is PropertyInfo) ? ((PropertyInfo)fieldInfo).PropertyType : ((FieldInfo)fieldInfo).FieldType;
					if (!IsKnownType(memberType))
						AddTypeAsSerializable(memberType);

					metaType.Add(fieldInfo.Name);
				}
			}
		}


		private static bool IsKnownType(Type type)
		{
			if (type== null || knownTypes.Contains(type))
				return true;

			if (runtimeTypeModel.IsDefined (type))
			{
				knownTypes.Add (type);
				return true;
			}

			var elementType = GetEnumerableElementType(type);
			if (elementType!= type && IsKnownType (elementType))
			{
				knownTypes.Add(type);
				return true;
			}

			return false;
		}


		private static Type GetEnumerableElementType(Type type)
		{
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
				type = type.GetGenericArguments()[0];
			else if (type.IsArray)
				type = type.GetElementType();

			return type;
		}

	}
}
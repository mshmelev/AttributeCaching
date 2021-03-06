﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using ProtoBuf;
using ProtoBuf.Meta;

namespace AttributeCaching.CacheAdapters.ProtoBuf
{
	/// <summary>
	/// Helps to serialize and deserialize custom classes.
	/// </summary>
	internal static class ProtoBufHelper
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

			// ours
			runtimeTypeModel.Add(typeof(XmlDocument), false).SetSurrogate(typeof(XmlDocumentSurrogate));
			runtimeTypeModel.Add(typeof(DateTimeOffset), false).SetSurrogate(typeof(DateTimeOffsetSurrogate));
			knownTypes.Add(typeof(XmlDocument));
			knownTypes.Add(typeof(DateTimeOffset));
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
			lock (runtimeTypeModel)
			{
				if (IsKnownType (type))
					return;
				AddTypeAsSerializable (type);
			}
		}



		/// <summary>
		/// Adds type as serializable with all it's public fields/properties
		/// </summary>
		/// <param name="type"></param>
		private static void AddTypeAsSerializable(Type type)
		{
			// special case for dictionaries
			var dicTypes = GetDictionaryElementTypes(type);
			if (dicTypes.Length > 0)
			{
				foreach (var dicType in dicTypes)
				{
					if (!IsKnownType (dicType))
						AddTypeAsSerializable (dicType);
				}
				knownTypes.Add (type);
				return;
			}


			type = GetEnumerableElementType(type);
			if (Nullable.GetUnderlyingType(type) != null)
				type = Nullable.GetUnderlyingType(type);

			var metaType = runtimeTypeModel.Add(type, false);
			knownTypes.Add (type);

			var fields = type.GetMembers(BindingFlags.Instance | BindingFlags.Public).OrderBy (f => f.Name);
			int fieldNumber = 0;
			foreach (var fieldInfo in fields)
			{
				if (fieldInfo.MemberType == MemberTypes.Field || fieldInfo.MemberType == MemberTypes.Property)
				{
					Type memberType;
					var propertyInfo = fieldInfo as PropertyInfo;
					if (propertyInfo != null)
					{
						memberType = propertyInfo.PropertyType;
						if (!propertyInfo.CanWrite)
							continue;
						if (!propertyInfo.SetMethod.IsPublic)
							continue;
					}
					else
					{
						memberType = ((FieldInfo)fieldInfo).FieldType;
					}

					if (!IsKnownType(memberType))
						AddTypeAsSerializable(memberType);

					var valueMember = metaType.AddField (++fieldNumber, fieldInfo.Name, null, null);
					valueMember.OverwriteList = true;
				}
			}
		}


		private static bool IsKnownType(Type type)
		{
			if (type == null || knownTypes.Contains(type))
				return true;

			// proto-buf natively supported
			if (runtimeTypeModel.IsDefined(type))
			{
				if (type.CustomAttributes.Any (attr => attr.AttributeType == typeof (DataContractAttribute) || attr.AttributeType == typeof (XmlTypeAttribute))
					&& type.GetCustomAttribute<ProtoContractAttribute>()== null)
				{
					// Type with DataContract/XmlType attribute but no ProtoContract are in a risk group: Protobuffer thinks that it can serialize them,
					// but it can only in the case they have DataMember.Order value.
					// To avoid risk all such types are forced to be serialized with all public fields/props
					return false;
				}
				knownTypes.Add(type);
				return true;
			}

			// dictionaries
			var dicTypes = GetDictionaryElementTypes (type);
			if (dicTypes.Length> 0)
			{
				if (dicTypes.All (IsKnownType))
				{
					knownTypes.Add (type);
					return true;
				}
				return false;
			}

			// enumerables
			var elementType = GetEnumerableElementType(type);
			if (elementType != type && IsKnownType(elementType))
			{
				knownTypes.Add(type);
				return true;
			}

			// nullables
			var nullType = Nullable.GetUnderlyingType(type);
			if (nullType != null && IsKnownType(nullType))
			{
				knownTypes.Add(type);
				return true;
			}

			return false;
		}


		private static Type GetEnumerableElementType(Type type)
		{
			if (type.IsArray)
				type = type.GetElementType();
			else if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type))
				type = type.GetGenericArguments()[0];

			return type;
		}


		private static Type[] GetDictionaryElementTypes (Type type)
		{
			if (type.IsGenericType && typeof (IDictionary).IsAssignableFrom (type))
				return type.GetGenericArguments();
			return new Type[0];
		}

	}
}
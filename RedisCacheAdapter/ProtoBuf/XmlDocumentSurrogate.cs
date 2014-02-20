using System.IO;
using System.Xml;
using ProtoBuf;

namespace AttributeCaching.CacheAdapters.ProtoBuf
{
	[ProtoContract]
	internal class XmlDocumentSurrogate
	{
		[ProtoMember(1)]
		public string XmlText { get; set; }

		public static implicit operator XmlDocumentSurrogate(XmlDocument xml)
		{
			if (xml == null)
				return null;

			using (var stringWriter = new StringWriter())
			{
				using (var xmlTextWriter = XmlWriter.Create(stringWriter))
				{
					xml.WriteTo(xmlTextWriter);
					xmlTextWriter.Flush();

					return new XmlDocumentSurrogate { XmlText = stringWriter.GetStringBuilder().ToString() };
				}
			}
		}

		public static implicit operator XmlDocument(XmlDocumentSurrogate value)
		{
			if (value == null)
				return null;

			var x = new XmlDocument();
			x.LoadXml(value.XmlText);
			return x;
		}
	}
}
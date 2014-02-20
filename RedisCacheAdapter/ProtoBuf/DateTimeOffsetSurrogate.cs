using System;
using ProtoBuf;
using ProtoBuf.Meta;

namespace AttributeCaching.CacheAdapters.ProtoBuf
{
    public class InitializeDateTimeOffsetSurrogate
    {
        static InitializeDateTimeOffsetSurrogate()
        {
            RuntimeTypeModel.Default.Add(typeof (DateTimeOffset), false).SetSurrogate(typeof (DateTimeOffsetSurrogate));
        }
    }

    [ProtoContract]
    public class DateTimeOffsetSurrogate
    {
        [ProtoMember(1)]
        public long DateTimeTicks { get; set; }

        [ProtoMember(2)]
        public short OffsetMinutes { get; set; }

        public static implicit operator DateTimeOffsetSurrogate(DateTimeOffset value)
        {
            return new DateTimeOffsetSurrogate
            {
                DateTimeTicks = value.Ticks,
                OffsetMinutes = (short) value.Offset.TotalMinutes
            };
        }

        public static implicit operator DateTimeOffset(DateTimeOffsetSurrogate value)
        {
	        if (value == null)
		        return DateTimeOffset.MinValue;

            return new DateTimeOffset(value.DateTimeTicks, TimeSpan.FromMinutes(value.OffsetMinutes));
        }
    }

}

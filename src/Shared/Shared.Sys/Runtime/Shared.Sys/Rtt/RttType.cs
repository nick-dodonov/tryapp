using System;
using System.Reflection;

namespace Shared.Sys.Rtt
{
    public class RttType
    {
        public Type Type = null!;
        public RttField[] Fields;

        public RttType(Type type)
        {
            Fields = Array.Empty<RttField>();
        }
    }

    public class RttField
    {
        public FieldInfo FieldInfo;
        public int RuntimeOffset;

        public RttField(FieldInfo fieldInfo)
        {
            FieldInfo = fieldInfo;
        }
    }
}

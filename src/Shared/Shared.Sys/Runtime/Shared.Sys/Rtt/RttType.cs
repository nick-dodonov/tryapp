using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace Shared.Sys.Rtt
{
    public class RttType
    {
        private static readonly Dictionary<Type, RttType> _types = new();
        private static readonly MethodInfo _isUnmanagedMethod;
        private static readonly MethodInfo _initGenericMethod;

        static RttType()
        {
            _isUnmanagedMethod = GetThisMethod(nameof(IsUnmanaged),
                BindingFlags.NonPublic | BindingFlags.Static);
            _initGenericMethod = GetThisMethod(nameof(InitGeneric),
                BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private static MethodInfo GetThisMethod(string name, BindingFlags flags)
        {
            var method = typeof(RttType).GetMethod(name, flags)!;
            if (method == null)
                throw new InvalidOperationException($"Method {name} not found");
            return method;
        }

        public static RttType Get<T>() where T : struct
        {
            if (_types.TryGetValue(typeof(T), out var type))
                return type;
            type = new(typeof(T));
            _types.Add(typeof(T), type);
            return type;
        }

        private readonly Type _type;
        public Type Type => _type;

        private readonly RttField[] _publicFields;
        public RttField[] PublicFields => _publicFields;

        private RttType(Type type)
        {
            _type = type;

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            _publicFields = new RttField[fields.Length];
            for (var i = 0; i < fields.Length; ++i)
                PublicFields[i] = new(fields[i]);

            var method = _initGenericMethod.MakeGenericMethod(type);
            method.Invoke(this, null);
        }

        [Preserve]
        private void InitGeneric<T>()
        {
            if (PublicFields.Length > 0)
                InitUnmanagedFields<T>();
        }

        private void InitUnmanagedFields<T>()
        {
            //TODO: another variants to obtain RttInfo for types not handled with user
            var methodInfo = Type.GetMethod(nameof(IRttInfoProvider.GetRttInfo), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (methodInfo == null)
                throw new InvalidOperationException($"Type {Type} does not have {nameof(IRttInfoProvider.GetRttInfo)} method");

            var dummy = (T)RuntimeHelpers.GetUninitializedObject(Type);
            var rttInfo = (RttInfo)methodInfo.Invoke(dummy, null);

            var fieldsLength = _publicFields.Length;
            for (var i = 0; i < fieldsLength; ++i)
            {
                var rttField = _publicFields[i];

                var fieldInfo = rttField.FieldInfo;
                var fieldName = fieldInfo.Name;
                var fieldType = fieldInfo.FieldType;

                rttField.IsUnmanaged = IsUnmanagedType(fieldType);

                // rttField.RuntimeOffset if provided
                var j = 0;
                var infoFields = rttInfo.FieldItems;
                var infoLength = infoFields.Count;
                for (; j < infoLength; j++)
                {
                    var infoField = infoFields[j];
                    if (infoField.Name != fieldName)
                        continue;

                    rttField.RuntimeOffset = infoField.Offset;
                    break;
                }
            }
        }

        [Preserve]
        private static bool IsUnmanaged<T>() where T : unmanaged => typeof(T).IsValueType; //true

        private static bool IsUnmanagedType(Type type)
        {
            try
            {
                var method = _isUnmanagedMethod.MakeGenericMethod(type);
                return (bool)method.Invoke(null, null);
            }
            catch
            {
                return false;
            }
        }
    }

    public class RttField
    {
        public FieldInfo FieldInfo { get; }
        public Type FieldType => FieldInfo.FieldType;

        public bool IsUnmanaged { get; internal set; }

        private int? _runtimeOffset;
        public bool HasRuntimeOffset => _runtimeOffset.HasValue;

        public int RuntimeOffset
        {
            get
            {
                if (_runtimeOffset.HasValue)
                    return _runtimeOffset.Value;

                throw new InvalidOperationException($"Field {FieldInfo.Name} of type {FieldInfo.ReflectedType!.FullName} didn't provided runtime offset (check GetRttInfo())");
            }
            internal set => _runtimeOffset = value;
        }

        internal RttField(FieldInfo fieldInfo)
        {
            FieldInfo = fieldInfo;
        }
    }

    public interface IRttInfoProvider
    {
        RttInfo GetRttInfo();
    }

    public class RttInfo
    {
        internal struct FieldItem
        {
            public readonly string Name;
            public readonly int Offset; // differs from Marshal.OffsetOf because it's used in runtime not only for marshaled objects

            public FieldItem(string name, int offset)
            {
                Name = name;
                Offset = offset;
            }
        }

        internal List<FieldItem> FieldItems { get; } = new();

        public unsafe RttInfo Add<T, TField>(ref T objRef, ref TField fieldRef, string fieldName)
        {
            var objPtr = (byte*)Unsafe.AsPointer(ref objRef);
            var fieldPtr = (byte*)Unsafe.AsPointer(ref fieldRef);
            FieldItems.Add(new(fieldName, (int)(fieldPtr - objPtr)));
            return this;
        }
    }
}
using System;
using System.Runtime.CompilerServices;

namespace Shared.Tp.Tick
{
    public static class NumericHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetLongMaxValue<T>() where T : unmanaged
        {
            if (typeof(T) == typeof(byte)) return byte.MaxValue;
            if (typeof(T) == typeof(sbyte)) return sbyte.MaxValue;
            if (typeof(T) == typeof(short)) return short.MaxValue;
            if (typeof(T) == typeof(ushort)) return ushort.MaxValue;
            if (typeof(T) == typeof(int)) return int.MaxValue;
            if (typeof(T) == typeof(uint)) return uint.MaxValue;
            if (typeof(T) == typeof(long)) return long.MaxValue;
    
            throw new ArgumentException($"Type {typeof(T)} is not supported");
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Add<T>(T a, T b) where T : unmanaged
        {
            if (typeof(T) == typeof(int))
            {
                var result = Unsafe.As<T, int>(ref a) + Unsafe.As<T, int>(ref b);
                return Unsafe.As<int, T>(ref result);
            }

            if (typeof(T) == typeof(long))
            {
                var result = Unsafe.As<T, long>(ref a) + Unsafe.As<T, long>(ref b);
                return Unsafe.As<long, T>(ref result);
            }

            if (typeof(T) == typeof(ushort))
            {
                unchecked
                {
                    var result = (ushort)(Unsafe.As<T, ushort>(ref a) + Unsafe.As<T, ushort>(ref b));
                    return Unsafe.As<ushort, T>(ref result);
                }
            }
            
            ThrowNotSupported<T>();
            return default;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Sub<T>(T a, T b) where T : unmanaged
        {
            if (typeof(T) == typeof(int))
            {
                var result = Unsafe.As<T, int>(ref a) - Unsafe.As<T, int>(ref b);
                return Unsafe.As<int, T>(ref result);
            }

            if (typeof(T) == typeof(long))
            {
                var result = Unsafe.As<T, long>(ref a) - Unsafe.As<T, long>(ref b);
                return Unsafe.As<long, T>(ref result);
            }

            if (typeof(T) == typeof(ushort))
            {
                unchecked
                {
                    var result = (ushort)(Unsafe.As<T, ushort>(ref a) - Unsafe.As<T, ushort>(ref b));
                    return Unsafe.As<ushort, T>(ref result);
                }
            }

            ThrowNotSupported<T>();
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Div<T>(T a, long b) where T : unmanaged
        {
            if (typeof(T) == typeof(int))
            {
                var ra = Unsafe.As<T, int>(ref a);
                return (float)((double)ra / b);
            }

            if (typeof(T) == typeof(long))
            {
                var ra = Unsafe.As<T, long>(ref a);
                return (float)((double)ra / b);
            }

            if (typeof(T) == typeof(ushort))
            {
                var ra = Unsafe.As<T, ushort>(ref a);
                return (float)ra / b;
            }

            ThrowNotSupported<T>();
            return 0;
        }
        
        private static void ThrowNotSupported<T>() => 
            throw new NotSupportedException($"Type {typeof(T)} is not supported");
    }
}
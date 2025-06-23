using System;
using System.Runtime.CompilerServices;

namespace Shared.Tp.Chrono
{
    /// <summary>
    /// Replacement for C#11 System.Numeric interfaces working in C#9
    /// </summary>
    public static class NumericHelper
    {
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
                return (float)((double)Unsafe.As<T, int>(ref a) / b);

            if (typeof(T) == typeof(long))
                return (float)((double)Unsafe.As<T, long>(ref a) / b);

            if (typeof(T) == typeof(ushort))
                return (float)Unsafe.As<T, ushort>(ref a) / b;

            ThrowNotSupported<T>();
            return 0;
        }
        
        private static void ThrowNotSupported<T>() => 
            throw new NotSupportedException($"Type {typeof(T)} is not supported");
    }
}
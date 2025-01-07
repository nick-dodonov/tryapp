using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Shared.Log
{
    /// <summary>
    /// Static logger (useful for quick usage without additional setup in ASP or in shared with client code)
    /// </summary>
    public static class Slog
    {
        public static ILoggerFactory Factory { get; private set; } = NullLoggerFactory.Instance;
        public static void SetFactory(ILoggerFactory factory) => Factory = factory;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Info(string message, [CallerFilePath] string path = "", [CallerMemberName] string member = "")
        {
            var category = new Category(path);
            message = $"{category.NameSpan.ToString()}: {member}: {message}"; //TODO: use zero-allocate
#if UNITY_5_6_OR_NEWER
            UnityEngine.Debug.unityLogger.Log(message);
#else
            //TODO: setup with ILogger
            Console.WriteLine(message);
#endif
        }
    }

    public readonly ref struct Category
    {
        private static readonly char[] PathSeparators = { '/', '\\' };
        private readonly string _path;
        private readonly int _start;
        private readonly int _length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Category(string path)
        {
            _path = path;
            _start = path.LastIndexOfAny(PathSeparators);
            ++_start; //excess slash is trimmed and not found case is handled too
            var end = path.LastIndexOf('.');
            _length = end >= 0 
                ? end - _start 
                : path.Length - _start;
        }

        public ReadOnlySpan<char> NameSpan => _path.AsSpan(_start, _length);
    }
}
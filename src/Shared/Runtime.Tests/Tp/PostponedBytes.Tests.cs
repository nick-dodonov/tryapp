using System;
using System.Text;
using NUnit.Framework;
using Shared.Tp.Util;

namespace Shared.Tests.Tp
{
    public class PostponedBytesTests
    {
        [Test]
        public void SimpleTest()
        {
            var utf8 = Encoding.UTF8;
            var p = new PostponedBytes();
            p.Add(utf8.GetBytes("123").AsSpan());
            p.Add(utf8.GetBytes("abcdefgh").AsSpan());
            Assert.IsFalse(p.Disconnected);
            var count = 0;
            p.Feed<object>((_, span) =>
            {
                switch (count)
                {
                    case 0: Assert.AreEqual(utf8.GetString(span), "123"); break;
                    case 1: Assert.AreEqual(utf8.GetString(span), "abcdefgh"); break;
                    default: Assert.Fail(); break;
                }
                ++count;
            }, null!);
            Assert.AreEqual(2, count);
        }
        
    }
}
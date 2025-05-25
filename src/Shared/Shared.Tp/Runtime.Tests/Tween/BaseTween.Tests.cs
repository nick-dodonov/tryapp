using System;

namespace Shared.Tp.Tests.Tween
{
    public class BaseTweenTests
    {
        //[Test] public void CheckFailWorks() => Assert.Fail();

        protected static (T, T, T) MakeTestData<T>(Func<int, T> factory) where T : new()
            => (factory(0), factory(1), new());
    }
}
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Shared.Sys.Unity.Tests
{
    [TestFixture]
    public class UnityTests
    {
        [UnityTest]
        public IEnumerator UnityTest_Yield()
        {
            yield return null;
        }
        
        // [UnityTest]
        // public IEnumerator UnityTest_WaitForSeconds()
        // {
        //     yield return new WaitForSeconds(5f);
        // }
    }
}
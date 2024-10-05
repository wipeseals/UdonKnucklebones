using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Wipeseals
{
    public class UdonKnucklebonesTest
    {
        [Test]
        public void InitializeTest()
        {
            var go = new GameObject("UdonKnucklebones");
            go.AddComponent<UdonKnucklebones>();
            var dut = go.GetComponent<UdonKnucklebones>();

            // OnPlayerJoinedまでは何もしない
            Assert.AreEqual(GameProgress.Initial, (GameProgress)dut.Progress);
        }
    }
}

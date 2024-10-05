using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Wipeseals
{
    public class UdonKnucklebonesTest
    {
        public static Tuple<GameObject, UdonKnucklebones> CreateUdonKnucklebones()
        {
            var go = new GameObject("UdonKnucklebones");
            go.AddComponent<UdonKnucklebones>();
            return new Tuple<GameObject, UdonKnucklebones>(go, go.GetComponent<UdonKnucklebones>());
        }

        [Test]
        public void InitializeTest()
        {
            var (go, dut) = CreateUdonKnucklebones();

            // OnPlayerJoinedまでは何もしない
            Assert.AreEqual(GameProgress.Initial, (GameProgress)dut.Progress);
        }

        [Test]
        public void ValidInspectorSettingFailTest()
        {
            var (go, dut) = CreateUdonKnucklebones();
            LogAssert.ignoreFailingMessages = true; // LogErrorが出る。Assert checkしたいので落とさない

            // OnPlayerJoinedでゲーム開始
            dut.OnPlayerJoined(null); // 本当はなにか値があってよいはずだが、今回はnull

            // Inspectorで設定すべき項目を埋めていないので、エラーになる
            Assert.AreEqual(GameProgress.ConfigurationError, (GameProgress)dut.Progress);
        }
    }
}

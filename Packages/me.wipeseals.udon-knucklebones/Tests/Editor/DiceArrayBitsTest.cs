using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Wipeseals
{
    public class DiceArrayBitsTest
    {

        [Test]
        public void GetDiceIndexTest()
        {
            Assert.AreEqual(0, DiceArrayBits.GetDiceIndex(0, 0));
            Assert.AreEqual(1, DiceArrayBits.GetDiceIndex(0, 1));
            Assert.AreEqual(2, DiceArrayBits.GetDiceIndex(0, 2));
            Assert.AreEqual(3, DiceArrayBits.GetDiceIndex(1, 0));
            Assert.AreEqual(4, DiceArrayBits.GetDiceIndex(1, 1));
            Assert.AreEqual(5, DiceArrayBits.GetDiceIndex(1, 2));
            Assert.AreEqual(6, DiceArrayBits.GetDiceIndex(2, 0));
            Assert.AreEqual(7, DiceArrayBits.GetDiceIndex(2, 1));
            Assert.AreEqual(8, DiceArrayBits.GetDiceIndex(2, 2));
        }
    }
}

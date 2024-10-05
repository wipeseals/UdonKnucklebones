using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Wipeseals
{
    public class DiceArrayBitsTest
    {
        [TestCase(0, 0, ExpectedResult = 0)]
        [TestCase(0, 1, ExpectedResult = 1)]
        [TestCase(0, 2, ExpectedResult = 2)]
        [TestCase(1, 0, ExpectedResult = 3)]
        [TestCase(1, 1, ExpectedResult = 4)]
        [TestCase(1, 2, ExpectedResult = 5)]
        [TestCase(2, 0, ExpectedResult = 6)]
        [TestCase(2, 1, ExpectedResult = 7)]
        [TestCase(2, 2, ExpectedResult = 8)]
        public int GetDiceIndexTest(int col, int row)
        {
            return DiceArrayBits.GetDiceIndex(col, row);
        }

        [TestCase(0x00000000_00000000UL, 0, 0, 1, ExpectedResult = 0x00000000_00000001UL)]
        [TestCase(0x00000000_00000000UL, 0, 1, 2, ExpectedResult = 0x00000000_00000020UL)]
        [TestCase(0x00000000_00000000UL, 0, 2, 3, ExpectedResult = 0x00000000_00000300UL)]
        [TestCase(0x00000000_00000000UL, 1, 0, 4, ExpectedResult = 0x00000000_00004000UL)]
        [TestCase(0x00000000_00000000UL, 1, 1, 5, ExpectedResult = 0x00000000_00050000UL)]
        [TestCase(0x00000000_00000000UL, 1, 2, 6, ExpectedResult = 0x00000000_00600000UL)]
        [TestCase(0x00000000_00000000UL, 2, 0, 1, ExpectedResult = 0x00000000_01000000UL)]
        [TestCase(0x00000000_00000000UL, 2, 1, 2, ExpectedResult = 0x00000000_20000000UL)]
        [TestCase(0x00000000_00000000UL, 2, 2, 3, ExpectedResult = 0x00000003_00000000UL)]
        [TestCase(0x00000001_11111111UL, 0, 0, 0, ExpectedResult = 0x00000001_11111110UL)]
        [TestCase(0x00000001_11111111UL, 0, 1, 0, ExpectedResult = 0x00000001_11111101UL)]
        [TestCase(0x00000001_11111111UL, 0, 2, 0, ExpectedResult = 0x00000001_11111011UL)]
        [TestCase(0x00000001_11111111UL, 1, 0, 0, ExpectedResult = 0x00000001_11110111UL)]
        [TestCase(0x00000001_11111111UL, 1, 1, 0, ExpectedResult = 0x00000001_11101111UL)]
        [TestCase(0x00000001_11111111UL, 1, 2, 0, ExpectedResult = 0x00000001_11011111UL)]
        [TestCase(0x00000001_11111111UL, 2, 0, 0, ExpectedResult = 0x00000001_10111111UL)]
        [TestCase(0x00000001_11111111UL, 2, 1, 0, ExpectedResult = 0x00000001_01111111UL)]
        [TestCase(0x00000001_11111111UL, 2, 2, 0, ExpectedResult = 0x00000000_11111111UL)]
        public ulong PutDiceTest(ulong initialBits, int col, int row, int value)
        {
            return DiceArrayBits.PutDice(initialBits, col, row, value);
        }

        [TestCase(0, ExpectedResult = 0x00000000_00000000UL)]
        [TestCase(1, ExpectedResult = 0x00000001_11111111UL)]
        [TestCase(2, ExpectedResult = 0x00000002_22222222UL)]
        [TestCase(3, ExpectedResult = 0x00000003_33333333UL)]
        [TestCase(4, ExpectedResult = 0x00000004_44444444UL)]
        [TestCase(5, ExpectedResult = 0x00000005_55555555UL)]
        [TestCase(6, ExpectedResult = 0x00000006_66666666UL)]
        public ulong PutAllDiceTest(int diceValue)
        {
            ulong diceArray = 0;
            for (int i = 0; i < DiceArrayBits.NUM_COLUMNS; i++)
            {
                for (int j = 0; j < DiceArrayBits.NUM_ROWS; j++)
                {
                    diceArray = DiceArrayBits.PutDice(diceArray, i, j, diceValue);
                }
            }
            return diceArray;
        }

        [TestCase(0x00000003_45654321UL, 0, ExpectedResult = new int[] { 1, 2, 3 })]
        [TestCase(0x00000003_45654321UL, 1, ExpectedResult = new int[] { 4, 5, 6 })]
        [TestCase(0x00000003_45654321UL, 2, ExpectedResult = new int[] { 5, 4, 3 })]
        public int[] GetColumnTest(ulong initialBits, int col)
        {
            return DiceArrayBits.GetColumn(initialBits, col);
        }

        [TestCase(0x00000001_11311321UL, 0, ExpectedResult = new int[] { 1, 1, 1 })]
        [TestCase(0x00000001_11311321UL, 1, ExpectedResult = new int[] { 2, 2, 1 })]
        [TestCase(0x00000001_11311321UL, 2, ExpectedResult = new int[] { 3, 3, 3 })]
        public int[] GetColumnRefCountTest(ulong initialBits, int col)
        {
            return DiceArrayBits.GetColumnRefCount(initialBits, col);
        }

        [TestCase(0x00000000_00000000UL, 0, new int[] { 1, 2, 3 }, ExpectedResult = 0x00000000_00000321UL)]
        [TestCase(0x00000000_00000000UL, 1, new int[] { 4, 5, 6 }, ExpectedResult = 0x00000000_00654000UL)]
        [TestCase(0x00000000_00000000UL, 2, new int[] { 5, 4, 3 }, ExpectedResult = 0x00000003_45000000UL)]
        [TestCase(0x00000003_33333333UL, 0, new int[] { 1, 2, 3 }, ExpectedResult = 0x00000003_33333321UL)]
        [TestCase(0x00000003_33333333UL, 1, new int[] { 4, 5, 6 }, ExpectedResult = 0x00000003_33654333UL)]
        [TestCase(0x00000003_33333333UL, 2, new int[] { 5, 4, 3 }, ExpectedResult = 0x00000003_45333333UL)]
        public ulong ReplaceColumnTest(ulong initialBits, int col, int[] values)
        {
            return DiceArrayBits.ReplaceColumn(initialBits, col, values);
        }

        [TestCase(0x00000000_00000000UL, 0, ExpectedResult = 0)]
        [TestCase(0x00000000_00000001UL, 0, ExpectedResult = 1)]
        [TestCase(0x00000000_00000011UL, 0, ExpectedResult = (1 + 1) * 2)]
        [TestCase(0x00000000_00000111UL, 0, ExpectedResult = (1 + 1 + 1) * 3)]
        [TestCase(0x00000000_00001111UL, 0, ExpectedResult = (1 + 1 + 1) * 3)]
        [TestCase(0x00000000_00011111UL, 0, ExpectedResult = (1 + 1 + 1) * 3)]
        [TestCase(0x00000000_00111111UL, 0, ExpectedResult = (1 + 1 + 1) * 3)]
        [TestCase(0x00000000_01111111UL, 0, ExpectedResult = (1 + 1 + 1) * 3)]
        [TestCase(0x00000000_11111111UL, 0, ExpectedResult = (1 + 1 + 1) * 3)]
        [TestCase(0x00000001_11111111UL, 0, ExpectedResult = (1 + 1 + 1) * 3)]
        [TestCase(0x00000001_11111112UL, 0, ExpectedResult = (1 + 1) * 2 + 2)]
        [TestCase(0x00000001_11111116UL, 0, ExpectedResult = (1 + 1) * 2 + 6)]
        [TestCase(0x00000001_11111156UL, 0, ExpectedResult = 1 + 5 + 6)]
        [TestCase(0x00000001_11111456UL, 0, ExpectedResult = 4 + 5 + 6)]
        [TestCase(0x00000002_22333444UL, 0, ExpectedResult = (4 + 4 + 4) * 3)]
        [TestCase(0x00000002_22333444UL, 1, ExpectedResult = (3 + 3 + 3) * 3)]
        [TestCase(0x00000002_22333444UL, 2, ExpectedResult = (2 + 2 + 2) * 3)]
        public int GetColumnScoreTest(ulong initialBits, int col)
        {
            return DiceArrayBits.GetColumnScore(initialBits, col);
        }

        [TestCase(0x00000000_00000000UL, ExpectedResult = 0)]
        [TestCase(0x00000000_00000001UL, ExpectedResult = 1)]
        [TestCase(0x00000000_00001001UL, ExpectedResult = 1 + 1)]
        [TestCase(0x00000000_01001001UL, ExpectedResult = 1 + 1 + 1)]
        [TestCase(0x00000000_21021021UL, ExpectedResult = 1 + 2 + 1 + 2 + 1 + 2)]
        [TestCase(0x00000003_21321321UL, ExpectedResult = 1 + 2 + 3 + 1 + 2 + 3 + 1 + 2 + 3)]
        [TestCase(0x00000006_66555444UL, ExpectedResult = (4 + 4 + 4) * 3 + (5 + 5 + 5) * 3 + (6 + 6 + 6) * 3)]
        public int GetTotalScoreTest(ulong initialBits)
        {
            return DiceArrayBits.GetTotalScore(initialBits);
        }

        [TestCase(0x00000000_00000000UL, 0, ExpectedResult = 0)]
        [TestCase(0x00000000_00000000UL, 1, ExpectedResult = 0)]
        [TestCase(0x00000000_00000000UL, 2, ExpectedResult = 0)]
        [TestCase(0x00000000_00000001UL, 0, ExpectedResult = 1)]
        [TestCase(0x00000000_00000021UL, 0, ExpectedResult = 2)]
        [TestCase(0x00000000_00000321UL, 0, ExpectedResult = 3)]
        [TestCase(0x00000000_00000301UL, 0, ExpectedResult = 2)]
        [TestCase(0x00000000_00000301UL, 1, ExpectedResult = 0)]
        public int GetColumnCountTest(ulong initialBits, int col)
        {
            return DiceArrayBits.GetColumnCount(initialBits, col);
        }

        [TestCase(0x00000000_00000000UL, 0, ExpectedResult = false)]
        [TestCase(0x00000000_00000000UL, 1, ExpectedResult = false)]
        [TestCase(0x00000000_00000000UL, 2, ExpectedResult = false)]
        [TestCase(0x00000000_00000001UL, 0, ExpectedResult = false)]
        [TestCase(0x00000000_00000021UL, 0, ExpectedResult = false)]
        [TestCase(0x00000000_00000321UL, 0, ExpectedResult = true)]
        [TestCase(0x00000000_00000301UL, 0, ExpectedResult = false)]
        [TestCase(0x00000001_23123321UL, 0, ExpectedResult = true)]
        [TestCase(0x00000001_23123321UL, 1, ExpectedResult = true)]
        [TestCase(0x00000001_23123321UL, 2, ExpectedResult = true)]
        public bool IsColumnFullTest(ulong initialBits, int col)
        {
            return DiceArrayBits.IsColumnFull(initialBits, col);
        }

        [TestCase(0x00000001_23123321UL, ExpectedResult = true)]
        [TestCase(0x00000001_11111111UL, ExpectedResult = true)]
        [TestCase(0x00000000_23123321UL, ExpectedResult = false)]
        [TestCase(0x00000001_23023321UL, ExpectedResult = false)]
        [TestCase(0x00000001_23123021UL, ExpectedResult = false)]
        [TestCase(0x00000001_23023021UL, ExpectedResult = false)]
        [TestCase(0x00000000_00000000UL, ExpectedResult = false)]
        public bool IsFullTest(ulong initialBits)
        {
            return DiceArrayBits.IsFull(initialBits);
        }

        [TestCase(0x00000000_00000000UL, 0, 1, ExpectedResult = 0x00000000_00000000UL)]
        [TestCase(0x00000000_00000123UL, 0, 1, ExpectedResult = 0x00000000_00000023UL)]
        [TestCase(0x00000000_00000123UL, 0, 2, ExpectedResult = 0x00000000_00000103UL)]
        [TestCase(0x00000000_00000123UL, 0, 3, ExpectedResult = 0x00000000_00000120UL)]
        [TestCase(0x00000000_00000113UL, 0, 1, ExpectedResult = 0x00000000_00000003UL)]
        [TestCase(0x00000000_00000111UL, 0, 1, ExpectedResult = 0x00000000_00000000UL)]
        [TestCase(0x00000003_33222111UL, 1, 1, ExpectedResult = 0x00000003_33222111UL)]
        [TestCase(0x00000003_33222111UL, 1, 2, ExpectedResult = 0x00000003_33000111UL)]
        [TestCase(0x00000003_33222111UL, 2, 2, ExpectedResult = 0x00000003_33222111UL)]
        [TestCase(0x00000003_33222111UL, 2, 3, ExpectedResult = 0x00000000_00222111UL)]
        [TestCase(0x00000003_33333333UL, 2, 3, ExpectedResult = 0x00000000_00333333UL)]
        public ulong RemoveByValueTest(ulong initialBits, int col, int value)
        {
            return DiceArrayBits.RemoveByValue(initialBits, col, value);
        }

        [TestCase(0x00000000_00000000UL, ExpectedResult = 0x00000000_00000000UL)]
        [TestCase(0x00000000_00000010UL, ExpectedResult = 0x00000000_00000001UL)]
        [TestCase(0x00000000_00200010UL, ExpectedResult = 0x00000000_00002001UL)]
        [TestCase(0x00000003_00200010UL, ExpectedResult = 0x00000000_03002001UL)]
        [TestCase(0x00000003_00200100UL, ExpectedResult = 0x00000000_03002001UL)]
        [TestCase(0x00000003_00020100UL, ExpectedResult = 0x00000000_03002001UL)]
        [TestCase(0x00000000_03020100UL, ExpectedResult = 0x00000000_03002001UL)]
        [TestCase(0x00000000_03020140UL, ExpectedResult = 0x00000000_03002014UL)]
        [TestCase(0x00000000_03520140UL, ExpectedResult = 0x00000000_03052014UL)]
        [TestCase(0x00000006_03520140UL, ExpectedResult = 0x00000000_63052014UL)]
        [TestCase(0x00000000_63502104UL, ExpectedResult = 0x00000000_63052014UL)]
        public ulong LeftJustifyTest(ulong initialBits)
        {
            return DiceArrayBits.LeftJustify(initialBits);
        }


    }
}

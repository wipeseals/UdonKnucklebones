using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using System;


public static class DiceArrayBits
{
    #region Constants
    /// <summary>
    /// サイコロを並べられる行数
    /// </summary>
    public const int NUM_COLUMNS = 3;
    /// <summary>
    /// サイコロを並べられる列数
    /// </summary>
    public const int NUM_ROWS = 3;

    /// <summary>
    /// サイコロ1個あたりのビット幅
    /// </summary>
    public const int DICE_BIT_WIDTH = 4; // 0~6が表現できれば良い

    /// <summary>
    /// サイコロの値取得用のBitMask
    /// </summary>
    /// <remarks>
    /// 0b1111 = 0x0F = 15, 0~6の値を取得するためのマスク
    /// DICE_BIT_WIDTHに合わせて変更する
    /// </remarks>
    public const ulong DICE_BIT_MASK = 0b1111;

    /// <summary>
    /// サイコロの無効地
    /// </summary>
    public const int INVALID_DICE_VALUE = 0;

    /// <summary>
    /// サイコロの最小値
    /// </summary>
    public const int MIN_DICE_VALUE = 1;

    /// <summary>
    /// サイコロの最大値
    /// </summary>
    public const int MAX_DICE_VALUE = 6;
    #endregion


    /// <summary>
    /// サイコロの配置から通し番号を取得する。LSBから列0行0, 列0行1, 列0行2, 列1行0, ... となる
    public static int GetDiceIndex(int col, int row) => col * NUM_ROWS + row;

    /// <summary>
    /// サイコロの配置からサイコロの値を取得する
    /// </summary>
    public static int GetDice(this ulong bits, int col, int row)
    {
        var index = GetDiceIndex(col, row);
        var bitOffset = index * DICE_BIT_WIDTH;

        return (int)((bits >> bitOffset) & DICE_BIT_MASK);
    }

    /// <summary>
    /// サイコロの配置を更新した値を返す
    /// </summary>
    public static ulong PutDice(this ulong bits, int col, int row, int value)
    {
        var index = GetDiceIndex(col, row);
        var bitOffset = index * DICE_BIT_WIDTH;

        // 一度クリアしてからセットする
        bits &= ~(DICE_BIT_MASK << bitOffset);
        bits |= ((ulong)value & DICE_BIT_MASK) << bitOffset;

        return bits;
    }

    /// <summary>
    /// 列単位でサイコロの配置を取得する
    /// </summary>
    public static int[] GetColumn(this ulong bits, int col)
    {
        var values = new int[NUM_ROWS];
        for (var row = 0; row < NUM_ROWS; row++)
        {
            values[row] = GetDice(bits, col, row);
        }
        return values;
    }

    /// <summary>
    /// 列内のサイコロが何個あるかを取得する
    /// </summary>
    public static int[] GetColumnRefCount(this int[] values)
    {
        var refCounts = new int[values.Length];
        for (var i = 0; i < refCounts.Length; i++)
        {
            refCounts[i] = 0;
        }

        for (int diceNum = MIN_DICE_VALUE; diceNum <= MAX_DICE_VALUE; diceNum++)
        {
            // 同じ値の数
            var counts = 0;
            foreach (var value in values)
            {
                if (value == diceNum)
                {
                    counts++;
                }
            }
            // 書いておく
            for (var row = 0; row < refCounts.Length; row++)
            {
                if (values[row] == diceNum)
                {
                    refCounts[row] = counts;
                }
            }
        }
        return refCounts;
    }

    /// <summary>
    /// 列単位でサイコロの配置を更新した値を返す
    /// </summary>
    public static ulong ReplaceColumn(this ulong bits, int col, int[] values)
    {
        for (var row = 0; row < NUM_ROWS; row++)
        {
            bits = PutDice(bits, col, row, values[row]);
        }
        return bits;
    }

    /// <summary>
    /// 列内のスコアを計算する
    /// サイコロの名の合計値だが、同じ値が複数ある場合その数分だけ乗算する
    /// </summary>
    public static int GetColumnScore(this ulong bits, int col)
    {
        var values = bits.GetColumn(col);
        var refCounts = GetColumnRefCount(values);

        // スコア計算本体
        // value * refCountしておけば、複数個ある場合の計算が楽
        // e.g. 2,5,2 => 2*2 + 5*1 + 2*2 = 13 = ((2+2)*2) + 5
        var score = 0;
        for (var i = 0; i < values.Length; i++)
        {
            score += values[i] * refCounts[i];
        }
        return score;
    }

    /// <summary>
    /// 全体のスコアを計算する
    /// </summary>
    public static int GetTotalScore(this ulong bits)
    {
        var scores = 0;
        for (var col = 0; col < NUM_COLUMNS; col++)
        {
            scores += bits.GetColumnScore(col);
        }
        return scores;
    }

    /// <summary>
    /// 配置済のサイコロの数を取得する
    /// </summary>
    public static int GetColumnCount(this ulong bits, int col)
    {
        var count = 0;
        for (var row = 0; row < NUM_ROWS; row++)
        {
            if (GetDice(bits, col, row) != INVALID_DICE_VALUE)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 指定された列が満杯かどうかを判定する
    /// </summary>
    public static bool IsColumnFull(this ulong bits, int col)
    {
        return GetColumnCount(bits, col) >= NUM_ROWS;
    }

    /// <summary>
    /// サイコロを置ける場所がなくなっていたらTrueを返す
    /// </summary>
    public static bool IsFull(this ulong bits)
    {
        for (var col = 0; col < NUM_COLUMNS; col++)
        {
            if (!bits.IsColumnFull(col))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 指定された列の指定された値を削除する
    /// Playerがサイコロを配置時、相手の列に同じ値がある場合、その値を削除するために使用する
    /// 空白を詰めることは行わない
    /// </summary>
    public static ulong RemoveByValue(this ulong bits, int col, int value)
    {
        // 列の値を取得
        var values = GetColumn(bits, col);
        // 指定された値を削除
        for (var row = 0; row < NUM_ROWS; row++)
        {
            if (values[row] == value)
            {
                values[row] = INVALID_DICE_VALUE;
                break;
            }
        }

        // 列の値を更新したbitを返す
        var newBits = bits.ReplaceColumn(col, values);
        return newBits;
    }

    /// <summary>
    /// 0をまえづめして返す
    /// </summary>
    /// <param name="bits"></param>
    /// <returns></returns>
    public static ulong LeftJustify(this ulong bits)
    {
        var newBits = 0UL;
        for (var col = 0; col < NUM_COLUMNS; col++)
        {
            var values = GetColumn(bits, col);
            var newValues = new int[NUM_ROWS];
            var newIndex = 0;
            for (var row = 0; row < NUM_ROWS; row++)
            {
                if (values[row] != INVALID_DICE_VALUE)
                {
                    newValues[newIndex] = values[row];
                    newIndex++;
                }
            }
            newBits = newBits.ReplaceColumn(col, newValues);
        }
        return newBits;
    }

}
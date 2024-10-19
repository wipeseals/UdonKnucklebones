// UdonChips対応を有効にする場合は定義する
// #define UDON_KNUCKLEBONES_SUPPORT_UDONCHIPS

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

using Wipeseals;
using UCS;

public class UdonKnucklebonesSupportUdonChips : UdonKnucklebones
{
    #region UdonChips Event
    /// <summary>
    /// 所持金を取得し同期変数に設定
    /// </summary>
    public override void OnUpdateCurrentUdonChips()
    {
        Log(ErrorLevel.Info, $"{nameof(OnUpdateCurrentUdonChips)}");

        var money = GameObject.Find("UdonChips").GetComponent<UdonChips>().money;
        if (IsMyselfPlayer1)
        {
            Player1UdonChips = money;
        }
        else if (IsMyselfPlayer2)
        {
            Player2UdonChips = money;
        }

        Log(ErrorLevel.Info, $"Player1UdonChips={Player1UdonChips} Player2UdonChips={Player2UdonChips}");
    }

    /// <summary>
    /// 勝敗の金額を反映。ローカル処理
    /// </summary>
    public override void OnApplyUdonChips()
    {
        Log(ErrorLevel.Info, $"{nameof(OnApplyUdonChips)}");

        // UdonChips自体はLocalなのでOwnerでなくても問題ない

        // 取引金額を計算
        var player1Score = GetDiceArrayBits(PLAYER1).GetTotalScore();
        var player2Score = GetDiceArrayBits(PLAYER2).GetTotalScore();
        var scoreDiff = (player1Score > player2Score) ? player1Score - player2Score : player2Score - player1Score;

        var ratio = ((Player1Type == (int)PlayerType.CPU || Player2Type == (int)PlayerType.CPU) ? UdonChipsCpuRate : UdonChipsPlayerRate);
        var applyMoney = scoreDiff * ratio;

        // 負けたPlayerが支払えないケースでは残金全てに設定。CPUの場合は全額のまま
        if (CurrentGameJudge == (int)GameJudge.Player1Win && Player1Type == (int)PlayerType.Human && Player2UdonChips < applyMoney)
        {
            applyMoney = Player2UdonChips;
        }
        else if (CurrentGameJudge == (int)GameJudge.Player2Win && Player2Type == (int)PlayerType.Human && Player1UdonChips < applyMoney)
        {
            applyMoney = Player1UdonChips;
        }

        // 取引。それぞれのローカルでmoneyを更新
        if (IsMyselfPlayer1 && IsMyselfPlayer2)
        {
            // 自分自身との対戦の場合は何もしない
            Log(ErrorLevel.Info, $"Player1 == Player2, Do nothing");

        }
        else if (IsMyselfPlayer1)
        {
            if (CurrentGameJudge == (int)GameJudge.Player1Win)
            {
                GameObject.Find("UdonChips").GetComponent<UdonChips>().money += applyMoney;
            }
            else if (CurrentGameJudge == (int)GameJudge.Player2Win)
            {
                GameObject.Find("UdonChips").GetComponent<UdonChips>().money -= applyMoney; // 事前に支払えないケースの対応は済んでいるので、ここではそのまま減算
            }
        }
        else if (IsMyselfPlayer2)
        {
            if (CurrentGameJudge == (int)GameJudge.Player2Win)
            {
                GameObject.Find("UdonChips").GetComponent<UdonChips>().money += applyMoney;
            }
            else if (CurrentGameJudge == (int)GameJudge.Player1Win)
            {
                GameObject.Find("UdonChips").GetComponent<UdonChips>().money -= applyMoney; // 事前に支払えないケースの対応は済んでいるので、ここではそのまま減算
            }
        }

        Log(ErrorLevel.Info, $"Player1Score={player1Score} Player2Score={player2Score} ScoreDiff={scoreDiff} ApplyMoney={applyMoney}");
    }
    #endregion

}

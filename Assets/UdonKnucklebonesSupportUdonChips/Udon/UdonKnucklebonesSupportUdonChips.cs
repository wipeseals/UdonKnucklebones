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
    /// <summary>
    /// 取引されたならtrue
    /// </summary>
    bool _isPaid = false;
    /// <summary>
    /// 取引レート
    /// </summary>
    float _rate = 0.0f;
    /// <summary>
    /// 取引されたUdonChipsの金額
    /// </summary>
    float _applyMoney = 0.0f;

    #region Synced Properties Accessor
    /// <summary>
    /// システムメッセージを取得。UI反映用のメッセージ
    /// </summary>
    /// <returns></returns>
    public override string GetSystemMessage()
    {
        switch ((GameProgress)Progress)
        {
            case GameProgress.Initial:
                return "Booting...";
            case GameProgress.WaitEnterPlayers:
                return "Waiting for Enter Players";
            case GameProgress.GameStart:
                return "Game Start!";
            case GameProgress.WaitPlayer1Roll:
                return "Player1: Roll the dice!";
            case GameProgress.Player1Rolling:
                return "Player1: Rolling the dice...";
            case GameProgress.WaitPlayer1Put:
                return $"Player1: Put the dice '{RolledDiceValue}'!";
            case GameProgress.WaitPlayer1Calc:
                return "Player1: Calculating...";
            case GameProgress.WaitPlayer2Roll:
                return "Player2: Roll the dice!";
            case GameProgress.Player2Rolling:
                return "Player2: Rolling the dice...";
            case GameProgress.WaitPlayer2Put:
                return $"Player2: Put the dice '{RolledDiceValue}'!";
            case GameProgress.WaitPlayer2Calc:
                return "Player2: Calculating...";
            case GameProgress.GameEnd:
                if (_isPaid)
                {
                    switch ((GameJudge)CurrentGameJudge)
                    {
                        case GameJudge.Player1Win:
                            return $"Player1 Win! {_applyMoney} chips paid!\n(diff in pt. x {_rate})";
                        case GameJudge.Player2Win:
                            return $"Player2 Win! {_applyMoney} chips paid!\n(diff in pt. x {_rate})";
                        case GameJudge.Draw:
                            return "Draw!";
                        default:
                            return "";
                    }
                }
                else
                {
                    switch ((GameJudge)CurrentGameJudge)
                    {
                        case GameJudge.Player1Win:
                            return "Player1 Win!";
                        case GameJudge.Player2Win:
                            return "Player2 Win!";
                        case GameJudge.Draw:
                            return "Draw!";
                        default:
                            return "";
                    }
                }
            case GameProgress.Aborted:
                return "Game Aborted!";
            case GameProgress.ConfigurationError:
                return "Configuration Error!";
            default:
                break;
        }
        return "";
    }
    #endregion

    #region UdonChips Event
    /// <summary>
    /// 所持金を取得し同期変数に設定
    /// </summary>
    public override void OnUpdateCurrentUdonChips()
    {
        Log(ErrorLevel.Info, $"{nameof(OnUpdateCurrentUdonChips)}");

        // 各Playerの金額更新が起きている間は決着していない
        _isPaid = false;

        // Scene中に配置されたUdonChipsを取得
        var go = GameObject.Find("UdonChips");
        if (go == null)
        {
            Log(ErrorLevel.Error, $"UdonChips is not found");
            return;
        }
        var uc = go.GetComponent<UdonChips>();


        var money = uc.money;
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

        // Scene中に配置されたUdonChipsを取得
        var go = GameObject.Find("UdonChips");
        if (go == null)
        {
            Log(ErrorLevel.Error, $"UdonChips is not found");
            return;
        }
        var uc = go.GetComponent<UdonChips>();

        // UdonChips自体はLocalなのでOwnerでなくても問題ない

        // 取引金額を計算
        var player1Score = GetDiceArrayBits(PLAYER1).GetTotalScore();
        var player2Score = GetDiceArrayBits(PLAYER2).GetTotalScore();
        var scoreDiff = (player1Score > player2Score) ? player1Score - player2Score : player2Score - player1Score;

        var enterCpu = (Player1Type == (int)PlayerType.CPU || Player2Type == (int)PlayerType.CPU);
        var rate = (enterCpu ? UdonChipsCpuRate : UdonChipsPlayerRate);

        // 取引金額計算結果は表示でも使う
        _isPaid = true;
        _rate = rate;
        _applyMoney = scoreDiff * rate;

        // 負けたPlayerが支払えないケースでは残金全てに設定。CPUの場合は全額のまま
        if (!enterCpu)
        {
            if (CurrentGameJudge == (int)GameJudge.Player1Win)
            {
                // Player1が買ったがPlayer2が支払えない場合
                if (Player2UdonChips < _applyMoney)
                {
                    _applyMoney = Player2UdonChips;
                }
            }
            else if (CurrentGameJudge == (int)GameJudge.Player2Win)
            {
                // Player2が買ったがPlayer1が支払えない場合
                if (Player1UdonChips < _applyMoney)
                {
                    _applyMoney = Player1UdonChips;
                }
            }
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
                uc.money += (float)_applyMoney;
            }
            else if (CurrentGameJudge == (int)GameJudge.Player2Win)
            {
                uc.money -= (float)_applyMoney; // 事前に支払えないケースの対応は済んでいるので、ここではそのまま減算
            }
        }
        else if (IsMyselfPlayer2)
        {
            if (CurrentGameJudge == (int)GameJudge.Player2Win)
            {
                uc.money += (float)_applyMoney;
            }
            else if (CurrentGameJudge == (int)GameJudge.Player1Win)
            {
                uc.money -= (float)_applyMoney; // 事前に支払えないケースの対応は済んでいるので、ここではそのまま減算
            }
        }

        Log(ErrorLevel.Info, $"Player1Score={player1Score} Player2Score={player2Score} ScoreDiff={scoreDiff} ApplyMoney={_applyMoney}");

        // OnApplyUdonChips はAllUserに通知されるだけのイベントで変数同期がない。UI反映のために手動でUI更新
        OnUIUpdate();
    }
    #endregion

}


using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using System;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// エラーレベル
/// </summary>
enum ErrorLevel
{
    Info,
    Warning,
    Error
};

/// <summary>
/// ゲームの勝敗状態
/// </summary>
enum GameJudge
{
    Player1Win,
    Player2Win,
    Draw,
    Continue
}

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class UdonKnucklebones : UdonSharpBehaviour
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

    /// <summary>
    /// Player1
    /// </summary>
    public const int PLAYER1 = 0;

    /// <summary>
    /// Player2
    /// </summary>
    public const int PLAYER2 = 1;

    #endregion

    #region Properties

    [Header("Udon Knucklebones")]

    [Header("for Debugging")]
    [SerializeField, Tooltip("デバッグモード")]
    public bool IsDebug = true;

    [Header("Dice Objects")]
    [SerializeField, Tooltip("転がす準備中に見せるサイコロ")]
    public Animator DiceForReady = null;

    [SerializeField, Tooltip("実際に転がるサイコロ")]
    public GameObject DiceForRoll = null;

    [SerializeField, Tooltip("サイコロを転がす際にUseするCollider")]
    public Collider DiceRollCollider = null;

    [Header("Selection Objects")]
    [SerializeField, Tooltip("Player1がサイコロを配置するときに列選択するためのCollider")]
    public Collider[] Player1ColumnColliders = null;

    [SerializeField, Tooltip("Player2がサイコロを配置するときに列選択するためのCollider")]
    public Collider[] Player2ColumnColliders = null;

    [Header("Dice Arrays")]
    [SerializeField, Tooltip("Player1の1列目のサイコロ格納用")]
    public Animator[] Player1Col1DiceArray = null;

    [SerializeField, Tooltip("Player1の2列目のサイコロ格納用")]
    public Animator[] Player1Col2DiceArray = null;

    [SerializeField, Tooltip("Player1の3列目のサイコロ格納用")]
    public Animator[] Player1Col3DiceArray = null;

    [SerializeField, Tooltip("Player2の1列目のサイコロ格納用")]
    public Animator[] Player2Col1DiceArray = null;

    [SerializeField, Tooltip("Player2の2列目のサイコロ格納用")]
    public Animator[] Player2Col2DiceArray = null;

    [SerializeField, Tooltip("Player2の3列目のサイコロ格納用")]
    public Animator[] Player2Col3DiceArray = null;

    [Header("Score Texts")]
    [SerializeField, Tooltip("Player1の各列のスコア表示用")]
    public TextMeshProUGUI[] Player1ColumnScoreTexts = null;

    [SerializeField, Tooltip("Player2の各列のスコア表示用")]
    public TextMeshProUGUI[] Player2ColumnScoreTexts = null;

    [Header("Info Texts")]
    [SerializeField, Tooltip("Player1のメインスコア表示用")]
    public TextMeshProUGUI Player1MainScoreText = null;

    [SerializeField, Tooltip("Player2のメインスコア表示用")]
    public TextMeshProUGUI Player2MainScoreText = null;

    [SerializeField, Tooltip("現在のターン表示用")]
    public TextMeshProUGUI TurnText = null;

    [SerializeField, Tooltip("システムメッセージ表示用")]
    public TextMeshProUGUI SystemText = null;

    [Header("Buttons")]
    [SerializeField, Tooltip("Player1参加ボタン")]
    public Button Player1EntryButton = null;

    [SerializeField, Tooltip("Player1離脱ボタン")]
    public Button Player1LeaveButton = null;

    [SerializeField, Tooltip("Player1 CPU参加ボタン")]
    public Button Player1CPUEntryButton = null;

    [SerializeField, Tooltip("Player2参加ボタン")]
    public Button Player2EntryButton = null;

    [SerializeField, Tooltip("Player2離脱ボタン")]
    public Button Player2LeaveButton = null;

    [SerializeField, Tooltip("Player2 CPU参加ボタン")]
    public Button Player2CPUEntryButton = null;

    [SerializeField, Tooltip("リセットボタン")]
    public Button ResetButton = null;

    #endregion

    #region Synced Properties

    [Header("Synced Properties")]

    /// <summary>
    /// 現在のターン数
    /// </summary>
    [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(CurrentTurn))]
    public int _currentTurn = 1;

    /// <summary>
    /// 現在のターン数
    /// </summary>
    public int CurrentTurn
    {
        get => _currentTurn;
        set
        {
            _currentTurn = value;
            OnUpdateSyncedProperties();
        }
    }

    /// <summary>
    /// 現在のプレイヤー (0 or 1)
    /// </summary>
    [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(CurrentPlayer))]
    public int _currentPlayer = 1;

    /// <summary>
    /// 現在のプレイヤー (0 or 1)
    /// </summary>
    public int CurrentPlayer
    {
        get => _currentPlayer;
        set
        {
            _currentPlayer = value;
            OnUpdateSyncedProperties();
        }
    }

    /// <summary>
    /// Player1のサイコロ配置(生データ)
    /// </summary>
    [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(Player1DiceArrayBits))]
    public ulong _player1DiceArrayBits = 0;

    /// <summary>
    /// Player1のサイコロ配置(生データ)
    /// </summary>
    public ulong Player1DiceArrayBits
    {
        get => _player1DiceArrayBits;
        set
        {
            _player1DiceArrayBits = value;
            OnUpdateSyncedProperties();
        }
    }

    /// <summary>
    /// Player2のサイコロ配置(生データ)
    /// </summary>
    [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(Player2DiceArrayBits))]
    public ulong _player2DiceArrayBits = 0;

    /// <summary>
    /// Player2のサイコロ配置(生データ)
    /// </summary>
    public ulong Player2DiceArrayBits
    {
        get => _player2DiceArrayBits;
        set
        {
            _player2DiceArrayBits = value;
            OnUpdateSyncedProperties();
        }
    }
    #endregion

    #region Event for Sync

    /// <summary>
    /// Synced Propertiesが更新されたときに呼び出される。UIの更新を行う
    /// </summary>
    public void OnUpdateSyncedProperties()
    {
        TurnText.text = $"Turn: {CurrentTurn:D3}";
        for (var col = 0; col < NUM_COLUMNS; col++)
        {
            Player1ColumnScoreTexts[col].text = $"{GetDiceArrayScoreByColumn(Player1DiceArrayBits, col):D02}";
            Player2ColumnScoreTexts[col].text = $"{GetDiceArrayScoreByColumn(Player2DiceArrayBits, col):D02}";
        }
        Player1MainScoreText.text = $"Player1: {GetDiceArrayTotalScore(Player1DiceArrayBits):D03} pt.";
        Player2MainScoreText.text = $"Player2: {GetDiceArrayTotalScore(Player2DiceArrayBits):D03} pt.";

        // TODO: 参加ボタンやら勝敗やらサイコロフリのステータスやら

    }
    #endregion

    #region Utility for Synced Properties

    /// <summary>
    /// Ownerだったらtrueを返す
    /// </summary>
    public bool IsOwner
    {
        get
        {
            var player = Networking.LocalPlayer;
            // debug時などは取得できないのでOwner扱いにする
            if (player == null)
            {
                return true;
            }
            return player.IsOwner(this.gameObject);
        }
    }

    /// <summary>
    /// Owner取得
    /// </summary>
    public void ChangeOwner()
    {
        // Ownerだったら何もしない
        if (IsOwner) return;

        // Unity debug時などはLocalPlayerが取得できない
        var player = Networking.LocalPlayer;
        if (player == null) return;

        Networking.SetOwner(player, this.gameObject);
    }

    /// <summary>
    /// Synced Propertiesを手動で更新する
    /// </summary>
    public void ManualSync()
    {
        RequestSerialization();
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(OnUpdateSyncedProperties));
    }

    /// <summary>
    /// 動機変数を全て初期化する
    /// </summary>
    void ResetSyncedProperties()
    {
        // Ownerのみが変更できる。Ownerでなければ取得
        if (!IsOwner)
        {
            ChangeOwner();
        }

        // Accessor経由で変更してからRequestSerialization
        CurrentTurn = 1;
        CurrentPlayer = 1;
        Player1DiceArrayBits = 0;
        Player2DiceArrayBits = 0;

        ManualSync();
    }

    #region DiceArrayBits Accessor

    /// <summary>
    /// プレイヤーのサイコロ配置を取得
    /// </summary>
    ulong GetDiceArrayBits(int player) => player == 0 ? Player1DiceArrayBits : Player2DiceArrayBits;

    /// <summary>
    /// プレイヤーのサイコロ配置を更新
    /// </summary>
    void UpdateDiceArrayBits(int player, ulong bits)
    {
        if (player == PLAYER1)
        {
            Player1DiceArrayBits = bits;
        }
        else if (player == PLAYER2)
        {
            Player2DiceArrayBits = bits;
        }
        else
        {
            Log(ErrorLevel.Error, $"Invalid player number: {player}");
        }
    }

    /// <summary>
    /// プレイヤーのサイコロ配置を取得
    /// </summary>
    int GetDiceArrayValue(int player, int col, int row) => GetDiceArrayValue(GetDiceArrayBits(player), col, row);

    /// <summary>
    /// プレイヤーのサイコロ配置を更新
    /// </summary>
    void UpdateDiceValue(int player, int col, int row, int data)
    {
        var srcBits = GetDiceArrayBits(player);
        var dstBits = ApplyDiceArrayValue(srcBits, col, row, data);
        UpdateDiceArrayBits(player, dstBits);
    }

    /// <summary>
    /// プレイヤーのサイコロ配置を列単位で取得
    /// </summary>
    int[] GetDiceArrayValueByColumn(int player, int col) => GetDiceArrayValueByColumn(GetDiceArrayBits(player), col);

    /// <summary>
    /// プレイヤーのサイコロ配置を列単位で更新
    /// </summary>
    void UpdateDiceArrayValueByColumn(int player, int col, int[] values)
    {
        var srcBits = GetDiceArrayBits(player);
        var dstBits = ApplyDiceArrayBitsByColumn(srcBits, col, values);
        UpdateDiceArrayBits(player, dstBits);
    }

    /// <summary>
    /// プレイヤーの列内のサイコロ数を取得
    /// </summary>
    int[] GetDiceArrayRefCountByColumn(int player, int col) => GetDiceArrayRefCountByColumn(GetDiceArrayValueByColumn(player, col));

    /// <summary>
    /// プレイヤーの列内のスコアを取得
    /// </summary>
    int GetDiceArrayScoreByColumn(int player, int col) => GetDiceArrayScoreByColumn(GetDiceArrayBits(player), col);

    /// <summary>
    /// プレイヤーの全体のスコアを取得
    /// </summary>
    int GetDiceArrayTotalScore(int player) => GetDiceArrayTotalScore(GetDiceArrayBits(player));

    /// <summary>
    /// プレイヤーのサイコロ配置の数を取得
    /// </summary>
    int GetDiceArrayCountByColumn(int player, int col) => GetDiceArrayCountByColumn(GetDiceArrayBits(player), col);

    /// <summary>
    /// プレイヤーが指定された列にサイコロを配置できるならTrueを返す
    /// 行数分。最大3個まで配置可能
    /// </summary>
    bool CanPlaceDiceByColumn(int player, int col) => GetDiceArrayCountByColumn(player, col) < NUM_ROWS;

    /// <summary>
    /// プレイヤーがサイコロを配置できない場合Trueを返す
    /// </summary>
    bool IsNoPlaceToPutDice(int player) => IsNoPlaceToPutDice(GetDiceArrayBits(player));

    /// <summary>
    /// サイコロを配置する
    /// </summary>
    void PlaceDice(int player, int col, int value)
    {
        // 指定された列の末尾に配置
        var count = GetDiceArrayCountByColumn(player, col);
        if (count >= NUM_ROWS)
        {
            Log(ErrorLevel.Error, $"Cannot place dice to column {col}");
            return;
        }
        UpdateDiceValue(player, col, count, value);

        // 相手の列に同じ値がある場合、削除する
        // 列のindexは向かい合っているため、NUM_COLUMNS - col - 1で対向している列を取得
        var opponentPlayer = player == 0 ? 1 : 0;
        var opponentColumn = NUM_COLUMNS - col - 1;
        // 相手の列から配置した値を削除
        var opponentValues = RemoveSpecifiedDiceValueByColumn(GetDiceArrayBits(opponentPlayer), opponentColumn, value);
        UpdateDiceArrayBits(opponentPlayer, opponentValues);
    }

    /// <summary>
    /// ゲームの勝敗を取得
    /// </summary>
    GameJudge GetGameJudge(int currentTurnPlayer)
    {
        // 現在のターンのプレイヤーが配置後、置けなくなった時点で終了
        if (!IsNoPlaceToPutDice(currentTurnPlayer))
        {
            // まだ置ける
            return GameJudge.Continue;
        }

        var player1Score = GetDiceArrayTotalScore(PLAYER1);
        var player2Score = GetDiceArrayTotalScore(PLAYER2);

        if (player1Score > player2Score)
        {
            return GameJudge.Player1Win;
        }
        else if (player1Score < player2Score)
        {
            return GameJudge.Player2Win;
        }
        else
        {
            return GameJudge.Draw;
        }
    }

    #endregion

    #endregion

    #region Utility for Misc
    /// <summary>
    /// Inspectorの設定が完了しているか確認する
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    bool IsFinishedInspectorSetting(out string msg)
    {
        // Check if all required fields are set
        if (DiceForReady == null)
        {
            msg = $"{nameof(DiceForReady)} is not set!";
            return false;
        }
        if (DiceForRoll == null)
        {
            msg = $"{nameof(DiceForRoll)} is not set!";
            return false;
        }
        if (DiceRollCollider == null)
        {
            msg = $"{nameof(DiceRollCollider)} is not set!";
            return false;
        }
        if (Player1ColumnColliders == null || Player1ColumnColliders.Length == 0)
        {
            msg = $"{nameof(Player1ColumnColliders)} is not set!";
            return false;
        }
        if (Player2ColumnColliders == null || Player2ColumnColliders.Length == 0)
        {
            msg = $"{nameof(Player2ColumnColliders)} is not set!";
            return false;
        }
        if (Player1Col1DiceArray == null || Player1Col1DiceArray.Length == 0)
        {
            msg = $"{nameof(Player1Col1DiceArray)} is not set!";
            return false;
        }
        if (Player1Col2DiceArray == null || Player1Col2DiceArray.Length == 0)
        {
            msg = $"{nameof(Player1Col2DiceArray)} is not set!";
            return false;
        }
        if (Player1Col3DiceArray == null || Player1Col3DiceArray.Length == 0)
        {
            msg = $"{nameof(Player1Col3DiceArray)} is not set!";
            return false;
        }
        if (Player2Col1DiceArray == null || Player2Col1DiceArray.Length == 0)
        {
            msg = $"{nameof(Player2Col1DiceArray)} is not set!";
            return false;
        }
        if (Player2Col2DiceArray == null || Player2Col2DiceArray.Length == 0)
        {
            msg = $"{nameof(Player2Col2DiceArray)} is not set!";
            return false;
        }
        if (Player2Col3DiceArray == null || Player2Col3DiceArray.Length == 0)
        {
            msg = $"{nameof(Player2Col3DiceArray)} is not set!";
            return false;
        }
        if (Player1ColumnScoreTexts == null || Player1ColumnScoreTexts.Length == 0)
        {
            msg = $"{nameof(Player1ColumnScoreTexts)} is not set!";
            return false;
        }
        if (Player2ColumnScoreTexts == null || Player2ColumnScoreTexts.Length == 0)
        {
            msg = $"{nameof(Player2ColumnScoreTexts)} is not set!";
            return false;
        }
        if (Player1MainScoreText == null)
        {
            msg = $"{nameof(Player1MainScoreText)} is not set!";
            return false;
        }
        if (Player2MainScoreText == null)
        {
            msg = $"{nameof(Player2MainScoreText)} is not set!";
            return false;
        }
        if (TurnText == null)
        {
            msg = $"{nameof(TurnText)} is not set!";
            return false;
        }
        if (SystemText == null)
        {
            msg = $"{nameof(SystemText)} is not set!";
            return false;
        }
        if (Player1EntryButton == null)
        {
            msg = $"{nameof(Player1EntryButton)} is not set!";
            return false;
        }
        if (Player1LeaveButton == null)
        {
            msg = $"{nameof(Player1LeaveButton)} is not set!";
            return false;
        }
        if (Player1CPUEntryButton == null)
        {
            msg = $"{nameof(Player1CPUEntryButton)} is not set!";
            return false;
        }
        if (Player2EntryButton == null)
        {
            msg = $"{nameof(Player2EntryButton)} is not set!";
            return false;
        }
        if (Player2LeaveButton == null)
        {
            msg = $"{nameof(Player2LeaveButton)} is not set!";
            return false;
        }
        if (Player2CPUEntryButton == null)
        {
            msg = $"{nameof(Player2CPUEntryButton)} is not set!";
            return false;
        }
        if (ResetButton == null)
        {
            msg = $"{nameof(ResetButton)} is not set!";
            return false;
        }

        // Check if all required fields in the dice arrays are set
        if (Player1ColumnColliders.Length != NUM_COLUMNS)
        {
            msg = $"{nameof(Player1ColumnColliders)} must have {NUM_COLUMNS} elements!";
            return false;
        }
        if (Player2ColumnColliders.Length != NUM_COLUMNS)
        {
            msg = $"{nameof(Player2ColumnColliders)} must have {NUM_COLUMNS} elements!";
            return false;
        }
        if (Player1Col1DiceArray.Length != NUM_ROWS)
        {
            msg = $"{nameof(Player1Col1DiceArray)} must have {NUM_ROWS} elements!";
            return false;
        }
        if (Player1Col2DiceArray.Length != NUM_ROWS)
        {
            msg = $"{nameof(Player1Col2DiceArray)} must have {NUM_ROWS} elements!";
            return false;
        }
        if (Player1Col3DiceArray.Length != NUM_ROWS)
        {
            msg = $"{nameof(Player1Col3DiceArray)} must have {NUM_ROWS} elements!";
            return false;
        }
        if (Player2Col1DiceArray.Length != NUM_ROWS)
        {
            msg = $"{nameof(Player2Col1DiceArray)} must have {NUM_ROWS} elements!";
            return false;
        }
        if (Player2Col2DiceArray.Length != NUM_ROWS)
        {
            msg = $"{nameof(Player2Col2DiceArray)} must have {NUM_ROWS} elements!";
            return false;
        }
        if (Player2Col3DiceArray.Length != NUM_ROWS)
        {
            msg = $"{nameof(Player2Col3DiceArray)} must have {NUM_ROWS} elements!";
            return false;
        }
        if (Player1ColumnScoreTexts.Length != NUM_COLUMNS)
        {
            msg = $"{nameof(Player1ColumnScoreTexts)} must have {NUM_COLUMNS} elements!";
            return false;
        }
        if (Player2ColumnScoreTexts.Length != NUM_COLUMNS)
        {
            msg = $"{nameof(Player2ColumnScoreTexts)} must have {NUM_COLUMNS} elements!";
            return false;
        }


        // All checks passed
        msg = "Setup is complete!";
        return true;
    }

    /// <summary>
    /// すべての状態をリセットする
    /// </summary>
    void ResetAllState()
    {
        // DiceForReadyのAnimationをリセット
        DiceForReady.gameObject.SetActive(true);
        DiceForReady.SetBool("IsReady", true);

        // DiceForRollの位置をリセットして非表示で待機状態にする
        DiceForRoll.transform.position = DiceForReady.transform.position;
        DiceForRoll.SetActive(false);

        // DiceRollColliderを無効化
        DiceRollCollider.enabled = false;

        // Player1 Column Collidersを無効化
        foreach (var collider in Player1ColumnColliders)
        {
            collider.enabled = false;
        }

        // Player2 Column Collidersを無効化
        foreach (var collider in Player2ColumnColliders)
        {
            collider.enabled = false;
        }

        // Player1のサイコロをAnimation Controllerから非表示に設定する
        foreach (var diceArray in new[] { Player1Col1DiceArray, Player1Col2DiceArray, Player1Col3DiceArray })
        {
            foreach (var dice in diceArray)
            {
                dice.gameObject.SetActive(true);
                dice.SetInteger("Number", 0);
                dice.SetInteger("RefCount", 0);
            }
        }
        // Player2のサイコロをAnimation Controllerから非表示に設定する
        foreach (var diceArray in new[] { Player2Col1DiceArray, Player2Col2DiceArray, Player2Col3DiceArray })
        {
            foreach (var dice in diceArray)
            {
                dice.gameObject.SetActive(true);
                dice.SetInteger("Number", 0);
                dice.SetInteger("RefCount", 0);
            }
        }

        // Player1のスコアをリセット
        foreach (var scoreText in Player1ColumnScoreTexts)
        {
            scoreText.text = "00";
        }
        Player1MainScoreText.text = "00";

        // Player2のスコアをリセット
        foreach (var scoreText in Player2ColumnScoreTexts)
        {
            scoreText.text = "00";
        }
        Player2MainScoreText.text = "00";

        // ターン表示をリセット
        TurnText.text = "01";

        // システムメッセージをリセット
        SystemText.text = "Ready";

        // Player1のボタンを有効化
        Player1EntryButton.interactable = true;
        Player1LeaveButton.interactable = false;
        Player1CPUEntryButton.interactable = true;

        // Player2のボタンを有効化
        Player2EntryButton.interactable = true;
        Player2LeaveButton.interactable = false;
        Player2CPUEntryButton.interactable = true;

        // リセットボタンを無効化
        ResetButton.interactable = false;
    }

    #region Dice計算

    /// <summary>
    /// サイコロの配置から通し番号を取得する。LSBから列0行0, 列0行1, 列0行2, 列1行0, ... となる
    static int GetDiceArrayIndex(int col, int row) => col * NUM_ROWS + row;

    /// <summary>
    /// サイコロの配置からサイコロの値を取得する
    /// </summary>
    static int GetDiceArrayValue(ulong bits, int col, int row)
    {
        var index = GetDiceArrayIndex(col, row);
        var bitOffset = index * DICE_BIT_WIDTH;

        return (int)((bits >> bitOffset) & DICE_BIT_MASK);
    }

    /// <summary>
    /// サイコロの配置を更新した値を返す
    /// </summary>
    static ulong ApplyDiceArrayValue(ulong bits, int col, int row, int value)
    {
        var index = GetDiceArrayIndex(col, row);
        var bitOffset = index * DICE_BIT_WIDTH;

        // 一度クリアしてからセットする
        bits &= ~(DICE_BIT_MASK << bitOffset);
        bits |= ((ulong)value & DICE_BIT_MASK) << bitOffset;

        return bits;
    }

    /// <summary>
    /// 列単位でサイコロの配置を取得する
    /// </summary>
    static int[] GetDiceArrayValueByColumn(ulong bits, int col)
    {
        var values = new int[NUM_ROWS];
        for (var row = 0; row < NUM_ROWS; row++)
        {
            values[row] = GetDiceArrayValue(bits, col, row);
        }
        return values;
    }

    /// <summary>
    /// 列内のサイコロが何個あるかを取得する
    /// </summary>
    static int[] GetDiceArrayRefCountByColumn(int[] values)
    {
        var refCounts = new int[values.Length];
        for (var i = 0; i < refCounts.Length; i++)
        {
            refCounts[i] = 0;
        }

        for (int diceNum = MIN_DICE_VALUE; diceNum <= MAX_DICE_VALUE; diceNum++)
        {
            // 同じ値の数. Lambda非対応
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
                    refCounts[row]++;
                }
            }
        }
        return refCounts;
    }

    /// <summary>
    /// 列単位でサイコロの配置を更新した値を返す
    /// </summary>
    static ulong ApplyDiceArrayBitsByColumn(ulong bits, int col, int[] values)
    {
        for (var row = 0; row < NUM_ROWS; row++)
        {
            bits = ApplyDiceArrayValue(bits, col, row, values[row]);
        }
        return bits;
    }

    /// <summary>
    /// 列内のスコアを計算する
    /// サイコロの名の合計値だが、同じ値が複数ある場合その数分だけ乗算する
    /// </summary>
    static int GetDiceArrayScoreByColumn(ulong bits, int col)
    {
        var values = GetDiceArrayValueByColumn(bits, col);
        var refCounts = GetDiceArrayRefCountByColumn(values);

        // スコア計算本体
        // value * refCountしておけば、複数個ある場合の計算が楽
        // e.g. 2,5,2 => 2*2 + 5*1 + 2*2 = 13 = ((2+2)*2) + 5
        // var score = Enumerable.Zip(values, refCounts, (v, c) => v * c).Sum();
        // Lambdaが使えないので、手動で計算する
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
    static int GetDiceArrayTotalScore(ulong bits)
    {
        // Lambda非対応
        // var scores = Enumerable.Range(0, NUM_COLUMNS).Select(col => GetDiceArrayScoreByColumn(bits, col));
        var scores = 0;
        for (var col = 0; col < NUM_COLUMNS; col++)
        {
            scores += GetDiceArrayScoreByColumn(bits, col);
        }
        return scores;
    }

    /// <summary>
    /// 配置済のサイコロの数を取得する
    /// </summary>
    static int GetDiceArrayCountByColumn(ulong bits, int col)
    {
        var count = 0;
        for (var row = 0; row < NUM_ROWS; row++)
        {
            if (GetDiceArrayValue(bits, col, row) != INVALID_DICE_VALUE)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// サイコロを置ける場所がなくなっていたらTrueを返す
    /// </summary>
    static bool IsNoPlaceToPutDice(ulong bits)
    {
        for (var col = 0; col < NUM_COLUMNS; col++)
        {
            if (GetDiceArrayCountByColumn(bits, col) < NUM_ROWS)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 指定された列の指定された値を削除する
    /// Playerがサイコロを配置時、相手の列に同じ値がある場合、その値を削除するために使用する
    /// </summary>
    static ulong RemoveSpecifiedDiceValueByColumn(ulong bits, int col, int value)
    {
        // 列の値を取得
        var values = GetDiceArrayValueByColumn(bits, col);
        // 削除対象のインデックスを除外した配列を作成
        // Lambda非対応
        // var remainedValues = values.Where(v => v != value).ToArray();
        var remainedValues = new int[NUM_ROWS];
        var remainedIndex = 0;
        for (var i = 0; i < values.Length; i++)
        {
            if (values[i] != value)
            {
                remainedValues[remainedIndex] = values[i];
                remainedIndex++;
            }
        }

        // 列の値を前詰めで作り直す
        var newValues = new int[NUM_ROWS];
        for (var i = 0; i < newValues.Length; i++)
        {
            newValues[i] = i < remainedValues.Length ? remainedValues[i] : INVALID_DICE_VALUE;
        }

        // 列の値を更新したbitを返す
        var newBits = ApplyDiceArrayBitsByColumn(bits, col, newValues);
        return newBits;
    }

    #endregion

    /// <summary>
    /// Prints a message to the console
    /// </summary>
    /// <param name="msg"></param>
    void Log(ErrorLevel level, string msg)
    {
        switch (level)
        {
            case ErrorLevel.Info:
                if (IsDebug)
                {
                    Debug.Log($"[UdonKnucklebones] {msg}");
                }
                break;
            case ErrorLevel.Warning:
                Debug.LogWarning($"[UdonKnucklebones] {msg}");
                break;
            case ErrorLevel.Error:
                Debug.LogError($"[UdonKnucklebones] {msg}");
                break;
        }
    }
    #endregion

    void Start()
    {
        if (!IsFinishedInspectorSetting(out var msg))
        {
            Log(ErrorLevel.Error, msg);
            return;
        }
        ResetAllState();

        // Ownerの場合だけ初期化実行者になる
        if (Networking.IsOwner(gameObject))
        {
            ResetSyncedProperties();
        }

        Log(ErrorLevel.Info, "Udon Knucklebones is ready!");
    }
}

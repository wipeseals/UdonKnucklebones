
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using System;


/// <summary>
/// エラーレベル
/// </summary>
public enum ErrorLevel : int
{
    Info,
    Warning,
    Error
};

/// <summary>
/// ゲームの勝敗状態
/// </summary>
public enum GameJudge : int
{
    Player1Win,
    Player2Win,
    Draw,
    Continue
}

/// <summary>
/// ゲームの状態
/// </summary>
public enum GameProgress : int
{
    /// <summary>
    /// 初期化直後
    /// </summary>
    Reset = 0,
    /// <summary>
    /// Player1の参加待ち
    /// </summary>
    WaitEnterPlayer1,
    /// <summary>
    /// Player2の参加待ち
    /// </summary>
    WaitEnterPlayer2,
    /// <summary>
    /// ゲーム開始。再戦用に設けた状態
    /// </summary>
    GameStart,
    /// <summary>
    /// Player1のサイコロ転がし待ち
    /// </summary>
    Player1Roll,
    /// <summary>
    /// Player1のサイコロ配置待ち
    /// </summary>
    Player1Put,
    /// <summary>
    /// Player2のサイコロ転がし待ち
    /// </summary>
    Player2Roll,
    /// <summary>
    /// Player2のサイコロ配置待ち
    /// </summary>
    Player2Put,
    /// <summary>
    /// ゲーム終了
    /// </summary>
    GameEnd,
    /// <summary>
    /// ゲーム中断
    /// </summary>
    GameAborted,
}

/// <summary>
/// Playerの種類
/// </summary>
public enum PlayerType : int
{
    /// <summary>
    /// 無効
    /// </summary>
    Invalid = 0,
    /// <summary>
    /// 人間
    /// </summary>
    Human,
    /// <summary>
    /// CPU
    /// </summary>
    CPU
}

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class UdonKnucklebones : UdonSharpBehaviour
{
    //////////////////////////////////////////////////////////////////////////////////////
    #region Variables

    #region Constants
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
    public EventEmitter DiceRollCollider = null;

    [Header("Selection Objects")]
    [SerializeField, Tooltip("Player1がサイコロを配置するときに列選択するためのCollider")]
    public EventEmitter[] Player1ColumnColliders = null;

    [SerializeField, Tooltip("Player2がサイコロを配置するときに列選択するためのCollider")]
    public EventEmitter[] Player2ColumnColliders = null;

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
    #region Private Properties

    #endregion
    #region Synced Properties

    [Header("Synced Properties")]

    [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(Progress))]
    public int _progress = 0;  // (int)Progress.Reset;

    /// <summary>
    /// ゲームの進行状態
    /// </summary>
    public int Progress
    {
        get => _progress;
        set
        {
            _progress = (int)value;
        }
    }

    /// <summary>
    /// Player1のタイプ
    /// </summary>
    [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(Player1Type))]
    public int _player1Type = 0;  // (int)PlayerType.Invalid;

    /// <summary>
    /// Player1のタイプ
    /// </summary>
    public int Player1Type
    {
        get => _player1Type;
        set
        {
            _player1Type = value;
        }
    }

    /// <summary>
    /// Player2のタイプ
    /// </summary>
    [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(Player2Type))]
    public int _player2Type = 0;  // (int)PlayerType.Invalid;

    /// <summary>
    /// Player2のタイプ
    /// </summary>
    public int Player2Type
    {
        get => (int)_player2Type;
        set
        {
            _player2Type = (int)value;
        }
    }

    /// <summary>
    /// サイコロの値. 0は未転がし
    /// </summary>
    [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(RolledDiceValue))]
    public int _rolledDiceValue = 0;

    /// <summary>
    /// サイコロの値. 0は未転がし
    /// </summary>
    public int RolledDiceValue
    {
        get => _rolledDiceValue;
        set
        {
            _rolledDiceValue = value;
        }
    }

    /// <summary>
    /// Player1の名前
    /// </summary>
    [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(Player1DisplayName))]
    public string _player1DisplayName = "";

    /// <summary>
    /// Player1の名前
    /// </summary>
    public string Player1DisplayName
    {
        get => _player1DisplayName;
        set
        {
            _player1DisplayName = value;
        }
    }

    /// <summary>
    /// Player2の名前
    /// </summary>
    [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(Player2DisplayName))]
    public string _player2DisplayName = "";

    /// <summary>
    /// Player2の名前
    /// </summary>
    public string Player2DisplayName
    {
        get => _player2DisplayName;
        set
        {
            _player2DisplayName = value;
        }
    }

    /// <summary>
    /// Player1のPlayerId
    /// </summary>
    [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(Player1PlayerId))]
    public int _player1PlayerId = 0;

    /// <summary>
    /// Player1のPlayerId
    /// </summary>
    public int Player1PlayerId
    {
        get => _player1PlayerId;
        set
        {
            _player1PlayerId = value;
        }
    }

    /// <summary>
    /// Player2のPlayerId
    /// </summary>
    [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(Player2PlayerId))]
    public int _player2PlayerId = 0;

    /// <summary>
    /// Player2のPlayerId
    /// </summary>
    public int Player2PlayerId
    {
        get => _player2PlayerId;
        set
        {
            _player2PlayerId = value;
        }
    }

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
        }
    }
    #endregion
    #endregion

    //////////////////////////////////////////////////////////////////////////////////////
    #region Synced Properties Accessor

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
        Log(ErrorLevel.Info, $"{nameof(ChangeOwner)}");

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
    public void SyncManually()
    {
        Log(ErrorLevel.Info, $"{nameof(SyncManually)}");

        // 自分用
        OnUpdateSyncedProperties();
        // 全員に送信
        RequestSerialization();
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(OnUpdateSyncedProperties));

    }

    /// <summary>
    /// 動機変数を全て初期化する
    /// </summary>
    void ResetSyncedProperties()
    {
        Log(ErrorLevel.Info, $"{nameof(ResetSyncedProperties)}: IsOwner={IsOwner}");

        // Ownerのみが変更できる。Ownerでなければ取得
        if (!IsOwner)
        {
            ChangeOwner();
        }

        // Accessor経由で変更してからRequestSerialization
        Progress = (int)GameProgress.Reset;
        Player1Type = (int)PlayerType.Invalid;
        Player2Type = (int)PlayerType.Invalid;
        RolledDiceValue = 0;
        Player1DisplayName = "";
        Player2DisplayName = "";
        Player1PlayerId = 0;
        Player2PlayerId = 0;
        CurrentTurn = 1;
        CurrentPlayer = 1;
        Player1DiceArrayBits = 0;
        Player2DiceArrayBits = 0;

        SyncManually();
    }
    #endregion

    #region DiceArrayBits Accessor

    /// <summary>
    /// プレイヤーのサイコロ配置を取得
    /// </summary>
    ulong GetDiceArrayBits(int player) => player == 0 ? Player1DiceArrayBits : Player2DiceArrayBits;

    /// <summary>
    /// プレイヤーのサイコロ配置を更新
    /// </summary>
    void SetDiceArrayBits(int player, ulong bits)
    {
        Log(ErrorLevel.Info, $"{nameof(SetDiceArrayBits)}: player={player}, bits={bits:016X}");

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

    void PutDice(int player, int col, int value, bool isIndexCross = true)
    {
        Log(ErrorLevel.Info, $"{nameof(PutDice)}: player={player}, col={col}, value={value}, isIndexCross={isIndexCross}");

        // 現在のターンのプレイヤーか確認してから
        if (player != CurrentPlayer)
        {
            Log(ErrorLevel.Warning, $"Invalid player number: {player}");
            return;
        }

        var srcBits = GetDiceArrayBits(player);
        // 指定された列の末尾に配置
        var rowIndex = srcBits.GetColumnCount(col);
        if (rowIndex >= DiceArrayBits.NUM_ROWS)
        {
            Log(ErrorLevel.Warning, $"Cannot place dice to column {col}");
            return;
        }
        SetDiceArrayBits(player, srcBits.PutDice(col, rowIndex, value));

        // 相手の列に同じ値がある場合、削除する
        var opponentPlayer = player == 0 ? 1 : 0;
        var opponentColumn = isIndexCross ? (DiceArrayBits.NUM_COLUMNS - col - 1) : col;// 列のindexは向かい合っている場合、DiceArrayBits.NUM_COLUMNS - col - 1で対向している列を取得

        // 相手の列から配置した値を削除
        SetDiceArrayBits(opponentPlayer, GetDiceArrayBits(opponentPlayer).RemoveByValue(opponentColumn, value));
    }

    /// <summary>
    /// ゲームの勝敗を取得
    /// </summary>
    GameJudge CalcGameJudge(int currentTurnPlayer)
    {
        // 現在のターンのプレイヤーが配置後、置けなくなった時点で終了
        if (!GetDiceArrayBits(currentTurnPlayer).IsFull())
        {
            // まだ置ける
            return GameJudge.Continue;
        }

        var player1Score = GetDiceArrayBits(PLAYER1).GetTotalScore();
        var player2Score = GetDiceArrayBits(PLAYER2).GetTotalScore();

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

    #region UI Utility

    /// <summary>
    /// Inspectorの設定が完了しているか確認する
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    bool IsFinishedInspectorSetting(out string msg)
    {
        Log(ErrorLevel.Info, $"{nameof(IsFinishedInspectorSetting)}");

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
        if (Player1ColumnColliders.Length != DiceArrayBits.NUM_COLUMNS)
        {
            msg = $"{nameof(Player1ColumnColliders)} must have {DiceArrayBits.NUM_COLUMNS} elements!";
            return false;
        }
        if (Player2ColumnColliders.Length != DiceArrayBits.NUM_COLUMNS)
        {
            msg = $"{nameof(Player2ColumnColliders)} must have {DiceArrayBits.NUM_COLUMNS} elements!";
            return false;
        }
        if (Player1Col1DiceArray.Length != DiceArrayBits.NUM_ROWS)
        {
            msg = $"{nameof(Player1Col1DiceArray)} must have {DiceArrayBits.NUM_ROWS} elements!";
            return false;
        }
        if (Player1Col2DiceArray.Length != DiceArrayBits.NUM_ROWS)
        {
            msg = $"{nameof(Player1Col2DiceArray)} must have {DiceArrayBits.NUM_ROWS} elements!";
            return false;
        }
        if (Player1Col3DiceArray.Length != DiceArrayBits.NUM_ROWS)
        {
            msg = $"{nameof(Player1Col3DiceArray)} must have {DiceArrayBits.NUM_ROWS} elements!";
            return false;
        }
        if (Player2Col1DiceArray.Length != DiceArrayBits.NUM_ROWS)
        {
            msg = $"{nameof(Player2Col1DiceArray)} must have {DiceArrayBits.NUM_ROWS} elements!";
            return false;
        }
        if (Player2Col2DiceArray.Length != DiceArrayBits.NUM_ROWS)
        {
            msg = $"{nameof(Player2Col2DiceArray)} must have {DiceArrayBits.NUM_ROWS} elements!";
            return false;
        }
        if (Player2Col3DiceArray.Length != DiceArrayBits.NUM_ROWS)
        {
            msg = $"{nameof(Player2Col3DiceArray)} must have {DiceArrayBits.NUM_ROWS} elements!";
            return false;
        }
        if (Player1ColumnScoreTexts.Length != DiceArrayBits.NUM_COLUMNS)
        {
            msg = $"{nameof(Player1ColumnScoreTexts)} must have {DiceArrayBits.NUM_COLUMNS} elements!";
            return false;
        }
        if (Player2ColumnScoreTexts.Length != DiceArrayBits.NUM_COLUMNS)
        {
            msg = $"{nameof(Player2ColumnScoreTexts)} must have {DiceArrayBits.NUM_COLUMNS} elements!";
            return false;
        }


        // All checks passed
        msg = "Setup is complete!";
        return true;
    }

    /// <summary>
    /// すべての状態をリセットする
    /// </summary>
    void ResetAllUIState()
    {
        Log(ErrorLevel.Info, $"{nameof(ResetAllUIState)}");

        // DiceForReadyのAnimationをリセット
        DiceForReady.gameObject.SetActive(true);
        DiceForReady.SetBool("IsReady", true);

        // DiceForRollの位置をリセットして非表示で待機状態にする
        DiceForRoll.transform.position = DiceForReady.transform.position;
        DiceForRoll.SetActive(false);

        // DiceRollColliderを無効化
        DiceRollCollider.IsEventSendable = false;

        // Player1 Column Collidersを無効化
        foreach (var collider in Player1ColumnColliders)
        {
            collider.IsEventSendable = false;
        }

        // Player2 Column Collidersを無効化
        foreach (var collider in Player2ColumnColliders)
        {
            collider.IsEventSendable = false;
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

    #region Event Process
    #endregion

    #region Event Handlers
    void Start()
    {
        Log(ErrorLevel.Info, $"{nameof(Start)}");

        // 設定完了確認とUIのリセット
        if (!IsFinishedInspectorSetting(out var msg))
        {
            Log(ErrorLevel.Error, msg);
            return;
        }
        ResetAllUIState();

        // Ownerの場合だけ初期化実行者になる
        if (Networking.IsOwner(gameObject))
        {
            ResetSyncedProperties();
        }
    }

    /// <summary>
    /// Synced Propertiesが更新されたときに呼び出される。UIの更新を行う
    /// </summary>
    public void OnUpdateSyncedProperties()
    {
        Log(ErrorLevel.Info, $"{nameof(OnUpdateSyncedProperties)}");

        TurnText.text = $"Turn: {CurrentTurn:D3}";
        for (var col = 0; col < DiceArrayBits.NUM_COLUMNS; col++)
        {
            Player1ColumnScoreTexts[col].text = $"{Player1DiceArrayBits.GetColumnScore(col):D02}";
            Player2ColumnScoreTexts[col].text = $"{Player2DiceArrayBits.GetColumnScore(col):D02}";
        }
        Player1MainScoreText.text = $"Player1: {Player1DiceArrayBits.GetTotalScore():D03} pt.";
        Player2MainScoreText.text = $"Player2: {Player2DiceArrayBits.GetTotalScore():D03} pt.";

        // TODO: 参加ボタンやら勝敗やらサイコロフリのステータスやら

    }

    /// <summary>
    /// Player1が参加ボタンを押したときに呼び出される
    /// </summary>
    public void OnPlayer1Entry()
    {
        Log(ErrorLevel.Info, nameof(OnPlayer1Entry));
        // TODO: Player1の参加処理
    }

    /// <summary>
    /// Player1が離脱ボタンを押したときに呼び出される
    /// </summary>
    public void OnPlayer1Leave()
    {
        Log(ErrorLevel.Info, nameof(OnPlayer1Leave));
        // TODO: Player1の離脱処理
    }

    /// <summary>
    /// Player1がCPU参加ボタンを押したときに呼び出される
    /// </summary>
    public void OnPlayer1CPUEntry()
    {
        Log(ErrorLevel.Info, nameof(OnPlayer1CPUEntry));
        // TODO: Player1のCPU参加処理
    }

    /// <summary>
    /// Player2が参加ボタンを押したときに呼び出される
    /// </summary>
    public void OnPlayer2Entry()
    {
        Log(ErrorLevel.Info, nameof(OnPlayer2Entry));
        // TODO: Player2の参加処理
    }

    /// <summary>
    /// Player2が離脱ボタンを押したときに呼び出される
    /// </summary>
    public void OnPlayer2Leave()
    {
        Log(ErrorLevel.Info, nameof(OnPlayer2Leave));
        // TODO: Player2の離脱処理
    }

    /// <summary>
    /// Player2がCPU参加ボタンを押したときに呼び出される
    /// </summary>
    public void OnPlayer2CPUEntry()
    {
        Log(ErrorLevel.Info, nameof(OnPlayer2CPUEntry));
        // TODO: Player2のCPU参加処理
    }

    /// <summary>
    /// リマッチボタンを押したときに呼び出される
    /// </summary>
    public void OnRematch()
    {
        Log(ErrorLevel.Info, nameof(OnRematch));
        // TODO: リマッチ処理
    }

    /// <summary>
    /// リセットボタンを押したときに呼び出される
    /// </summary>
    public void OnReset()
    {
        Log(ErrorLevel.Info, nameof(OnReset));
        // TODO: リセット処理
    }

    /// <summary>
    /// サイコロを転がしをUseしたときに呼び出される
    /// </summary>
    public void OnRollDice()
    {
        Log(ErrorLevel.Info, nameof(OnRollDice));
    }

    /// <summary>
    /// Player1が1列目にサイコロを配置したときに呼び出される
    /// </summary>
    public void OnPutP1C1()
    {
        Log(ErrorLevel.Info, nameof(OnPutP1C1));
        // TODO: サイコロを配置する
    }

    /// <summary>
    /// Player1が2列目にサイコロを配置したときに呼び出される
    /// </summary>
    public void OnPutP1C2()
    {
        Log(ErrorLevel.Info, nameof(OnPutP1C2));
        // TODO: サイコロを配置する
    }

    /// <summary>
    /// Player1が3列目にサイコロを配置したときに呼び出される
    /// </summary>
    public void OnPutP1C3()
    {
        Log(ErrorLevel.Info, nameof(OnPutP1C3));
        // TODO: サイコロを配置する
    }

    /// <summary>
    /// Player2が1列目にサイコロを配置したときに呼び出される
    /// </summary>
    public void OnPutP2C1()
    {
        Log(ErrorLevel.Info, nameof(OnPutP2C1));
        // TODO: サイコロを配置する
    }

    /// <summary>
    /// Player2が2列目にサイコロを配置したときに呼び出される
    /// </summary>
    public void OnPutP2C2()
    {
        Log(ErrorLevel.Info, nameof(OnPutP2C2));
        // TODO: サイコロを配置する
    }

    /// <summary>
    /// Player2が3列目にサイコロを配置したときに呼び出される
    /// </summary>
    public void OnPutP2C3()
    {
        Log(ErrorLevel.Info, nameof(OnPutP2C3));
        // TODO: サイコロを配置する
    }

    #endregion
    //////////////////////////////////////////////////////////////////////////////////////
}

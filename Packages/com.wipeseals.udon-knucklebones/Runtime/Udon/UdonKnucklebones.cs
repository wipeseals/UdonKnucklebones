
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

enum ErrorLevel
{
    Info,
    Warning,
    Error
};

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class UdonKnucklebones : UdonSharpBehaviour
{
    #region Constants
    /// <summary>
    /// サイコロを並べられる行数
    /// </summary>
    public readonly int NUM_COLUMNS = 3;
    /// <summary>
    /// サイコロを並べられる列数
    /// </summary>
    public readonly int NUM_ROWS = 3;
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

    /// <summary>
    /// セットアップが完了しているかどうかを確認します
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    public bool IsSetupComplete(out string msg)
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

    void Start()
    {
        if (!IsSetupComplete(out var msg))
        {
            Log(ErrorLevel.Error, msg);
            return;
        }
        Log(ErrorLevel.Info, "Udon Knucklebones is ready!");
    }
}

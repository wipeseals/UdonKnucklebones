
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class UdonKnucklebones : UdonSharpBehaviour
{
    #region Constants
    /// <summary>
    /// The number of columns in the knucklebones game
    /// </summary>
    public readonly int NUM_COLUMNS = 3;
    /// <summary>
    /// The number of rows in the knucklebones game
    /// </summary>
    public readonly int NUM_ROWS = 3;
    #endregion

    #region Properties

    [Header("Udon Knucklebones")]
    [SerializeField, Tooltip("Debug Enabled")]
    public bool IsDebug = true;

    [SerializeField, Tooltip("The dice object to roll")]
    public GameObject Dice = null;


    [SerializeField, Tooltip("The dice array to visualize for player 1 column 1")]
    public GameObject[] Player1Col1DiceArray = null;

    [SerializeField, Tooltip("The dice array to visualize for player 1 column 2")]
    public GameObject[] Player1Col2DiceArray = null;

    [SerializeField, Tooltip("The dice array to visualize for player 1 column 3")]
    public GameObject[] Player1Col3DiceArray = null;

    [SerializeField, Tooltip("The dice array to visualize for player 2 column 1")]
    public GameObject[] Player2Col1DiceArray = null;

    [SerializeField, Tooltip("The dice array to visualize for player 2 column 2")]
    public GameObject[] Player2Col2DiceArray = null;

    [SerializeField, Tooltip("The dice array to visualize for player 2 column 3")]
    public GameObject[] Player2Col3DiceArray = null;

    [SerializeField, Tooltip("Score text for player 1")]
    public Text[] Player1ColumnScoreTexts = null;

    [SerializeField, Tooltip("Score text for player 2")]
    public Text[] Player2ColumnScoreTexts = null;

    [SerializeField, Tooltip("Main score text for player 1")]
    public Text Player1MainScoreText = null;

    [SerializeField, Tooltip("Main score text for player 2")]
    public Text Player2MainScoreText = null;

    [SerializeField, Tooltip("The text to display the current turn")]
    public Text TurnText = null;

    [SerializeField, Tooltip("The text to display the current system message")]
    public Text SystemText = null;

    #endregion

    public bool IsSetupComplete()
    {
        return Dice != null &&
            Player1Col1DiceArray != null &&
            Player1Col2DiceArray != null &&
            Player1Col3DiceArray != null &&
            Player2Col1DiceArray != null &&
            Player2Col2DiceArray != null &&
            Player2Col3DiceArray != null &&
            Player1ColumnScoreTexts != null &&
            Player2ColumnScoreTexts != null &&
            Player1MainScoreText != null &&
            Player2MainScoreText != null &&
            TurnText != null &&
            SystemText != null;
    }

    /// <summary>
    /// Prints a message to the console
    /// </summary>
    /// <param name="msg"></param>
    void Log(string msg)
    {
        if (IsDebug)
        {
            Debug.Log($"[UdonKnucklebones] {msg}");
        }
    }

    void Start()
    {
        if (!IsSetupComplete())
        {
            Debug.LogError("Udon Knucklebones is not setup correctly!");
            return;
        }
        Log("Udon Knucklebones is ready!");
    }
}

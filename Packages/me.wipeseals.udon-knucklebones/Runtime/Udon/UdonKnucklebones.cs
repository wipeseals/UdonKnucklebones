using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using System;

namespace Wipeseals
{
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
        /// 初期状態。IsActive=falseで開始されたケースでOnEnable/OnDisable/Startが呼ばれない
        /// OnPlayerJoinedで初期化する
        /// </summary>
        Initial = 0,
        /// <summary>
        /// Player1/2の参加待ち
        /// </summary>
        WaitEnterPlayers,
        /// <summary>
        /// ゲーム開始。再戦用に設けた状態
        /// </summary>
        GameStart,

        /// <summary>
        /// Player1のサイコロ転がし待ち
        /// </summary>
        WaitPlayer1Roll,
        /// <summary>
        /// Player1のサイコロ転がし中
        /// </summary>
        Player1Rolling,
        /// <summary>
        /// Player1のサイコロ配置待ち
        /// </summary>
        WaitPlayer1Put,
        /// <summary>
        /// Player2のサイコロ計算待ち。ついでにサイコロの前詰めアニメーションも行う
        /// </summary>
        WaitPlayer1Calc,

        /// <summary>
        /// Player2のサイコロ転がし待ち
        /// </summary>
        WaitPlayer2Roll,
        /// <summary>
        /// Player1のサイコロ転がし中
        /// </summary>
        Player2Rolling,
        /// <summary>
        /// Player2のサイコロ配置待ち
        /// </summary>
        WaitPlayer2Put,
        /// <summary>
        /// Player2のサイコロ計算待ち。ついでにサイコロの前詰めアニメーションも行う
        /// 次の遷移先はWaitPlayer1Roll or GameEnd
        /// </summary>
        WaitPlayer2Calc,

        /// <summary>
        /// ゲーム終了
        /// </summary>
        GameEnd,
        /// <summary>
        /// ゲーム中断
        /// </summary>
        Aborted,
        /// <summary>
        /// 設定エラー
        /// </summary>
        ConfigurationError,
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

    /// <summary>
    /// Unity Vector3の方向に対応した列挙型
    /// </summary>
    public enum FaceDirection : int
    {
        Forward = 0, // (  0, +1, +1) Z+ 前
        Back,        // (  0,  0, -1) Z- 後
        Left,        // ( -1,  0,  0) X- 左
        Right,       // ( +1,  0,  0) X+ 右
        Up,          // (  0, +1,  0) Y+ 上
        Down         // (  0, -1,  0) Y- 下
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class UdonKnucklebones : UdonSharpBehaviour
    {
        //////////////////////////////////////////////////////////////////////////////////////
        // 変数定義関連
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

        [Header("for System Configuration")]
        [SerializeField, Tooltip("デバッグモード")]
        public bool IsDebug = true;

        [SerializeField, Tooltip("勝敗決定時、得点差からUdonChipsに換金するRate (PvP)")]
        public float UdonChipsPlayerRate = 50.0f;

        [SerializeField, Tooltip("勝敗決定時、得点差からUdonChipsに換金するRate (vs CPU)")]
        public float UdonChipsCpuRate = 5.0f;

        [SerializeField, Tooltip("CPUがイベントを進める速度")]
        public float ThinkTimeForCpu = 1.5f;


        [SerializeField, Tooltip("サイコロの目決定用Polling時間")]
        public float PollingSecForRolling = 0.2f;

        [SerializeField, Tooltip("Player1/2間で列番号が正面で一致しない(1,2,3 <-> 3,2,1)ときにtrue")]
        public bool IsColumnIndexCrossed = true;

        [SerializeField, Tooltip("サイコロを転がすときの力の強さ。ForceMode.VelovicityChangeで加速度を与える")]
        public float DiceRollForceRange = 1.5f;

        [SerializeField, Tooltip("サイコロ転がしのタイムアウト時間。吹き飛んでしまったときなどの対策")]
        public float DiceRollTimeoutSec = 5.0f;

        [Header("Dice Configuration")]
        [SerializeField, Tooltip("+Z方向のサイコロの目")]
        public int DiceValueForFront = 1;

        [SerializeField, Tooltip("-Z方向のサイコロの目")]
        public int DiceValueForBack = 6;

        [SerializeField, Tooltip("+X方向のサイコロの目")]
        public int DiceValueForRight = 5;

        [SerializeField, Tooltip("-X方向のサイコロの目")]
        public int DiceValueForLeft = 2;

        [SerializeField, Tooltip("+Y方向のサイコロの目")]
        public int DiceValueForTop = 4;

        [SerializeField, Tooltip("-Y方向のサイコロの目")]
        public int DiceValueForBottom = 3;

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

        [SerializeField, Tooltip("Player1 CPU参加ボタン")]
        public Button Player1CPUEntryButton = null;

        [SerializeField, Tooltip("Player2参加ボタン")]
        public Button Player2EntryButton = null;

        [SerializeField, Tooltip("Player2 CPU参加ボタン")]
        public Button Player2CPUEntryButton = null;

        [SerializeField, Tooltip("リセットボタン")]
        public Button ResetButton = null;

        [SerializeField, Tooltip("リマッチボタン")]
        public Button RematchButton = null;

        [Header("Sound Effects")]
        [SerializeField, Tooltip("サイコロ転がし音音源")]
        public AudioClip DiceRollAudioClip = null;

        [SerializeField, Tooltip("サイコロ転がし音の再生用")]
        public AudioSource DiceRollAudioSource = null;

        [SerializeField, Tooltip("サイコロ配置音音源")]
        public AudioClip DicePutAudioClip = null;

        [SerializeField, Tooltip("サイコロ配置音の再生用")]
        public AudioSource DicePutAudioSource = null;

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
        /// ゲームの勝敗状態
        /// </summary>
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(CurrentGameJudge))]
        public int _currentGameJudge = 0;  // (int)GameJudge.Continue;

        /// <summary>
        /// ゲームの勝敗状態
        /// </summary>
        public int CurrentGameJudge
        {
            get => _currentGameJudge;
            set
            {
                _currentGameJudge = (int)value;
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

        /// <summary>
        /// 転がしたサイコロの位置
        /// </summary>
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(DiceRollPosition))]
        public Vector3 _diceRollPosition = Vector3.zero;

        /// <summary>
        /// 転がしたサイコロの位置
        /// </summary>
        public Vector3 DiceRollPosition
        {
            get => _diceRollPosition;
            set
            {
                _diceRollPosition = value;
            }
        }

        /// <summary>
        /// 転がしたサイコロの向き
        /// </summary>
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(DiceRollRotation))]
        public Quaternion _diceRollRotation = Quaternion.identity;

        /// <summary>
        /// 転がしたサイコロの向き
        /// </summary>
        public Quaternion DiceRollRotation
        {
            get => _diceRollRotation;
            set
            {
                _diceRollRotation = value;
            }
        }

        /// <summary>
        /// Player1のUdonChips総額
        /// </summary>
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(Player1UdonChips))]
        public float _player1UdonChips = 0.0f;

        /// <summary>
        /// Player1のUdonChips総額
        /// </summary>
        public float Player1UdonChips
        {
            get => _player1UdonChips;
            set
            {
                _player1UdonChips = value;
            }
        }

        /// <summary>
        /// Player2のUdonChips総額
        /// </summary>
        [UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(Player2UdonChips))]
        public float _player2UdonChips = 0.0f;

        /// <summary>
        /// Player2のUdonChips総額
        /// </summary>
        public float Player2UdonChips
        {
            get => _player2UdonChips;
            set
            {
                _player2UdonChips = value;
            }
        }

        #endregion
        #region Private Variables

        /// <summary>
        /// UI初期化を行ったかどうか。これは同期せず1度はLocal実行する
        /// </summary>
        bool _isSetupUI = false;

        /// <summary>
        /// サイコロ転がし始めてからのTimeout検知用
        /// </summary>
        float _diceRollStartTime = 0.0f;

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////
        /// 便利関数ほか
        #region Synced Properties Accessor

        /// <summary>
        /// 現在のプレイヤー (0 or 1)
        /// </summary>
        public int CurrentPlayer
        {
            get
            {
                switch (Progress)
                {
                    case (int)GameProgress.WaitPlayer1Roll:
                    case (int)GameProgress.Player1Rolling:
                    case (int)GameProgress.WaitPlayer1Put:
                    case (int)GameProgress.WaitPlayer1Calc:
                        return PLAYER1;
                    case (int)GameProgress.WaitPlayer2Roll:
                    case (int)GameProgress.Player2Rolling:
                    case (int)GameProgress.WaitPlayer2Put:
                    case (int)GameProgress.WaitPlayer2Calc:
                        return PLAYER2;
                    default:
                        Log(ErrorLevel.Error, $"Invalid Progress: {Progress}");
                        return PLAYER1;
                }
            }
        }

        /// <summary>
        /// 設定に問題がある場合はtrue
        /// </summary>
        public bool IsConfigurationError => Progress == (int)GameProgress.ConfigurationError;

        /// <summary>
        /// ゲームが開始していないならtrue
        /// </summary>
        public bool IsGameNotReady => Progress == (int)GameProgress.Initial || Progress == (int)GameProgress.WaitEnterPlayers;

        /// <summary>
        /// ゲーム終了済ならtrue
        /// </summary>
        public bool IsGameEnd => (Progress == (int)GameProgress.GameEnd) || (Progress == (int)GameProgress.Aborted);

        /// <summary>
        /// 現在のプレイヤーがCPUならtrue
        /// </summary>
        public bool IsControlledByCpu =>
            (CurrentPlayer == PLAYER1 && Player1Type == (int)PlayerType.CPU)
            || (CurrentPlayer == PLAYER2 && Player2Type == (int)PlayerType.CPU);

        /// <summary>
        /// CPUしか参加していなければtrue
        /// </summary>
        public bool IsCpuOnly => (Player1Type == (int)PlayerType.CPU && Player2Type == (int)PlayerType.CPU);

        /// <summary>
        /// 自身がPlayer1ならtrue
        /// </summary>
        public bool IsMyselfPlayer1 => IsUnityDebug || Networking.LocalPlayer.playerId == Player1PlayerId;

        /// <summary>
        /// 自身がPlayer2ならtrue
        /// </summary>
        public bool IsMyselfPlayer2 => IsUnityDebug || Networking.LocalPlayer.playerId == Player2PlayerId;

        /// <summary>
        /// 自身が参加しているならtrue
        /// </summary>
        public bool IsJoinedMyself => IsMyselfPlayer1 || IsMyselfPlayer2;

        /// <summary>
        /// 現在のプレイヤーが自分ならtrue
        /// </summary>
        public bool IsMyTurn => (IsMyselfPlayer1 && CurrentPlayer == PLAYER1)
         || (IsMyselfPlayer2 && CurrentPlayer == PLAYER2);

        /// <summary>
        /// DiceForRollの向きからサイコロの目を取得
        /// </summary>
        /// <returns></returns>
        public int GetRolledDiceValue()
        {
            // DiceForRollの向き情報を取得
            var forward = DiceForRoll.gameObject.transform.forward;
            var up = DiceForRoll.gameObject.transform.up;
            var right = DiceForRoll.gameObject.transform.right;
            var back = -forward;
            var down = -up;
            var left = -right;

            // Vector3.upとの角度が最も小さいものが上面
            // anglesのindexはFaceDirectionと対応
            var angles = new[] {
                Vector3.Angle(forward, Vector3.up),
                Vector3.Angle(back, Vector3.up),
                Vector3.Angle(left, Vector3.up),
                Vector3.Angle(right, Vector3.up),
                Vector3.Angle(up, Vector3.up),
                Vector3.Angle(down, Vector3.up),
            };

            // 最小値のインデックスを取得
            var minIndex = 0;
            for (var i = 1; i < angles.Length; i++)
            {
                if (angles[i] < angles[minIndex])
                {
                    minIndex = i;
                }
            }

            // インデックスから方向を取得
            var diceValue = 0;
            switch ((FaceDirection)minIndex)
            {
                case FaceDirection.Forward:
                    diceValue = DiceValueForFront;
                    break;
                case FaceDirection.Back:
                    diceValue = DiceValueForBack;
                    break;
                case FaceDirection.Left:
                    diceValue = DiceValueForLeft;
                    break;
                case FaceDirection.Right:
                    diceValue = DiceValueForRight;
                    break;
                case FaceDirection.Up:
                    diceValue = DiceValueForTop;
                    break;
                case FaceDirection.Down:
                    diceValue = DiceValueForBottom;
                    break;
                default:
                    Log(ErrorLevel.Error, $"Invalid direction: {minIndex}");
                    break;
            }

            Log(ErrorLevel.Info, $"diceValue={diceValue} minIndex={minIndex} angles=[{angles[0]} {angles[1]} {angles[2]} {angles[3]} {angles[4]} {angles[5]}]");
            return diceValue;
        }

        /// <summary>
        /// Player1のサイコロ列の配列
        /// </summary>
        public Animator[][] Player1ColDiceArrayList => new[] { Player1Col1DiceArray, Player1Col2DiceArray, Player1Col3DiceArray };
        /// <summary>
        /// Player2のサイコロ列の配列
        /// </summary>
        public Animator[][] Player2ColDiceArrayList => new[] { Player2Col1DiceArray, Player2Col2DiceArray, Player2Col3DiceArray };

        /// <summary>
        /// Unity Debug時かどうか
        /// </summary>
        public bool IsUnityDebug => Networking.LocalPlayer == null;

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

            // 全員に送信
            RequestSerialization();
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(OnUIUpdate));

        }

        /// <summary>
        /// 動機変数を全て初期化する
        /// </summary>
        public void ResetSyncedProperties()
        {
            Log(ErrorLevel.Info, $"{nameof(ResetSyncedProperties)}: IsOwner={IsOwner}");

            // Ownerのみが変更できる。Ownerでなければ取得
            if (!IsOwner)
            {
                ChangeOwner();
            }

            // Accessor経由で変更してからRequestSerialization
            Progress = (int)GameProgress.WaitEnterPlayers;
            CurrentGameJudge = (int)GameJudge.Continue;
            Player1Type = (int)PlayerType.Invalid;
            Player2Type = (int)PlayerType.Invalid;
            RolledDiceValue = 0;
            Player1DisplayName = "";
            Player2DisplayName = "";
            Player1PlayerId = 0;
            Player2PlayerId = 0;
            CurrentTurn = 1;
            Player1DiceArrayBits = 0;
            Player2DiceArrayBits = 0;
            DiceRollPosition = Vector3.zero;
            DiceRollRotation = Quaternion.identity;
            Player1UdonChips = 0.0f;
            Player2UdonChips = 0.0f;
        }
        #endregion
        #region DiceArrayBits Accessor

        /// <summary>
        /// プレイヤーのサイコロ配置を取得
        /// </summary>
        public ulong GetDiceArrayBits(int player) => player == 0 ? Player1DiceArrayBits : Player2DiceArrayBits;

        /// <summary>
        /// プレイヤーのサイコロ配置を更新
        /// </summary>
        public void SetDiceArrayBits(int player, ulong bits)
        {
            Log(ErrorLevel.Info, $"{nameof(SetDiceArrayBits)}: player={player}");

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

        #endregion
        #region UdonChips Event (Empty)
        /// <summary>
        /// 所持金を取得し同期変数に設定
        /// 本家UdonKnucklebonesではUdonChipsへの参照を持っていないので何もしない
        /// </summary>
        public virtual void OnUpdateCurrentUdonChips()
        {
        }

        /// <summary>
        /// 勝敗の金額を反映。ローカル処理
        /// 本家UdonKnucklebonesではUdonChipsへの参照を持っていないので何もしない
        /// </summary>
        public virtual void OnApplyUdonChips()
        {
        }
        #endregion
        #region UI Utility

        /// <summary>
        /// Inspectorの設定が完了しているか確認する
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool IsValidInspectorSettings(out string msg)
        {
            Log(ErrorLevel.Info, $"{nameof(IsValidInspectorSettings)}");

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
            if (RematchButton == null)
            {
                msg = $"{nameof(RematchButton)} is not set!";
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

            if (DiceRollAudioClip == null)
            {
                msg = $"{nameof(DiceRollAudioClip)} is not set!";
                return false;
            }
            if (DiceRollAudioSource == null)
            {
                msg = $"{nameof(DiceRollAudioSource)} is not set!";
                return false;
            }
            if (DicePutAudioClip == null)
            {
                msg = $"{nameof(DicePutAudioClip)} is not set!";
                return false;
            }
            if (DicePutAudioSource == null)
            {
                msg = $"{nameof(DicePutAudioSource)} is not set!";
                return false;
            }

            // All checks passed
            msg = "Setup is complete!";
            return true;
        }

        /// <summary>
        /// すべての状態をリセットする
        /// </summary>
        public void ResetAllUIState()
        {
            Log(ErrorLevel.Info, $"{nameof(ResetAllUIState)}");

            // DiceForReadyのAnimationをリセット
            DiceForReady.gameObject.SetActive(false);
            DiceForReady.SetBool("IsReady", false);

            // DiceRollColliderを無効化
            DiceRollCollider.IsEventSendable = false;

            // DiceForRollの位置をリセットして非表示で待機状態にする
            DiceForRoll.transform.position = DiceForReady.transform.position;
            DiceForRoll.SetActive(false);

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
                    dice.gameObject.SetActive(false);
                    dice.SetInteger("Number", 0);
                    dice.SetInteger("RefCount", 0);
                }
            }
            // Player2のサイコロをAnimation Controllerから非表示に設定する
            foreach (var diceArray in new[] { Player2Col1DiceArray, Player2Col2DiceArray, Player2Col3DiceArray })
            {
                foreach (var dice in diceArray)
                {
                    dice.gameObject.SetActive(false);
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
            SystemText.text = "Initialize";

            // Player1のボタンを無効化
            Player1EntryButton.interactable = false;
            Player1CPUEntryButton.interactable = false;

            // Player2のボタンを無効化
            Player2EntryButton.interactable = false;
            Player2CPUEntryButton.interactable = false;

            // リセットボタンを無効化
            ResetButton.interactable = false;
            RematchButton.interactable = false;
        }

        /// <summary>
        /// Prints a message to the console
        /// </summary>
        /// <param name="msg"></param>
        public void Log(ErrorLevel level, string msg)
        {
            switch (level)
            {
                case ErrorLevel.Info:
                    if (IsDebug)
                    {
                        UnityEngine.Debug.Log($"[UdonKnucklebones] {msg}");
                    }
                    break;
                case ErrorLevel.Warning:
                    UnityEngine.Debug.LogWarning($"[UdonKnucklebones] {msg}");
                    break;
                case ErrorLevel.Error:
                    UnityEngine.Debug.LogError($"[UdonKnucklebones] {msg}");
                    break;
            }
        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////
        // Event関連
        #region Event Process

        /// <summary>
        /// UIの設定ができているか確認してからUIの初期化
        /// </summary>
        public bool SetupUI()
        {
            // 設定完了確認とUIのリセット
            if (!IsValidInspectorSettings(out var msg))
            {
                Log(ErrorLevel.Error, msg);
                return false;
            }
            ResetAllUIState();
            return true;
        }

        /// <summary>
        /// ゲームの完全初期化。Playerなども含めてクリア
        /// Ownerではない場合は取得
        /// </summary>
        public void InitAllGameStatus()
        {
            Log(ErrorLevel.Info, $"{nameof(InitAllGameStatus)}");

            // Ownerのみが変更できる。Ownerでなければ取得
            if (!IsOwner)
            {
                ChangeOwner();
            }

            // 変数初期化して同期
            ResetSyncedProperties(); // state: * -> WaitEnterPlayers

            SyncManually();
        }


        /// <summary>
        /// Player参加
        /// </summary>
        public void JoinPlayer(int player, PlayerType type, string displayName, int playerId)
        {
            Log(ErrorLevel.Info, $"{nameof(JoinPlayer)}: player={player}, type={type}, displayName={displayName}, playerId={playerId}");

            // Ownerのみが変更できる。Ownerでなければ取得
            if (!IsOwner)
            {
                ChangeOwner();
            }

            if (player == PLAYER1)
            {
                Player1Type = (int)type;
                Player1DisplayName = displayName;
                Player1PlayerId = playerId;
            }
            else if (player == PLAYER2)
            {
                Player2Type = (int)type;
                Player2DisplayName = displayName;
                Player2PlayerId = playerId;
            }
            else
            {
                Log(ErrorLevel.Error, $"Invalid player number: {player}");
            }

            SyncManually();
        }

        /// <summary>
        /// CPU Player参加
        /// </summary>
        /// <param name="player"></param>
        public void JoinCpuPlayer(int player)
        {
            Log(ErrorLevel.Info, $"{nameof(JoinCpuPlayer)}: player={player}");

            // Ownerのみが変更できる。Ownerでなければ取得
            if (!IsOwner)
            {
                ChangeOwner();
            }

            if (player == PLAYER1)
            {
                Player1Type = (int)PlayerType.CPU;
                Player1DisplayName = "CPU1";
                Player1PlayerId = -1;
            }
            else if (player == PLAYER2)
            {
                Player2Type = (int)PlayerType.CPU;
                Player2DisplayName = "CPU2";
                Player2PlayerId = -1;
            }
            else
            {
                Log(ErrorLevel.Error, $"Invalid player number: {player}");
            }

            SyncManually();
        }

        /// <summary>
        /// Player離脱
        /// </summary>
        public void LeavePlayer(int player)
        {
            Log(ErrorLevel.Info, $"{nameof(LeavePlayer)}: player={player}");

            // Ownerのみが変更できる。Ownerでなければ取得
            if (!IsOwner)
            {
                ChangeOwner();
            }

            if (player == PLAYER1)
            {
                Player1Type = (int)PlayerType.Invalid;
                Player1DisplayName = "";
                Player1PlayerId = 0;
            }
            else if (player == PLAYER2)
            {
                Player2Type = (int)PlayerType.Invalid;
                Player2DisplayName = "";
                Player2PlayerId = 0;
            }
            else
            {
                Log(ErrorLevel.Error, $"Invalid player number: {player}");
            }

            SyncManually();
        }

        /// <summary>
        /// ゲーム開始
        /// </summary>
        public void StartGame()
        {
            Log(ErrorLevel.Info, $"{nameof(StartGame)}");

            // Ownerのみが変更できる。Ownerでなければ取得
            if (!IsOwner)
            {
                ChangeOwner();
            }

            // 盤面クリア
            Player1DiceArrayBits = 0;
            Player2DiceArrayBits = 0;
            CurrentTurn = 1;
            CurrentGameJudge = (int)GameJudge.Continue;

            // ゲーム開始. どちらのターンにするかはランダム
            Progress = UnityEngine.Random.Range(0, 2) == 0 ? (int)GameProgress.WaitPlayer1Roll : (int)GameProgress.WaitPlayer2Roll;
            SyncManually();

            // CPUなら自動で進める
            if (IsControlledByCpu)
            {
                // 固定時間だとふりはじめの角度が一致する可能性があるので、ランダムに遅延させる
                SendCustomEventDelayedSeconds(nameof(OnRollDice), ThinkTimeForCpu * (1.0f + 0.5f * UnityEngine.Random.Range(0.0f, 0.5f)));
            }
        }

        /// <summary>
        /// サイコロを降る
        /// </summary>
        public void StartRoll()
        {
            // Ownerのみが変更できる。Ownerでなければ取得
            if (!IsOwner)
            {
                ChangeOwner();
            }

            // サイコロの音を鳴らす。これはメインシーケンスとは無関係にイベント発生
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(OnPlayOneshotForRoll));

            // WaitPlayer1Roll or WaitPlayer2Roll状態だとAnimationで回しているのでその座標を転写
            DiceForRoll.SetActive(true);
            DiceForRoll.transform.position = DiceForReady.transform.position;
            DiceForRoll.transform.rotation = DiceForReady.transform.rotation;
            DiceForReady.SetBool("IsReady", false);
            DiceForReady.gameObject.SetActive(false);
            // 弾き飛ばす
            DiceForRoll.gameObject.GetComponent<Rigidbody>().AddForce(
                new Vector3(
                    DiceRollForceRange * UnityEngine.Random.Range(-1, 1),
                    DiceRollForceRange * UnityEngine.Random.Range(-1, 1),
                    DiceRollForceRange * UnityEngine.Random.Range(-1, 1)
                ),
                ForceMode.VelocityChange
            );
            // 同期用の座標にも転写
            DiceRollPosition = DiceForRoll.transform.position;
            DiceRollRotation = DiceForRoll.transform.rotation;

            // ステータス更新
            if (CurrentPlayer == PLAYER1)
            {
                Progress = (int)GameProgress.Player1Rolling;
            }
            else if (CurrentPlayer == PLAYER2)
            {
                Progress = (int)GameProgress.Player2Rolling;
            }

            // 吹き飛び対策
            _diceRollStartTime = Time.time;

            // サイコロの見張り役は自分だけがやれば良い
            SendCustomEventDelayedSeconds(nameof(OnPollingRoll), PollingSecForRolling);
        }

        /// <summary>
        /// サイコロを転がしている間のポーリング処理
        /// </summary>
        public void PollingRoll()
        {
            Log(ErrorLevel.Info, $"{nameof(PollingRoll)} Progress={Progress} CurrentPlayer={CurrentPlayer}");

            // Ownerのみが変更できる。Ownerでなければ取得
            if (!IsOwner)
            {
                ChangeOwner();
            }

            // ObjectSyncを付与していないので手動同期
            DiceRollPosition = DiceForRoll.transform.position;
            DiceRollRotation = DiceForRoll.transform.rotation;

            var isTimeout = (Time.time - _diceRollStartTime) > DiceRollTimeoutSec;
            if (isTimeout)
            {
                // タイムアウトした場合は現在の向きを取得
                Log(ErrorLevel.Warning, $"{nameof(PollingRoll)}: Timeout!");
            }
            else
            {
                // Rigidbodyが止まっていない場合は再度ポーリング
                if (!DiceForRoll.gameObject.GetComponent<Rigidbody>().IsSleeping())
                {
                    // 同期用
                    SyncManually();
                    // 自身のイベントを再度呼び出し
                    SendCustomEventDelayedSeconds(nameof(OnPollingRoll), PollingSecForRolling);
                    return;
                }
            }

            // Timeout時間クリア
            _diceRollStartTime = 0.0f;

            // サイコロの目が決定したら、その値をセットしPlayerに配置先を選ばせる
            RolledDiceValue = GetRolledDiceValue();
            if (CurrentPlayer == PLAYER1)
            {
                Progress = (int)GameProgress.WaitPlayer1Put;
            }
            else if (CurrentPlayer == PLAYER2)
            {
                Progress = (int)GameProgress.WaitPlayer2Put;
            }

            Log(ErrorLevel.Info, $"{nameof(PollingRoll)}: RolledDiceValue={RolledDiceValue} Player={CurrentPlayer}");
            SyncManually();

            // CPUなら自動で進める. 可変長にできないので埋めておく
            var usableEventNames = new[] { "", "", "" };
            var eventCount = 0;
            if (IsControlledByCpu)
            {
                if (CurrentPlayer == PLAYER1)
                {
                    var events = new[] { nameof(OnPutP1C1), nameof(OnPutP1C2), nameof(OnPutP1C3) };
                    for (int col = 0; col < DiceArrayBits.NUM_COLUMNS; col++)
                    {
                        // まだ配置できる列がある場合のみイベントを登録
                        if (!Player1DiceArrayBits.IsColumnFull(col))
                        {
                            usableEventNames[eventCount] = events[col];
                            eventCount++;
                        }
                    }
                }
                else if (CurrentPlayer == PLAYER2)
                {
                    var events = new[] { nameof(OnPutP2C1), nameof(OnPutP2C2), nameof(OnPutP2C3) };
                    for (int col = 0; col < DiceArrayBits.NUM_COLUMNS; col++)
                    {
                        // まだ配置できる列がある場合のみイベントを登録
                        if (!Player2DiceArrayBits.IsColumnFull(col))
                        {
                            usableEventNames[eventCount] = events[col];
                            eventCount++;
                        }
                    }
                }
            }
            // ランダムで選択するイベント発行
            if (eventCount > 0)
            {
                var eventName = usableEventNames[UnityEngine.Random.Range(0, eventCount)];
                SendCustomEventDelayedSeconds(eventName, ThinkTimeForCpu);
            }
        }

        /// <summary>
        /// サイコロを配置する
        /// </summary>
        public void PutDice(int col)
        {
            var player = CurrentPlayer;
            var value = RolledDiceValue;

            Log(ErrorLevel.Info, $"{nameof(PutDice)}: player={player}, col={col}, value={value}, isIndexCross={IsColumnIndexCrossed}");

            // Ownerのみが変更できる。Ownerでなければ取得
            if (!IsOwner)
            {
                ChangeOwner();
            }

            // サイコロの音を鳴らす。これはメインシーケンスとは無関係にイベント発生
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(OnPlayOneshotForPut));

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
            var opponentPlayer = player == PLAYER1 ? PLAYER2 : PLAYER1;
            var opponentColumn = IsColumnIndexCrossed ? (DiceArrayBits.NUM_COLUMNS - col - 1) : col;// 列のindexは向かい合っている場合、DiceArrayBits.NUM_COLUMNS - col - 1で対向している列を取得

            // 相手の列から配置した値を削除
            SetDiceArrayBits(opponentPlayer, GetDiceArrayBits(opponentPlayer).RemoveByValue(opponentColumn, value));

            // 次のステップへ
            if (CurrentPlayer == PLAYER1)
            {
                Progress = (int)GameProgress.WaitPlayer1Calc;
            }
            else if (CurrentPlayer == PLAYER2)
            {
                Progress = (int)GameProgress.WaitPlayer2Calc;
            }

            SyncManually();

            // ゲームの勝敗決定も自分が行えばよいのでローカルイベント発行
            SendCustomEventDelayedSeconds(nameof(OnJudgeFinishGame), ThinkTimeForCpu);
        }

        /// <summary>
        /// ゲームの勝敗を取得
        /// </summary>
        public void JudgeFinishGame()
        {
            // Ownerのみが変更できる。Ownerでなければ取得
            if (!IsOwner)
            {
                ChangeOwner();
            }


            // 毎ターンごとに各PlayerがOwnerを持ち、変数更新を行えるのでUdonChips最新値を取得しておく
            // 差額計算に近いタイミングではあるが、反映までの僅かな間に変更されるケースは諦める (一応Underflowしない対策入れた)
            OnUpdateCurrentUdonChips();

            // 左詰めの処理していないのでここでやる（PutDice時の消えるアニメーション流したいため)
            SetDiceArrayBits(PLAYER1, GetDiceArrayBits(PLAYER1).LeftJustify());
            SetDiceArrayBits(PLAYER2, GetDiceArrayBits(PLAYER2).LeftJustify());

            // まだ置けるなら続行
            if (!GetDiceArrayBits(CurrentPlayer).IsFull())
            {
                // まだ置ける
                CurrentGameJudge = (int)GameJudge.Continue;
                // 次のプレイヤーに進む
                if (CurrentPlayer == PLAYER1)
                {
                    Progress = (int)GameProgress.WaitPlayer2Roll;
                }
                else if (CurrentPlayer == PLAYER2)
                {
                    Progress = (int)GameProgress.WaitPlayer1Roll;
                }
                CurrentTurn++;
                SyncManually();

                // Progress更新した時点でCurrentPlayerの戻り値が変わる。CPUならRollするトリガを与える
                if (CurrentPlayer == PLAYER1 && Player1Type == (int)PlayerType.CPU)
                {
                    SendCustomEventDelayedSeconds(nameof(OnRollDice), ThinkTimeForCpu);
                }
                else if (CurrentPlayer == PLAYER2 && Player2Type == (int)PlayerType.CPU)
                {
                    SendCustomEventDelayedSeconds(nameof(OnRollDice), ThinkTimeForCpu);
                }
                return;
            }

            // スコア計算して終わり
            var player1Score = GetDiceArrayBits(PLAYER1).GetTotalScore();
            var player2Score = GetDiceArrayBits(PLAYER2).GetTotalScore();

            if (player1Score > player2Score)
            {
                CurrentGameJudge = (int)GameJudge.Player1Win;
            }
            else if (player1Score < player2Score)
            {
                CurrentGameJudge = (int)GameJudge.Player2Win;
            }
            else
            {
                CurrentGameJudge = (int)GameJudge.Draw;
            }

            // ゲーム終了
            Progress = (int)GameProgress.GameEnd;
            SyncManually();

            // UdonChips更新のためのイベント発火
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(OnGameEnding));
        }


        #endregion
        #region Event Handlers
        void Start()
        {
            Log(ErrorLevel.Info, $"{nameof(Start)}");

            // OnPlayerJoined で初期化実行するのでここでは何もしない
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            Log(ErrorLevel.Info, $"{nameof(OnPlayerJoined)}: {IsOwner}");

            // UI初期化自体は同期変数のProgressにかかわらず1回実行。ゲーム初期化前に複数人入ったときの対策
            if (!_isSetupUI)
            {
                _isSetupUI = true;
                bool hasSetupSucceeded = SetupUI();

                // Ownerかつ同期変数未初期化の場合はProgressに反映して以後のJoinerにも通知
                if (IsOwner && (Progress == (int)GameProgress.Initial))
                {
                    if (hasSetupSucceeded)
                    {
                        // 同期変数の初期化とPlayer追加待ちへ
                        InitAllGameStatus();
                    }
                    else
                    {
                        // Inspector設定が不完全な場合はエラー状態にしておく
                        this.Progress = (int)GameProgress.ConfigurationError;
                    }
                    SyncManually();
                }
            }

            // Configuraiton Errorの場合は何もしない
            if (IsConfigurationError)
            {
                return;
            }

            // なにかやることがあれば追加
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            Log(ErrorLevel.Info, $"{nameof(OnPlayerLeft)}: {player.displayName} {IsOwner}");

            // Configuraiton Errorの場合は何もしない
            if (IsConfigurationError)
            {
                return;
            }

            // Ownerの場合、Playerどちらかに含まれるか検査し、抜けた場合はAbort
            if (IsOwner)
            {
                if (Player1PlayerId == player.playerId)
                {
                    // Player1が抜けた
                    Player1PlayerId = 0;
                    Player1DisplayName = "";
                    Player1Type = (int)PlayerType.Invalid;
                    Progress = (int)GameProgress.Aborted;
                    Log(ErrorLevel.Warning, $"{nameof(OnPlayerLeft)}: Player1");
                }
                else if (Player2PlayerId == player.playerId)
                {
                    // Player2が抜けた
                    Player2PlayerId = 0;
                    Player2DisplayName = "";
                    Player2Type = (int)PlayerType.Invalid;
                    Progress = (int)GameProgress.Aborted;
                    Log(ErrorLevel.Warning, $"{nameof(OnPlayerLeft)}: Player2");
                }
                SyncManually();
            }
        }

        /// <summary>
        /// Synced Propertiesが更新されたときに呼び出される。UIの更新を行う
        /// </summary>
        public void OnUIUpdate()
        {
            Log(ErrorLevel.Info, $"{nameof(OnUIUpdate)}");

            // Configuration Errorの場合、使えないUIがある場合があるので何もしない
            if (IsConfigurationError)
            {
                return;
            }

            // 頭上で回転するサイコロは、Roll待ちのケースでのみ表示。
            // Use可能なのは現在のPlayerのみ
            bool isVisibleRollReady = (Progress == (int)GameProgress.WaitPlayer1Roll) || (Progress == (int)GameProgress.WaitPlayer2Roll);
            DiceForReady.gameObject.SetActive(isVisibleRollReady);
            DiceForReady.SetBool("IsReady", isVisibleRollReady);
            bool isRollable = (IsUnityDebug)
                || (Networking.LocalPlayer.playerId == Player1PlayerId && Progress == (int)GameProgress.WaitPlayer1Roll)
                || (Networking.LocalPlayer.playerId == Player2PlayerId && Progress == (int)GameProgress.WaitPlayer2Roll);
            DiceRollCollider.IsEventSendable = isRollable;

            // 転がす前の場合は、転がしたあとのサイコロは非表示かつ場所リセット
            if (isVisibleRollReady || isRollable)
            {
                DiceForRoll.SetActive(false);
                DiceForRoll.transform.position = DiceForReady.transform.position;
                DiceForRoll.transform.rotation = DiceForReady.transform.rotation;
            }
            else
            {
                // サイコロを転がし始めてから配置するまでは、サイコロを転がし中として扱う
                bool isRolled = (Progress == (int)GameProgress.Player1Rolling)
                             || (Progress == (int)GameProgress.Player2Rolling)
                             || (Progress == (int)GameProgress.WaitPlayer1Put)
                             || (Progress == (int)GameProgress.WaitPlayer2Put);
                DiceForRoll.SetActive(isRolled);

                // 転がし中かつOwnerではない場合は、Ownerからの座標を転写
                if (!IsOwner && isRolled)
                {
                    DiceForRoll.transform.position = DiceRollPosition;
                    DiceForRoll.transform.rotation = DiceRollRotation;
                }
            }

            // Player1の配置ができるのは、Player1の配置待ちの場合
            // ただし、各列のサイコロ配置数が最大に達している場合は配置できない
            bool isPlayer1Put = (IsUnityDebug) || (Networking.LocalPlayer.playerId == Player1PlayerId && Progress == (int)GameProgress.WaitPlayer1Put);
            for (int col = 0; col < DiceArrayBits.NUM_COLUMNS; col++)
            {
                // Player1のボタンが押せるケースは配置まちかつPlayer1が自分自身の場合
                Player1ColumnColliders[col].IsEventSendable = isPlayer1Put && !Player1DiceArrayBits.IsColumnFull(col);
            }
            // Player2も同様
            bool isPlayer2Put = (IsUnityDebug) || (Networking.LocalPlayer.playerId == Player2PlayerId && Progress == (int)GameProgress.WaitPlayer2Put);
            for (int col = 0; col < DiceArrayBits.NUM_COLUMNS; col++)
            {
                Player2ColumnColliders[col].IsEventSendable = isPlayer2Put && !Player2DiceArrayBits.IsColumnFull(col);
            }

            // Player1/2のサイコロ表示状態を同期
            for (int col = 0; col < DiceArrayBits.NUM_COLUMNS; col++)
            {
                var player1RefCount = Player1DiceArrayBits.GetColumnRefCount(col);
                var player2RefCount = Player2DiceArrayBits.GetColumnRefCount(col);

                for (int row = 0; row < DiceArrayBits.NUM_ROWS; row++)
                {
                    var dice1 = Player1ColDiceArrayList[col][row];
                    dice1.gameObject.SetActive(true); // 表示はずっとする
                    dice1.SetInteger("RefCount", player1RefCount[row]); // Animatorでサイコロの状態が変わる。0なら非表示2,3はアクセント表示が追加
                    dice1.SetInteger("Number", Player1DiceArrayBits.GetDice(col, row)); // Animatorでサイコロ上面の向きが変わる

                    var dice2 = Player2ColDiceArrayList[col][row];
                    dice2.gameObject.SetActive(true); // 表示はずっとする
                    dice2.SetInteger("RefCount", player2RefCount[row]); // Animatorでサイコロの状態が変わる。0なら非表示2,3はアクセント表示が追加
                    dice2.SetInteger("Number", Player2DiceArrayBits.GetDice(col, row)); // Animatorでサイコロ上面の向きが変わる
                }
            }

            // スコア表示を更新
            for (var col = 0; col < DiceArrayBits.NUM_COLUMNS; col++)
            {
                Player1ColumnScoreTexts[col].text = $"{Player1DiceArrayBits.GetColumnScore(col):D02}";
                Player2ColumnScoreTexts[col].text = $"{Player2DiceArrayBits.GetColumnScore(col):D02}";
            }
            Player1MainScoreText.text = $"Player1: {Player1DiceArrayBits.GetTotalScore():D03} pt.";
            Player2MainScoreText.text = $"Player2: {Player2DiceArrayBits.GetTotalScore():D03} pt.";

            // ターン表示を更新
            TurnText.text = $"Turn: {CurrentTurn:D3}";

            // システムメッセージを更新
            switch ((GameProgress)Progress)
            {
                case GameProgress.Initial:
                    SystemText.text = "Booting...";
                    break;
                case GameProgress.WaitEnterPlayers:
                    SystemText.text = "Waiting for Enter Players";
                    break;
                case GameProgress.GameStart:
                    SystemText.text = "Game Start!";
                    break;

                case GameProgress.WaitPlayer1Roll:
                    SystemText.text = "Player1: Roll the dice!";
                    break;
                case GameProgress.Player1Rolling:
                    SystemText.text = "Player1: Rolling the dice...";
                    break;
                case GameProgress.WaitPlayer1Put:
                    SystemText.text = $"Player1: Put the dice '{RolledDiceValue}'!";
                    break;
                case GameProgress.WaitPlayer1Calc:
                    SystemText.text = "Player1: Calculating...";
                    break;

                case GameProgress.WaitPlayer2Roll:
                    SystemText.text = "Player2: Roll the dice!";
                    break;
                case GameProgress.Player2Rolling:
                    SystemText.text = "Player2: Rolling the dice...";
                    break;
                case GameProgress.WaitPlayer2Put:
                    SystemText.text = $"Player2: Put the dice '{RolledDiceValue}'!";
                    break;
                case GameProgress.WaitPlayer2Calc:
                    SystemText.text = "Player2: Calculating...";
                    break;

                case GameProgress.GameEnd:
                    switch ((GameJudge)CurrentGameJudge)
                    {
                        case GameJudge.Player1Win:
                            SystemText.text = "Player1 Win!";
                            break;
                        case GameJudge.Player2Win:
                            SystemText.text = "Player2 Win!";
                            break;
                        case GameJudge.Draw:
                            SystemText.text = "Draw!";
                            break;
                        default:
                            break;
                    }
                    break;
                case GameProgress.Aborted:
                    SystemText.text = "Game Aborted!";
                    break;
                case GameProgress.ConfigurationError:
                    SystemText.text = "Configuration Error!";
                    break;

                default:
                    break;
            }

            // Join済ならLeaveだけ。Join前ならEntryだけ
            Player1EntryButton.interactable = (Player1PlayerId == 0);
            Player1CPUEntryButton.interactable = (Player1PlayerId == 0);
            Player2EntryButton.interactable = (Player2PlayerId == 0);
            Player2CPUEntryButton.interactable = (Player2PlayerId == 0);
            // Join済なら選んでいない方を消す
            Player1EntryButton.gameObject.SetActive((Player1PlayerId == 0) || (Player1Type == (int)PlayerType.Human));
            Player1CPUEntryButton.gameObject.SetActive((Player1PlayerId == 0) || (Player1Type == (int)PlayerType.CPU));
            Player2EntryButton.gameObject.SetActive((Player2PlayerId == 0) || (Player2Type == (int)PlayerType.Human));
            Player2CPUEntryButton.gameObject.SetActive((Player2PlayerId == 0) || (Player2Type == (int)PlayerType.CPU));

            // リセットはいたずら防止目的で設定。CPUだけの試合、もしくはゲーム開始前なら操作可能。それ以外はPlayerのみ押せる
            var isResetable = IsCpuOnly || IsGameNotReady || IsJoinedMyself;
            ResetButton.interactable = isResetable;
            // リマッチはリセットの条件に加え、ゲーム終了後のみ押せる
            // 途中で最初に戻って流れ出す、もしくはPlayer未設定のまま開始される対策
            RematchButton.interactable = isResetable && IsGameEnd;
        }

        /// <summary>
        /// Player1が参加ボタンを押したときに呼び出される
        /// </summary>
        public void OnPlayer1Entry()
        {
            Log(ErrorLevel.Info, nameof(OnPlayer1Entry));

            // Configuraiton Errorの場合は何もしない
            if (IsConfigurationError)
            {
                return;
            }

            // 参加
            JoinPlayer(PLAYER1, PlayerType.Human, Networking.LocalPlayer.displayName, Networking.LocalPlayer.playerId);

            // Player2が参加している場合は、ゲーム開始
            if (Player2PlayerId != 0)
            {
                StartGame();
            }
        }

        /// <summary>
        /// Player1がCPU参加ボタンを押したときに呼び出される
        /// </summary>
        public void OnPlayer1CPUEntry()
        {
            Log(ErrorLevel.Info, nameof(OnPlayer1CPUEntry));

            // Configuraiton Errorの場合は何もしない
            if (IsConfigurationError)
            {
                return;
            }

            // CPU参加
            JoinCpuPlayer(PLAYER1);

            // Player2が参加している場合は、ゲーム開始
            if (Player2PlayerId != 0)
            {
                StartGame();
            }
        }

        /// <summary>
        /// Player2が参加ボタンを押したときに呼び出される
        /// </summary>
        public void OnPlayer2Entry()
        {
            Log(ErrorLevel.Info, nameof(OnPlayer2Entry));

            // Configuraiton Errorの場合は何もしない
            if (IsConfigurationError)
            {
                return;
            }

            // 参加
            JoinPlayer(PLAYER2, PlayerType.Human, Networking.LocalPlayer.displayName, Networking.LocalPlayer.playerId);

            // Player1が参加している場合は、ゲーム開始
            if (Player1PlayerId != 0)
            {
                StartGame();
            }
        }

        /// <summary>
        /// Player2がCPU参加ボタンを押したときに呼び出される
        /// </summary>
        public void OnPlayer2CPUEntry()
        {
            Log(ErrorLevel.Info, nameof(OnPlayer2CPUEntry));

            // Configuraiton Errorの場合は何もしない
            if (IsConfigurationError)
            {
                return;
            }

            // CPU参加
            JoinCpuPlayer(PLAYER2);

            // Player1が参加している場合は、ゲーム開始
            if (Player1PlayerId != 0)
            {
                StartGame();
            }
        }

        /// <summary>
        /// リマッチボタンを押したときに呼び出される
        /// </summary>
        public void OnRematch()
        {
            Log(ErrorLevel.Info, nameof(OnRematch));

            // Configuraiton Errorの場合は何もしない
            if (IsConfigurationError)
            {
                return;
            }

            // Playerは保持したままゲームをリセットして再開
            StartGame();
        }

        /// <summary>
        /// リセットボタンを押したときに呼び出される
        /// </summary>
        public void OnReset()
        {
            Log(ErrorLevel.Info, nameof(OnReset));

            // Configuraiton Errorの場合は何もしない
            if (IsConfigurationError)
            {
                return;
            }

            // Playerもリセット。参加するところからやり直し
            InitAllGameStatus();
        }

        /// <summary>
        /// サイコロを転がしをUseしたときに呼び出される
        /// </summary>
        public void OnRollDice()
        {
            Log(ErrorLevel.Info, nameof(OnRollDice));

            // Configuraiton Errorの場合は何もしない
            if (IsConfigurationError)
            {
                return;
            }

            // サイコロを転がしを開始
            StartRoll();
        }

        /// <summary>
        /// サイコロの値を更新するイベントハンドラ
        /// </summary>
        public void OnPollingRoll()
        {
            Log(ErrorLevel.Info, nameof(OnPollingRoll));

            // Configuraiton Errorの場合は何もしない
            if (IsConfigurationError)
            {
                return;
            }

            // サイコロの値を決定するために観測
            PollingRoll();
        }

        /// <summary>
        /// Player1が1列目にサイコロを配置したときに呼び出される
        /// </summary>
        public void OnPutP1C1()
        {
            Log(ErrorLevel.Info, nameof(OnPutP1C1));

            // Configuraiton Errorの場合は何もしない
            if (IsConfigurationError)
            {
                return;
            }

            // 現在のPlayerと一致しているかは見ておく
            if (CurrentPlayer != PLAYER1)
            {
                Log(ErrorLevel.Warning, $"{nameof(OnPutP1C1)}: Invalid Player={CurrentPlayer}");
                return;
            }

            // サイコロを配置する
            PutDice(0);
        }

        /// <summary>
        /// Player1が2列目にサイコロを配置したときに呼び出される
        /// </summary>
        public void OnPutP1C2()
        {
            Log(ErrorLevel.Info, nameof(OnPutP1C2));

            // Configuraiton Errorの場合は何もしない
            if (IsConfigurationError)
            {
                return;
            }

            // 現在のPlayerと一致しているかは見ておく
            if (CurrentPlayer != PLAYER1)
            {
                Log(ErrorLevel.Warning, $"{nameof(OnPutP1C2)}: Invalid Player={CurrentPlayer}");
                return;
            }

            // サイコロを配置する
            PutDice(1);
        }

        /// <summary>
        /// Player1が3列目にサイコロを配置したときに呼び出される
        /// </summary>
        public void OnPutP1C3()
        {
            Log(ErrorLevel.Info, nameof(OnPutP1C3));

            // Configuraiton Errorの場合は何もしない
            if (IsConfigurationError)
            {
                return;
            }

            // 現在のPlayerと一致しているかは見ておく
            if (CurrentPlayer != PLAYER1)
            {
                Log(ErrorLevel.Warning, $"{nameof(OnPutP1C3)}: Invalid Player={CurrentPlayer}");
                return;
            }

            // サイコロを配置する
            PutDice(2);
        }

        /// <summary>
        /// Player2が1列目にサイコロを配置したときに呼び出される
        /// </summary>
        public void OnPutP2C1()
        {
            Log(ErrorLevel.Info, nameof(OnPutP2C1));

            // Configuraiton Errorの場合は何もしない
            if (IsConfigurationError)
            {
                return;
            }

            // 現在のPlayerと一致しているかは見ておく
            if (CurrentPlayer != PLAYER2)
            {
                Log(ErrorLevel.Warning, $"{nameof(OnPutP2C1)}: Invalid Player={CurrentPlayer}");
                return;
            }

            // サイコロを配置する
            PutDice(0);

        }

        /// <summary>
        /// Player2が2列目にサイコロを配置したときに呼び出される
        /// </summary>
        public void OnPutP2C2()
        {
            Log(ErrorLevel.Info, nameof(OnPutP2C2));

            // Configuraiton Errorの場合は何もしない
            if (IsConfigurationError)
            {
                return;
            }

            // 現在のPlayerと一致しているかは見ておく
            if (CurrentPlayer != PLAYER2)
            {
                Log(ErrorLevel.Warning, $"{nameof(OnPutP2C2)}: Invalid Player={CurrentPlayer}");
                return;
            }

            // サイコロを配置する
            PutDice(1);
        }

        /// <summary>
        /// Player2が3列目にサイコロを配置したときに呼び出される
        /// </summary>
        public void OnPutP2C3()
        {
            Log(ErrorLevel.Info, nameof(OnPutP2C3));

            // Configuraiton Errorの場合は何もしない
            if (IsConfigurationError)
            {
                return;
            }

            // 現在のPlayerと一致しているかは見ておく
            if (CurrentPlayer != PLAYER2)
            {
                Log(ErrorLevel.Warning, $"{nameof(OnPutP2C3)}: Invalid Player={CurrentPlayer}");
                return;
            }

            // サイコロを配置する
            PutDice(2);
        }

        public void OnJudgeFinishGame()
        {
            Log(ErrorLevel.Info, nameof(OnJudgeFinishGame));

            // Configuraiton Errorの場合は何もしない
            if (IsConfigurationError)
            {
                return;
            }

            // ゲームの勝敗を判定
            JudgeFinishGame();
        }

        /// <summary>
        /// サイコロを転がしの効果音を再生するイベント
        /// メインのシーケンスとは別に再生だけのイベントを送出
        /// </summary>
        public void OnPlayOneshotForRoll()
        {
            Log(ErrorLevel.Info, nameof(OnPlayOneshotForRoll));

            // Configuraiton Errorの場合は何もしない
            if (IsConfigurationError)
            {
                return;
            }

            // サイコロを転がしの効果音を再生
            DiceRollAudioSource.PlayOneShot(DiceRollAudioClip);
        }

        /// <summary>
        /// サイコロを配置の効果音を再生するイベント
        /// メインのシーケンスとは別に再生だけのイベントを送出
        /// </summary>
        public void OnPlayOneshotForPut()
        {
            Log(ErrorLevel.Info, nameof(OnPlayOneshotForPut));

            // Configuraiton Errorの場合は何もしない
            if (IsConfigurationError)
            {
                return;
            }

            // サイコロを配置の効果音を再生
            DicePutAudioSource.PlayOneShot(DicePutAudioClip);
        }

        /// <summary>
        /// ゲーム終了時に呼び出されるイベント。ローカルで表示・反映するものがあれば実行
        /// </summary>
        public void OnGameEnding()
        {
            Log(ErrorLevel.Info, nameof(OnGameEnding));

            // Configuraiton Errorの場合は何もしない
            if (IsConfigurationError)
            {
                return;
            }

            // ゲーム終了に合わせて残金精算
            OnApplyUdonChips();
        }
        #endregion
    }
}
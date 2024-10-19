
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UCS
{
	public class UdonChipsInstantShop_ToggleActive : UdonSharpBehaviour
	{
		private UdonChips udonChips = null;

		/// <summary>
		/// ON/OFFを切り換えるターゲット
		/// </summary>
		public GameObject[] toggleTargets = null;

		[Tooltip( "購入価格" )]
		public float price = 100;

		[Tooltip("一回きり\nTrueのとき二度目からは「売り切れ」反応をします。\nFalseのとき何度でも購入できON/OFFが切り替わります")]
		public bool oneTimeOnly = false;

		[HideInInspector][UdonSynced]
		public bool oneTimeBought = false;
		[HideInInspector][UdonSynced]
		public bool[] syncActives = new bool[0];

		public UdonChipsShopAnimator button = null;

		void Start()
		{
			// UdonChipsがWorld内に存在すると想定し初期化
			var ucgo = GameObject.Find( "UdonChips" );

			if(ucgo == null)
			{
				Debug.LogError( "[ERROR] 'UdonChip'の名前がついたGameObjectがワールド内にありません。" );
			}

			udonChips = ucgo.GetComponent<UdonChips>();

			if(udonChips == null)
			{
				Debug.LogError( "[ERROR] 'UdonChip'の名前がついたGameObjectに、UdonChipsが設定されていません。" );
			}

			if(toggleTargets == null)
			{
				Debug.LogError( "[ERROR] 'toggleActiveTargets'がnullのため正常動作できません。" );
			}
			else
			{
				for(int i = 0; i < toggleTargets.Length; ++i)
				{
					if(toggleTargets[i] == null)
					{
						Debug.LogError( "[ERROR] 'toggleActiveTargets[" + i + "]'がnullのため正常動作できません。" );
					}
				}
			}
		}

		private void Update()
		{
			button.isLackOfFunds = price > udonChips.money;
			button.isSoldout = oneTimeOnly && oneTimeBought;
		}

		public override void Interact()
		{
			// 自身のオーナーシップを安定させる
			if(Networking.LocalPlayer != null)
			{
				Networking.SetOwner( Networking.LocalPlayer, gameObject );
			}

			if(oneTimeBought && oneTimeOnly)
			{
				button.OnSoldout();
			}

            if (oneTimeOnly)
            {
                if (!oneTimeBought)
                {
					// 資金が足りるとき
					if (udonChips.money >= price)
					{
						// 資金を消費
						udonChips.money -= price;

						ChangeTargetsActive();

						// 購入成功イベント
						button.OnBuy();
						oneTimeBought = true;
					}
					else
					{
						// 資金不足による購入失敗イベント
						button.OnLackOfFunds();
					}
                }
                else
                {
					button.OnSoldout();
				}
            }
            else
            {
				// 資金が足りるとき
				if (udonChips.money >= price)
				{
					// 資金を消費
					udonChips.money -= price;

					ChangeTargetsActive();

					// 購入成功イベント
					button.OnBuy();
					oneTimeBought = true;
				}
				else
				{
					// 資金不足による購入失敗イベント
					button.OnLackOfFunds();
				}
			}
		}

		private void ChangeTargetsActive()
		{
			for(int i = 0; i < toggleTargets.Length; ++i)
			{
				toggleTargets[i].SetActive( !toggleTargets[i].activeSelf );
			}


			SyncSendChangeTargetsActive();
		}
		
		private void SyncSendChangeTargetsActive()
		{
			if(toggleTargets.Length != syncActives.Length)
			{
				syncActives = new bool[toggleTargets.Length];
			}

			for(int i = 0; i < syncActives.Length; ++i)
			{
				syncActives[i] = toggleTargets[i].activeSelf;
			}

			if(Networking.LocalPlayer != null)
			{
				Networking.SetOwner( Networking.LocalPlayer, this.gameObject );
			}
			RequestSerialization();
		}

		private void SyncReceiveChangeTargetActive()
		{
			if(toggleTargets.Length != syncActives.Length)
			{
				return;
			}

			for(int i = 0; i < syncActives.Length; ++i)
			{
				toggleTargets[i].SetActive( syncActives[i] );
			}
		}

		public override void OnDeserialization()
		{
			SyncReceiveChangeTargetActive();
		}
	}
}
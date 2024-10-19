
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UCS
{
	/// <summary>
	/// オブジェクトを購入するショップ
	/// </summary>
    public class UdonChipsInstantShop_ObjectPool : UdonSharpBehaviour
    {
        private UdonChips udonChips = null;

		[Tooltip( "購入価格" )]
		public float price = 100f;

		[Tooltip( "スポーンする場所" )]
		public Transform spawnAt = null;

		public UdonChipsObjectPool objectPool = null;

		public bool itemUseRigidbody = true;

        public UdonChipsShopAnimator shopAnimator = null;
		
        private void Start()
        {
			// UdonChipsがWorld内に存在すると想定し初期化
			var ucgo = GameObject.Find("UdonChips");

			if(ucgo == null)
			{
				Debug.LogError( "[ERROR] 'UdonChip'の名前がついたGameObjectがワールド内にありません。" );
			}

			udonChips = ucgo.GetComponent<UdonChips>();

			if(udonChips == null)
			{
				Debug.LogError( "[ERROR] 'UdonChip'の名前がついたGameObjectに、UdonChipsが設定されていません。" );
			}

			if(objectPool == null)
			{
				Debug.LogError( "[ERROR] objectPoolが設定されていません。" );
			}

			if(shopAnimator == null)
			{
				Debug.LogError( "[ERROR] eventTargetが設定されていません。" );
			}

			// spawnAtが未設定のとき、自身を対象とする（いちおう）
			if(spawnAt == null)
			{
				spawnAt = this.transform;
			}
		}

		private void Update()
		{
			shopAnimator.isLackOfFunds = price > udonChips.money;
			shopAnimator.isSoldout = !objectPool.CanSpawn();
		}

		public override void Interact()
        {
			// 自身のオーナーシップを安定させる
            if (Networking.LocalPlayer != null)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

			// 資金が足りるとき
            if (udonChips.money >= price)
            {
				// オブジェクトプールのオーナーを取得し、プールから取り出し処理を可能にする
				if(Networking.LocalPlayer != null)
				{
					Networking.SetOwner( Networking.LocalPlayer, objectPool.gameObject );
				}
				// プールからオブジェクトを取り出そうとする
				var obj = objectPool.TryToSpawn();

                if (obj != null)
                {
					// 取り出し成功
					if(Networking.LocalPlayer != null)
					{
						Networking.SetOwner( Networking.LocalPlayer, obj );
					}

					// 資金を消費
					udonChips.money -= price;

					// Unity的にGetComponent<Rigidbody>戻り値をNullチェックしたいが、UdonSharpがその変数のNullチェックでNullのとき例外を出すのでユーザーの事前設定が必要
                    if (itemUseRigidbody)
					{
						// Rigidbodyのとき
						var body = obj.GetComponent<Rigidbody>();
						body.position = obj.transform.position = spawnAt.position;
						body.rotation = obj.transform.rotation = spawnAt.rotation;
						body.velocity = Vector3.zero;
						body.angularVelocity = Vector3.zero;
					}
					else
					{
						// Transformのとき
                        obj.transform.position = spawnAt.position;
                        obj.transform.rotation = spawnAt.rotation;
                    }
					
					// 購入成功イベント
                    shopAnimator.OnBuy();
                }
                else
                {
					// 売り切れイベント
                    shopAnimator.OnSoldout();
                }
            }
            else
            {
				// 資金不足による購入失敗イベント
                shopAnimator.OnLackOfFunds();
            }
        }
    }

   


}

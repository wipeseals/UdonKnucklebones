
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UCS
{
	public class UdonChipsShopAnimator : UdonSharpBehaviour
	{
		[Header("== GameObject (On/Off) ==")]
		public GameObject[] activeOnBuy;
		public GameObject[] activeOnSoldout;
		public GameObject[] activeOnLackOfFunds;

		public GameObject[] activeIsIdle;
		public GameObject[] activeIsSoldout;
		public GameObject[] activeIsLackOfFunds;
		public GameObject[] activeIsNotIdle;
		public GameObject[] activeIsNotSoldout;
		public GameObject[] activeIsNotLackOfFunds;

		[Header( "== Audio ==" )]
		public AudioSource audioOnBuy;
		public AudioSource audioOnSoldout;
		public AudioSource audioOnLackOfFunds;

		[Header( "== Animator (Trigger) ==" )]
		public Animator animator;

		public string triggerOnBuy;
		public string triggerOnSoldout;
		public string triggerOnLackOfFunds;

		public string flagSoldout;
		public string flagLackOfFunds;

		[Header( "== Test (Animation Only) ==" )]
		public bool testBuy = false;
		public bool testSoldout = false;
		public bool testLackOfFunds = false;

		[HideInInspector]
		public bool isSoldout = false;
		[HideInInspector]
		public bool isLackOfFunds = false;

		/// <summary>
		/// 毎フレーム処理
		/// （テスト動作用）
		/// </summary>
		private void Update()
		{
			UpdateAnimator();

			SetGameObjectArrayActive( activeIsIdle, !isSoldout && !isLackOfFunds );
			SetGameObjectArrayActive( activeIsLackOfFunds, isLackOfFunds );
			SetGameObjectArrayActive( activeIsSoldout, isSoldout );
			SetGameObjectArrayActive( activeIsNotIdle, isSoldout || isLackOfFunds );
			SetGameObjectArrayActive( activeIsNotLackOfFunds, !isLackOfFunds );
			SetGameObjectArrayActive( activeIsNotSoldout, !isSoldout );

			UpdateTestEvents();
		}

		private void UpdateAnimator()
		{
			if(animator != null)
			{
				if(!string.IsNullOrEmpty( flagSoldout ))
				{
					animator.SetBool( flagSoldout, isSoldout );
				}

				if(!string.IsNullOrEmpty( flagLackOfFunds ))
				{
					animator.SetBool( flagLackOfFunds, isLackOfFunds );
				}
			}
		}

		private void UpdateTestEvents()
		{
			if(testBuy)
			{
				OnBuy();
				testBuy = false;
			}

			if(testSoldout)
			{
				OnSoldout();
				testSoldout = false;
			}

			if(testLackOfFunds)
			{
				OnLackOfFunds();
				testLackOfFunds = false;
			}
		}

		/// <summary>
		/// 購入成功時イベント
		/// </summary>
		public void OnBuy()
		{
			TryTriggerAnimator( triggerOnBuy );

			TryPlayAudio( audioOnBuy );

			SetGameObjectArrayActive( activeOnBuy, true );
			SetGameObjectArrayActive( activeOnSoldout, false );
			SetGameObjectArrayActive( activeOnLackOfFunds, false );
		}

		/// <summary>
		/// 売り切れ時イベント
		/// </summary>
		public void OnSoldout()
		{
			TryTriggerAnimator( triggerOnSoldout );

			TryPlayAudio( audioOnSoldout );

			SetGameObjectArrayActive( activeOnBuy, false );
			SetGameObjectArrayActive( activeOnSoldout, true );
			SetGameObjectArrayActive( activeOnLackOfFunds, false );
		}

		/// <summary>
		/// 資金不足による購入失敗時イベント
		/// </summary>
		public void OnLackOfFunds()
		{
			TryTriggerAnimator( triggerOnLackOfFunds );

			TryPlayAudio( audioOnLackOfFunds );

			SetGameObjectArrayActive( activeOnBuy, false );
			SetGameObjectArrayActive( activeOnSoldout, false );
			SetGameObjectArrayActive( activeOnLackOfFunds, true );
		}

		/// <summary>
		/// Animatorのトリガーを起動
		/// </summary>
		/// <param name="trigger"></param>
		private void TryTriggerAnimator( string trigger )
		{
			if(animator != null && !string.IsNullOrEmpty(trigger))
			{
				animator.SetTrigger( trigger );
			}
		}

		/// <summary>
		/// 音声を再生
		/// </summary>
		/// <param name="audio"></param>
		private void TryPlayAudio( AudioSource audio )
		{
			if(audio != null)
			{
				audio.Play();
			}
		}

		/// <summary>
		/// GameObject群のActiveを切り換え
		/// </summary>
		/// <param name="gameObjectArray"></param>
		/// <param name="active"></param>
		private void SetGameObjectArrayActive( GameObject[] gameObjectArray, bool active )
		{
			if(gameObjectArray == null)
			{
				return;
			}

			foreach(var go in gameObjectArray)
			{
				if(go != null)
				{
					go.SetActive( active );
				}
			}
		}
	}
}
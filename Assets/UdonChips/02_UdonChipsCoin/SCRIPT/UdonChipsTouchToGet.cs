
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UCS
{
	public class UdonChipsTouchToGet : UdonSharpBehaviour
	{
		private UCS.UdonChips udonChips = null;

		[SerializeField]
		[Tooltip( "入手金額" )]
		private int price = 100;

		[SerializeField]
		[Tooltip( "自動復活" )]
		private bool autoRespawn = true;

		[SerializeField]
		[Tooltip("Trueのとき、コインはグローバルとして振舞います。誰かがコインととると、他の人は取れません。\nFalseのとき、コインはローカルとして振舞います。自分がコインをとっても、他人には影響しません。")]
		private bool isGlobalTaken = false;

		[SerializeField]
		[Tooltip( "リスポーン間隔(秒)" )]
		private float respawnTime = 10f;
		private float respawnTimeRemain = 0f;

		[SerializeField]
		[Tooltip("サウンド")] 
		private AudioSource audioSource_coinGet;

		[SerializeField]
		private Animator animator = null;

		private const string paramName = "Visible";

		private void Start()
		{
			udonChips = GameObject.Find("UdonChips").GetComponent<UdonChips>();
			animator.SetBool( paramName, true );
		}

		private void Update()
		{
			if(autoRespawn && respawnTime > 0f)
			{
				respawnTimeRemain = Mathf.Max( 0f, respawnTimeRemain - Time.deltaTime );

				if(respawnTimeRemain <= 0f)
				{
					animator.SetBool( paramName, true );
				}
			}
		}

		public override void OnPlayerTriggerEnter( VRCPlayerApi player )
		{
			if(!player.isLocal)
			{
				return;
			}

			if(respawnTimeRemain > 0f)
			{
				return;
			}

			udonChips.money += price;
			if(isGlobalTaken)
			{
	#if UNITY_EDITOR
				Taken();
	#else
				SendCustomNetworkEvent( VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Taken" );
				Taken();
	#endif
			}
			else
			{
				Taken();
			}
		}

		public void Taken()
		{
			animator.SetBool( paramName, false );

			if(audioSource_coinGet != null)
            {
				audioSource_coinGet.Play();
            }

			respawnTimeRemain = respawnTime;
		}
	}
}

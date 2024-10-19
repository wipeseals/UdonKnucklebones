
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UCS
{
	public class UdonChipsInteractToGet : UdonSharpBehaviour
	{
		
		private UCS.UdonChips udonChips = null;

		[SerializeField]
		[Tooltip( "入手金額" )]
		private int price = 100;

		[SerializeField]
		[Tooltip("自動復活")]
		private bool autoRespawn = true;

		[SerializeField]
		[Tooltip("リスポーン間隔(秒)")]
		private float respawnTime = 10f;
		//[SerializeField]
		private float respawnTimeRemain = 0f;

		[SerializeField]
		private Animator animator = null;

		[SerializeField]
		[Tooltip("サウンド")]
		private AudioSource audioSource_coinGet;

		[SerializeField]
		[Tooltip("インタラクト用コライダー\nリスポーン待ちの間、このコライダーをOFFにします。")]
		private Collider interactCollider = null;

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
					interactCollider.enabled = true;
				}
			}
		}

		public override void Interact()
		{
			if(respawnTimeRemain > 0f)
			{
				return;
			}

			udonChips.money += price;
			animator.SetBool( paramName, false );
			respawnTimeRemain = respawnTime;
			interactCollider.enabled = false;

			if (audioSource_coinGet != null)
			{
				audioSource_coinGet.Play();
			}
		}
	}
}


using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UCS.Utility
{
	public class Despawner : UdonSharpBehaviour
	{
		public bool useTimelimitDespawn = true;
		public float autoDespawnTimeLimit = float.PositiveInfinity;
		private float autoDespawnTimeRemain = 0f;

		public bool useHeightDespawn = false;
		public float autoDespawnLowerHeight = -100f;
		public float autoDespawnHigherHeight = 1000f;

		private void OnEnable()
		{
			autoDespawnTimeRemain = autoDespawnTimeLimit;
		}

		private void Update()
		{
			if(useHeightDespawn)
			{
				if(transform.position.y > autoDespawnHigherHeight)
				{
					gameObject.SetActive( false );
				}

				if(transform.position.y < autoDespawnLowerHeight)
				{
					gameObject.SetActive( false );
				}
			}

			if(useTimelimitDespawn)
			{
				if(autoDespawnTimeRemain < 0f)
				{
					gameObject.SetActive( false );
				}

				autoDespawnTimeRemain -= Time.deltaTime;
			}
		}
	}
}

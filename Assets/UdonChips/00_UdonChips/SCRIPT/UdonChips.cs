using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UCS
{
	public class UdonChips : UdonSharpBehaviour
	{
		[Tooltip("現在の所持金（初期所持金）")]
		public float money = 1000;

		public string format = "$ {0:F0}";
	}
}

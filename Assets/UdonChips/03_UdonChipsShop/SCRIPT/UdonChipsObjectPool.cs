using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;

namespace UCS
{
	/// <summary>
	/// ※VRCObjectPool実装前の実装を引き継ぐインターフェース
	/// </summary>
	public class UdonChipsObjectPool : UdonSharpBehaviour
	{
		[SerializeField]
		private VRCObjectPool objectPool = null;
		
		/// <summary>
		/// オブジェクトプールからオブジェクトを引き出せるか？
		/// </summary>
		/// <returns></returns>
		public bool CanSpawn()
		{
			for(int i = 0; i < objectPool.Pool.Length; ++i)
			{
				if(!objectPool.Pool[i].activeSelf)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// オブジェクトプールからオブジェクトを引き出す
		/// </summary>
		/// <returns></returns>
		public GameObject TryToSpawn()
		{
			return objectPool.TryToSpawn();
		}

		/// <summary>
		/// オブジェクトをプールに返す
		/// </summary>
		/// <param name="obj"></param>
		public void Return( GameObject obj )
		{
			objectPool.Return( obj );
		}
	}
}

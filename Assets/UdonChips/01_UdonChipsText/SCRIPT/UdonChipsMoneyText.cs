
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRC.SDKBase;
using VRC.Udon;

namespace UCS
{
	public class UdonChipsMoneyText : UdonSharpBehaviour
	{
		[SerializeField]
		private TextMeshProUGUI textMeshPro = null;

		[SerializeField]
		private Text text = null;

		[SerializeField]
		private string format = string.Empty;

		private UdonChips udonChips = null;

		[SerializeField]
		private bool dramRoll = false;

		[SerializeField]
		private float dramRollMinPerSec = 10;

		[SerializeField]
		private float dramRollFactorPerSec = 1f;

		float lastMoney = 0f;
		bool firstTake = true;

		private void Start()
		{
			udonChips = GameObject.Find("UdonChips").GetComponent<UdonChips>();

			if (textMeshPro == null)
			{
				textMeshPro = GetComponent<TextMeshProUGUI>();
			}

			if(text == null)
			{
				text = GetComponent<Text>();
			}
		}

		private void OnEnable()
		{
			firstTake = true;
		}

		private void Update()
		{
			UpdateText();
		}

		private void UpdateText()
		{
			if(firstTake)
			{
				firstTake = false;
				lastMoney = udonChips.money;
				ApplyText();
			}
		
			// 変更があったときだけ表示を変える
			if(lastMoney != udonChips.money)
			{
				if(dramRoll)
				{
					float delta = lastMoney - udonChips.money;
				
					float maxDelta = Mathf.Max( dramRollMinPerSec, Mathf.Abs( delta * dramRollFactorPerSec ) ) * Time.deltaTime;
					lastMoney = Mathf.MoveTowards( lastMoney, udonChips.money, maxDelta );
				}
				else
				{
					lastMoney = udonChips.money;
				}
				ApplyText();
			}
		}

		private void ApplyText()
		{
			if(string.IsNullOrEmpty( format ))
			{
				format = udonChips.format;
			}

			if(text != null)
			{
				text.text = string.Format( format, lastMoney );
			}
			if(textMeshPro != null)
			{
				textMeshPro.text = string.Format( format, lastMoney );
			}
		}
	}
}

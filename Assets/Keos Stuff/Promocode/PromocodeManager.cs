using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using TMPro;

public class PromocodeManager : MonoBehaviour
{
    [System.Serializable]
    public struct Promocode
    {
        public string Code;
        public UnityEvent OnRedeem;
    }

    public List<Promocode> Promocodes = new List<Promocode>();

    public TextMeshPro CurrentInputText;

    [HideInInspector]
    public string input = "";

    public void TryCollect()
    {
        foreach (var c in Promocodes)
        {
            if (input == c.Code && PlayerPrefs.GetInt("PromoCodes" + c.Code) != 1)
            {
                if (c.OnRedeem != null)
                {
                    c.OnRedeem.Invoke();
                }
                print("Redeemed Code: " + c.Code);
                PlayerPrefs.SetInt("PromoCodes" + c.Code, 1);
            }
            else
            {
                print("Code: " + c.Code + " Is Invalid");
            }
            input = string.Empty;
        }
    }
}

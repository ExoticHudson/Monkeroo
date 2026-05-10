using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

public class EasyCurrencyHandling : MonoBehaviour
{
    public string CurrencyCode;

    public void AddCurrency(int AddAmount)
    {
        var request = new AddUserVirtualCurrencyRequest
        {
            VirtualCurrency = CurrencyCode,
            Amount = AddAmount
        };

        PlayFabClientAPI.AddUserVirtualCurrency(request, OnCurrencyAdded, OnError);
    }

    public void SubtractCurrency(int SubtractAmount)
    {
        var request = new SubtractUserVirtualCurrencyRequest
        {
            VirtualCurrency = CurrencyCode,
            Amount = SubtractAmount
        };

        PlayFabClientAPI.SubtractUserVirtualCurrency(request, OnCurrencySubtracted, OnError);
    }

    private void OnCurrencyAdded(ModifyUserVirtualCurrencyResult result)
    {
        Debug.Log("Added the currency");
    }

    private void OnCurrencySubtracted(ModifyUserVirtualCurrencyResult result)
    {
        Debug.Log("Subtracted the currency");
    }

    private void OnError(PlayFabError error)
    {
        Debug.Log("An error occurred while modifying the currency");
    }
}

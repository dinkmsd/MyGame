using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PlayfabManager : MonoBehaviour
{
    public TMP_InputField nameInput;
    public TMP_Text displayNameText;
    public TMP_Text playFabIdText;
    public TMP_Text killsText;
    public GameObject rowPrefab;
    public Transform rowsParent;


    [Header("Windows")]
    public GameObject nameWindow;
    public GameObject leaderboardWindow;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Login();
    }

    // Update is called once per frame
    void Login()
    {
        var request = new LoginWithCustomIDRequest
        {
            CustomId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true
            }
        };
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnError);
    }

    void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("Successfully logged in");
        playFabIdText.text = "PlayFab ID: " + result.PlayFabId;
        string name = null;
        if (result.InfoResultPayload.PlayerProfile != null)
            name = result.InfoResultPayload.PlayerProfile.DisplayName;
        if (name == null)
        {
            leaderboardWindow.SetActive(false);
            nameWindow.SetActive(true);
        }
        else
        {
            displayNameText.text = "Welcome, " + name;
            nameWindow.SetActive(false);
            leaderboardWindow.SetActive(true);
        }
        IncrementKills();
    }

    public void SubmitNameButton()
    {
        var request = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = nameInput.text,
        };
        PlayFabClientAPI.UpdateUserTitleDisplayName(request, OnDispayNameUpdate, OnError);
    }

    void OnDispayNameUpdate(UpdateUserTitleDisplayNameResult result)
    {
        Debug.Log("Name updated successfully");
        nameWindow.SetActive(false);
        leaderboardWindow.SetActive(true);
    }

    public void IncrementKills()
    {
        ExecuteCloudScript("IncrementPlayerStat", new Dictionary<string, object> { { "statName", "Kills" } });
    }

    void ExecuteCloudScript(string functionName, Dictionary<string, object> args)
    {
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = functionName,
            FunctionParameter = args
        };
        PlayFabClientAPI.ExecuteCloudScript(request, OnCloudScriptSuccess, OnError);
    }

    void OnCloudScriptSuccess(ExecuteCloudScriptResult result)
    {
        // Fetch updated stats
        GetPlayerStats();
    }

    void GetPlayerStats()
    {
        PlayFabClientAPI.GetPlayerStatistics(new GetPlayerStatisticsRequest(), OnStatsReceived, OnStatsFailure);
    }

    void OnStatsReceived(GetPlayerStatisticsResult result)
    {
        Debug.Log("Retrieved player statistics");
        foreach (var stat in result.Statistics)
        {
            if (stat.StatisticName == "Kills")
                killsText.text = "Kills: " + stat.Value;
        }
    }

    void OnStatsFailure(PlayFabError error)
    {
        Debug.LogError("Failed to fetch stats: " + error.ErrorMessage);
    }

    public void FetchLeaderboard()
    {
        var args = new Dictionary<string, object> { { "statName", "Kills" }, { "maxResults", 10 } };
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "GetTopScores",
            FunctionParameter = args
        };
        PlayFabClientAPI.ExecuteCloudScript(request, OnCloudScriptGetTopSuccess, OnError);
    }

    void OnCloudScriptGetTopSuccess(ExecuteCloudScriptResult result)
    {
        // ExecuteCloudScriptResult
        // GetLeaderboardResult
        // var leaderboardDic = result as Dictionary<string, object>;
        var leaderboardResult = result.FunctionResult as GetLeaderboardResult;

        foreach (var item in leaderboardResult.Leaderboard)
        {
            GameObject newGo = Instantiate(rowPrefab, rowsParent);
            Text[] texts = newGo.GetComponentsInChildren<Text>();
            texts[0].text = item.Position.ToString();
            texts[1].text = item.PlayFabId;
            texts[2].text = item.StatValue.ToString();
            Debug.Log(string.Format("PLACE: {0} | ID: {1} | VALUE: {2}", item.Position, item.PlayFabId, item.StatValue));
        }
    }
    void OnError(PlayFabError error)
    {
        Debug.Log("Error while logging in/creating account!");
        Debug.Log(error.GenerateErrorReport());
    }
}

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
        ExecuteCloudScript("GetTopScores", new Dictionary<string, object> { { "statName", "Kills" }, { "maxResults", 10 } });
    }

    void OnCloudScriptSuccess(ExecuteCloudScriptResult result)
    {
        if (result.FunctionName == "GetTopScores")
        {
            // Clear existing entries
            foreach (Transform child in leaderboardContent)
                Destroy(child.gameObject);

            // Parse leaderboard data
            var entries = result.FunctionResult.Data as List<object>;
            foreach (var entry in entries)
            {
                var entryDict = entry as Dictionary<string, object>;
                var rank = entryDict["Position"] as int?;
                var name = entryDict["DisplayName"] as string;
                var score = entryDict["StatValue"] as int?;

                // Create UI entry
                var entryObj = Instantiate(leaderboardEntryPrefab, leaderboardContent);
                entryObj.GetComponent<Text>().text = $"{rank}. {name}: {score}";
            }
        }
    }
    void OnError(PlayFabError error)
    {
        Debug.Log("Error while logging in/creating account!");
        Debug.Log(error.GenerateErrorReport());
    }
}

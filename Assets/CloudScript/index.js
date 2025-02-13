handlers.IncrementPlayerStat = (args) => {
  const statName = args.statName || "Kills";
  const incrementValue = args.incrementValue || 1;
  const playerId = currentPlayerId;
  const getStatsRequest = {
    PlayFabId: playerId,
  };
  const currentStats = server.GetPlayerStatistics(getStatsRequest);
  var currentScore = 0;

  for (let i = 0; i < currentStats.Statistics.length; i++) {
    if (currentStats.Statistics[i].StatisticName === statName) {
      currentScore = currentStats.Statistics[i].Value;
      break;
    }
  }

  const newScore = currentScore + incrementValue;

  const updateRequest = {
    PlayFabId: playerId,
    Statistics: [
      {
        StatisticName: statName,
        Value: newScore,
      },
    ],
  };
  server.UpdatePlayerStatistics(updateRequest);
  return { message: "Stat incremented successfully!" };
};

handlers.GetTopScores = (args) => {
  const statName = args.statName || "Kills";
  const maxResults = args.maxResults || 10;
  const leaderboardRequest = {
    StatisticName: statName,
    MaxResultsCount: maxResults,
  };
  const leaderboard = server.GetLeaderboard(leaderboardRequest);
  return leaderboard;
};

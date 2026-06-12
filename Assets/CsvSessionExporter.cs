using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// One row of session data for CSV export (registration, grid settings, stats).
/// </summary>
public struct SessionCsvPayload
{
    public string EndReason;
    public string PlayerName;
    public int PlayerAge;
    public string PlayerGender;
    public int IsRegistered;
    public int GameDurationSeconds;
    public int GridSize;
    public int PathLength;
    public int NumberOfTurns;
    public float DisplayTime;
    public float SegmentDelay;
    public int FlipColors;
    public float DelayBeforeRecall;
    public int NumberOfSnakes;
    public int NumberOfDummySnakes;
    public int ChancesPerPath;
    public int PathsTotal;
    public int Success;
    public int Fail;
    public int SecondsPlayed;
    public float DifficultyScore;
    public float SuccessRate;
    public float PerformanceGrade;
    // New fields for stage system
    public int MaxStageReached;
    public float BenchmarkScore;  // Success rate on stage 0 (benchmark)
    public int MlAdaptive;
}

/// <summary>
/// Appends session rows to per-user CSV: {FirstName}_{LastName}_sessions.csv
/// </summary>
public static class CsvSessionExporter
{
    const string Header =
        "SessionNumber," +
        "PlayerName," +
        "PlayerAge [y]," +
        "PlayerGender," +
        "IsRegistered [0/1]," +
        "Time," +
        "GameDuration [s]," +
        "PathsTotal [-]," +
        "Success [-]," +
        "Fail [-]," +
        "EndReason [-]," +
        "SecondsPlayed [s]," +
        "SuccessRate [0-1]," +
        "MaxStageReached [-]," +
        "BenchmarkScore [0-1]," +
        "MlAdaptive [0/1]";

    /// <summary>
    /// Sequential index for the next session row in this player's <c>*_sessions.csv</c>
    /// (matches <see cref="AppendRow"/>). Use for path rows in the same play session.
    /// </summary>
    public static int GetNextSessionNumber(string playerName)
    {
        return ComputeNextSessionNumber(GetCsvPath(playerName));
    }

    /// <summary>
    /// Get the CSV file path for a specific player
    /// </summary>
    public static string GetCsvPath(string playerName)
    {
        // Sanitize player name for filename
        string safeName = SanitizeFileName(playerName);
        if (string.IsNullOrEmpty(safeName))
            safeName = "Unknown_Player";
            
        string fileName = safeName + "_sessions.csv";
        
#if UNITY_EDITOR || UNITY_STANDALONE
        string parent = Directory.GetParent(Application.dataPath)?.FullName;
        if (!string.IsNullOrEmpty(parent))
        {
            string dataFolder = Path.Combine(parent, "Data");
            if (!Directory.Exists(dataFolder))
                Directory.CreateDirectory(dataFolder);
            return Path.Combine(dataFolder, fileName);
        }
#endif
        string persistentDataFolder = Path.Combine(Application.persistentDataPath, "Data");
        if (!Directory.Exists(persistentDataFolder))
            Directory.CreateDirectory(persistentDataFolder);
        return Path.Combine(persistentDataFolder, fileName);
    }
    
    /// <summary>
    /// Sanitize a string for use as a filename
    /// </summary>
    static string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "";
            
        // Replace spaces with underscores
        string result = name.Replace(" ", "_");
        
        // Remove invalid filename characters
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            result = result.Replace(c.ToString(), "");
        }
        
        return result;
    }

    public static void AppendRow(SessionCsvPayload p)
    {
        string path = GetCsvPath(p.PlayerName);
        DateTime localNow = DateTime.Now;
        int sessionNumber = ComputeNextSessionNumber(path);

        string[] cells =
        {
            sessionNumber.ToString(CultureInfo.InvariantCulture),
            Escape(p.PlayerName),
            p.PlayerAge.ToString(CultureInfo.InvariantCulture),
            Escape(p.PlayerGender),
            p.IsRegistered.ToString(CultureInfo.InvariantCulture),
            Escape(FormatSimpleLocalTime(localNow)),
            p.GameDurationSeconds.ToString(CultureInfo.InvariantCulture),
            p.PathsTotal.ToString(CultureInfo.InvariantCulture),
            p.Success.ToString(CultureInfo.InvariantCulture),
            p.Fail.ToString(CultureInfo.InvariantCulture),
            Escape(p.EndReason),
            p.SecondsPlayed.ToString(CultureInfo.InvariantCulture),
            p.SuccessRate.ToString("F3", CultureInfo.InvariantCulture),
            p.MaxStageReached.ToString(CultureInfo.InvariantCulture),
            p.BenchmarkScore.ToString("F3", CultureInfo.InvariantCulture),
            p.MlAdaptive.ToString(CultureInfo.InvariantCulture)
        };

        string line = string.Join(",", cells);
        bool writeHeader = !File.Exists(path) || new FileInfo(path).Length == 0;

        try
        {
            using (var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                if (writeHeader)
                    writer.WriteLine(Header);
                writer.WriteLine(line);
            }

            Debug.Log("Session CSV row written to: " + path);
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to write session CSV: " + ex.Message);
        }
    }

    /// <summary>Like 19-4-26 19:19 — local calendar, no seconds.</summary>
    static string FormatSimpleLocalTime(DateTime local)
    {
        int y2 = local.Year % 100;
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0}-{1}-{2} {3}:{4}",
            local.Day,
            local.Month,
            y2.ToString("00", CultureInfo.InvariantCulture),
            local.Hour,
            local.Minute.ToString("00", CultureInfo.InvariantCulture));
    }

    static int ComputeNextSessionNumber(string path)
    {
        if (!File.Exists(path))
            return 1;

        try
        {
            string[] lines = File.ReadAllLines(path);
            if (lines.Length == 0)
                return 1;
            string h = lines[0];
            if (h.StartsWith("SessionNumber", StringComparison.Ordinal) ||
                h.StartsWith("PlayerNumber", StringComparison.Ordinal))
                return lines.Length;
            return lines.Length + 1;
        }
        catch
        {
            return 1;
        }
    }

    static string Escape(string value)
    {
        if (value == null)
            value = string.Empty;
        bool mustQuote = value.IndexOfAny(new[] { ',', '"', '\r', '\n', '\t' }) >= 0;
        string escaped = value.Replace("\"", "\"\"");
        if (mustQuote)
            return "\"" + escaped + "\"";
        return escaped;
    }
}

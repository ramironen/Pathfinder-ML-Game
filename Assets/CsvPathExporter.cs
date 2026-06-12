using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// One row of path data for CSV export (per-path attempt for ML).
/// </summary>
public struct PathCsvPayload
{
    public string PlayerName;
    public int PlayerAge;
    public string PlayerGender;
    public int SessionNumber;
    public string SessionDate;
    public int PathNumber;
    public int Stage;
    public string StageName;
    public int StageAttempt;
    public int GridSize;
    public int PathLength;
    public int NumberOfTurns;
    public int NumberOfSnakes;
    public int NumberOfDummySnakes;
    public int FlipColors;
    public float DisplayTime;
    public float SegmentDelay;
    public float DelayBeforeRecall;
    public int Success;
    public float PathDurationMs;
    public float FirstMoveDelayMs;
    public float TimeInSessionMs;
    public int MlAdaptive;
    public float PredictedPSuccess;
}

/// <summary>
/// Appends path rows to per-user CSV files: {FirstName}_{LastName}_paths.csv
/// </summary>
public static class CsvPathExporter
{
    const string Header =
        "SessionNumber," +
        "SessionDate," +
        "PathNumber," +
        "Stage," +
        "StageName," +
        "StageAttempt," +
        "GridSize," +
        "PathLength," +
        "NumberOfTurns," +
        "NumberOfSnakes," +
        "NumberOfDummySnakes," +
        "FlipColors," +
        "DisplayTime," +
        "SegmentDelay," +
        "DelayBeforeRecall," +
        "Success," +
        "PathDurationMs," +
        "FirstMoveDelayMs," +
        "TimeInSessionMs," +
        "MlAdaptive," +
        "PredictedPSuccess";

    /// <summary>
    /// Get the CSV file path for a specific player
    /// </summary>
    public static string GetCsvPath(string playerName)
    {
        // Sanitize player name for filename
        string safeName = SanitizeFileName(playerName);
        if (string.IsNullOrEmpty(safeName))
            safeName = "Unknown_Player";
            
        string fileName = safeName + "_paths.csv";
        
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

    public static void AppendRow(PathCsvPayload p)
    {
        string path = GetCsvPath(p.PlayerName);

        string[] cells =
        {
            p.SessionNumber.ToString(CultureInfo.InvariantCulture),
            Escape(p.SessionDate),
            p.PathNumber.ToString(CultureInfo.InvariantCulture),
            p.Stage.ToString(CultureInfo.InvariantCulture),
            Escape(p.StageName),
            p.StageAttempt.ToString(CultureInfo.InvariantCulture),
            p.GridSize.ToString(CultureInfo.InvariantCulture),
            p.PathLength.ToString(CultureInfo.InvariantCulture),
            p.NumberOfTurns.ToString(CultureInfo.InvariantCulture),
            p.NumberOfSnakes.ToString(CultureInfo.InvariantCulture),
            p.NumberOfDummySnakes.ToString(CultureInfo.InvariantCulture),
            p.FlipColors.ToString(CultureInfo.InvariantCulture),
            p.DisplayTime.ToString("F2", CultureInfo.InvariantCulture),
            p.SegmentDelay.ToString("F2", CultureInfo.InvariantCulture),
            p.DelayBeforeRecall.ToString("F2", CultureInfo.InvariantCulture),
            p.Success.ToString(CultureInfo.InvariantCulture),
            p.PathDurationMs.ToString("F0", CultureInfo.InvariantCulture),
            p.FirstMoveDelayMs.ToString("F0", CultureInfo.InvariantCulture),
            p.TimeInSessionMs.ToString("F0", CultureInfo.InvariantCulture),
            p.MlAdaptive.ToString(CultureInfo.InvariantCulture),
            p.PredictedPSuccess.ToString("F3", CultureInfo.InvariantCulture)
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

            Debug.Log("Path CSV row written to: " + path);
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to write path CSV: " + ex.Message);
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

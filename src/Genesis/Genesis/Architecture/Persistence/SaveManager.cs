using System;
using System.IO;
using System.Text.Json;
using Arch.Core;
using Genesis.Architecture.Audio;
using Genesis.Persistence.Meta;
using Genesis.Persistence.Run;

namespace Genesis.Architecture.Persistence;

/// <summary>
/// Serves as the central facade for all game persistence operations (Saving/Loading).
/// </summary>
public static class SaveManager
{
    // Strategy for encoding/decoding save data. Defaults to Base64.
    // Switch to PlainTextStrategy for debugging.
    private static ISaveEncodingStrategy EncodingStrategy { get; set; } = new Base64Strategy();

    private const string MetaDataBaseName = "meta";
    public const int MaxSaveSlots = 3;

    private static readonly JsonSerializerOptions sJsonOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new PointConverter(),
            new Vector2Converter()
        }
    };

    public static void SetEncodingStrategy(ISaveEncodingStrategy strategy) => EncodingStrategy = strategy;

    private static string GetMetaFilePath() => $"{MetaDataBaseName}{EncodingStrategy.Extension}";

    private static string GetFilePathForSlot(int slotIndex)
    {
        var isValid = slotIndex is >= 0 and <= MaxSaveSlots;
        return isValid ? $"save_{slotIndex + 1}{EncodingStrategy.Extension}" : throw new ArgumentOutOfRangeException(
            paramName: nameof(slotIndex),
            message: "Slot index must be between 0 and " + MaxSaveSlots
        );
    }

    private static void WriteToFile(string filePath, string content)
    {
        var encoded = EncodingStrategy.Encode(content);
        File.WriteAllText(filePath, encoded);
    }

    private static string ReadFromFile(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        var content = File.ReadAllText(filePath);
        return string.IsNullOrWhiteSpace(content) ? null : EncodingStrategy.Decode(content);
    }

    public static void SaveRun(World world, int slotIndex)
    {
        var saveData = RunSaveData.Fetch(world);
        var filePath = GetFilePathForSlot(slotIndex);
        var jsonString = JsonSerializer.Serialize(saveData, sJsonOptions);
        WriteToFile(filePath, jsonString);
    }

    public static bool DeleteRun(int slotIndex)
    {
        var filePath = GetFilePathForSlot(slotIndex);
        if (!File.Exists(filePath)) {return false;}
        File.Delete(filePath);
        return true;
    }

    public static RunSaveData LoadRun(int slotIndex)
    {
        var filePath = GetFilePathForSlot(slotIndex);
        
        try
        {
            var jsonString = ReadFromFile(filePath);
            return string.IsNullOrWhiteSpace(jsonString) ? null : JsonSerializer.Deserialize<RunSaveData>(jsonString, sJsonOptions);
        }
        catch (Exception e) when (e is IOException or JsonException)
        {
            Console.WriteLine($"Error loading save slot {slotIndex}: {e.Message}. File may be corrupted.");
            return null;
        }
    }

    public static void SaveMeta(MetaData dataData)
    {
        var jsonString = JsonSerializer.Serialize(dataData, sJsonOptions);
        WriteToFile(GetMetaFilePath(), jsonString);
    }

    public static MetaData LoadMeta()
    {
        var filePath = GetMetaFilePath();

        try
        {
            var jsonString = ReadFromFile(filePath);
            if (string.IsNullOrWhiteSpace(jsonString)) return MetaData.NewDefault();
            
            var metaData = JsonSerializer.Deserialize<MetaData>(jsonString, sJsonOptions);
            

            if (metaData == null)
            {
                return MetaData.NewDefault();
            }
            
            if (metaData.Statistics.Equals(default(GlobalStatsData)))
            {
                metaData.Statistics = new GlobalStatsData();
            }
            
            metaData.Achievements ??= new GlobalAchievementsData();
            metaData.AudioSettings ??= new AudioSettings();
            metaData.TutorialSettings ??= new TutorialSettings();
            
            return metaData;
        }
        catch (Exception e) when (e is IOException or JsonException)
        {
            Console.WriteLine($"Error loading meta data: {e.Message}. Starting new profile.");
            return MetaData.NewDefault();
        }
    }
}
using Sirenix.Serialization;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
public class LevelDataIO
{
    private static string savePath = Application.persistentDataPath + "/myLevelData.bytes";

    public static string SaveLevelData(LevelData levelData, string levelName = "", bool saveToStreamingAssets = false, bool saveCurrent = false)
    {
        string fileName = "";
        if (levelName.Length > 0)
        {
            fileName = levelName;
        }
        if (!saveCurrent)
        {
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            fileName += timestamp;
        }

        if (fileName.Length > 50)
        {
            fileName = fileName.Substring(0, 50);
        }

        string fullFileName = $"mLD_{fileName}.json";
        string fullPath = saveToStreamingAssets
        ? Path.Combine(Application.streamingAssetsPath, fullFileName)
        : Path.Combine(Application.persistentDataPath, fullFileName);


        var serializableData = LevelDataConverter.FromLevelData(levelData);

        byte[] bytes = SerializationUtility.SerializeValue(serializableData, DataFormat.JSON);
        File.WriteAllBytes(fullPath, bytes);
        Debug.Log($"LevelData saved to {fullPath}");
        return fileName;
    }

    public static void LoadLevelData(LevelData levelData, string path = null)
    {
        if (string.IsNullOrEmpty(path)) path = savePath;

        if (!File.Exists(path))
        {
            Debug.LogWarning($"Save file not found: {path}");
            return;
        }

        byte[] bytes = File.ReadAllBytes(path);
        var serializableData = SerializationUtility.DeserializeValue<SerializableLevelData>(bytes, DataFormat.JSON);
        LevelDataConverter.ApplyToLevelData(serializableData, levelData);
        Debug.Log($"LevelData loaded from {path}");
    }

    public static void LoadLevelDataByName(LevelData levelData, string levelName, bool loadFromStreamingAssets = false)
    {
        string fileName = $"mLD_{levelName}.json";
        string fullPath = loadFromStreamingAssets
        ? Path.Combine(Application.streamingAssetsPath, fileName)
        : Path.Combine(Application.persistentDataPath, fileName);
        //string fullPath = Path.Combine(Application.persistentDataPath, fileName);
        LoadLevelData(levelData, fullPath);
    }

    public static List<string> GetAllSavedLevelFiles(bool fromStreamingAssets = false)
    {

        string[] files = new string[0];
        if (fromStreamingAssets)
        {
            files = Directory.GetFiles(Application.streamingAssetsPath, "mLD_*.json");
        }
        else
            files = Directory.GetFiles(Application.persistentDataPath, "mLD_*.json");

        var fileNames = files
        .OrderByDescending(File.GetCreationTime)
        .Select(path =>
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            return fileName.Substring("mLD_".Length);
        })
        .ToList();
        return fileNames;
    }

    public static void DeleteLevelDataByName(string levelName, bool fromStreamingAssets = false)
    {
        string fileName = $"mLD_{levelName}.json";
        string fullPath = fromStreamingAssets
        ? Path.Combine(Application.streamingAssetsPath, fileName)
        : Path.Combine(Application.persistentDataPath, fileName);
        //string fullPath = Path.Combine(Application.persistentDataPath, fileName);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            Debug.Log($"Deleted level file: {fullPath}");
        }
        else
        {
            Debug.LogWarning($"File not found, cannot delete: {fullPath}");
        }
    }

    public static void RenameLevelData(string oldLevelName, string newLevelName, bool fromStreamingAssets = false)
    {
        string oldFileName = $"mLD_{oldLevelName}.json";
        string newFileName = $"mLD_{newLevelName}.json";

        string oldFullPath = fromStreamingAssets
        ? Path.Combine(Application.streamingAssetsPath, oldFileName)
        : Path.Combine(Application.persistentDataPath, oldFileName);

        string newFullPath = fromStreamingAssets
        ? Path.Combine(Application.streamingAssetsPath, newFileName)
        : Path.Combine(Application.persistentDataPath, newFileName);

        //string oldFullPath = Path.Combine(Application.persistentDataPath, oldFileName);
        //string newFullPath = Path.Combine(Application.persistentDataPath, newFileName);

        if (File.Exists(oldFullPath))
        {
            if (File.Exists(newFullPath))
            {
                Debug.LogError($"Cannot rename: target file already exists at {newFullPath}");
                return;
            }

            File.Move(oldFullPath, newFullPath);
            Debug.Log($"Renamed level file from {oldFullPath} to {newFullPath}");
        }
        else
        {
            Debug.LogWarning($"Original level file not found: {oldFullPath}");
        }
    }

}

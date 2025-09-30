using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class PlayerAcommSaveData
{
    public List<string> keys;
    public List<string> values;
}

public class PlayerAcommodationsSaveInterface : MonoBehaviour
{
    public string key;
    public bool loadStringOnEnable = false;
    public bool loadIntOnEnable = false;
    public bool loadFloatOnEnable = false;
    public bool loadBoolOnEnable = false;
    public string fallbackString = "";
    public int fallbackInt = 0;
    public float fallbackFloat = 0;
    public bool fallbackBool = false;
    public UnityEvent<string> onLoadString;
    public UnityEvent<int> onLoadInt;
    public UnityEvent<float> onLoadFloat;
    public UnityEvent<bool> onLoadBool;

    public void SaveString(string s) => PlayerAcommSaveGlobal.SetString(key, s);
    public void SaveInt(int i) => PlayerAcommSaveGlobal.SetInt(key, i);
    public void SaveFloat(float f) => PlayerAcommSaveGlobal.SetFloat(key, f);
    public void SaveBool(bool b) => PlayerAcommSaveGlobal.SetBool(key, b);

    public void LoadString()
    {
        if (PlayerAcommSaveGlobal.GetString(key, out string result))
            onLoadString.Invoke(result);
        else
            onLoadString.Invoke(fallbackString);
    }
    public void LoadInt()
    {
        if (PlayerAcommSaveGlobal.GetInt(key, out int result))
            onLoadInt.Invoke(result);
        else
            onLoadInt.Invoke(fallbackInt);
    }
    public void LoadFloat()
    {
        if (PlayerAcommSaveGlobal.GetFloat(key, out float result))
            onLoadFloat.Invoke(result);
        else
            onLoadFloat.Invoke(fallbackFloat);
    }
    public void LoadBool()
    {
        if (PlayerAcommSaveGlobal.GetBool(key, out bool result))
            onLoadBool.Invoke(result);
        else
            onLoadBool.Invoke(fallbackBool);
    }

    public void CommitSaveData() => PlayerAcommSaveGlobal.CommitSaveData();
    public void CommitSaveData(string saveName) => PlayerAcommSaveGlobal.CommitSaveData(saveName);
    public void LoadSaveData() => PlayerAcommSaveGlobal.LoadSaveData();
    public void LoadSaveData(string saveName) => PlayerAcommSaveGlobal.LoadSaveData(saveName);
    
    public void LoadSlot(int slot)
    {
        PlayerAcommSaveGlobal.activeSaveSlot = slot;
        LoadSaveData();
    }

    public void SetSlot0Name(string newName) => SetSlotName(0, newName);
    public void SetSlot1Name(string newName) => SetSlotName(1, newName);
    public void SetSlot2Name(string newName) => SetSlotName(2, newName);

    private void SetSlotName(int slot, string newName)
    {
        PlayerAcommSaveGlobal.RenameSave(PlayerAcommSaveGlobal.saveSlots[slot], newName);
        PlayerPrefs.SetString("saveName_" + slot, newName);
    }
}

public static class PlayerAcommSaveGlobal
{
    public static Dictionary<string, string> activeSaveData;
    public static int activeSaveSlot = 0;
    public static string[] saveSlots = new string[] { "Save 0", "Save 1", "Save 2" };

    public static bool GetString(string key, out string result)
    {
        return GetValue(key, out result);
    }

    public static bool GetInt(string key, out int result)
    {
        bool success = GetValue(key, out string rawValue);
        result = success ? int.Parse(rawValue) : 0;
        return success;
    }

    public static bool GetFloat(string key, out float result)
    {
        bool success = GetValue(key, out string rawValue);
        result = success ? float.Parse(rawValue) : 0;
        return success;
    }

    public static bool GetBool(string key, out bool result)
    {
        bool success = GetValue(key, out string rawValue);
        result = success ? bool.Parse(rawValue) : false;
        return success;
    }

    public static bool GetValue(string key, out string result)
    {
        try
        {
            result = activeSaveData[key];
            return true;
        }
        catch
        {
            result = "";
            return false;
        }
    }

    public static void SetString(string key, string value)
    {
        if (activeSaveData.ContainsKey(key))
            activeSaveData[key] = value;
        else
            activeSaveData.Add(key, value);
    }

    public static void SetInt(string key, int value)
    {
        if (activeSaveData.ContainsKey(key))
            activeSaveData[key] = value.ToString();
        else
            activeSaveData.Add(key, value.ToString());
    }

    public static void SetFloat(string key, float value)
    {
        if (activeSaveData.ContainsKey(key))
            activeSaveData[key] = value.ToString();
        else
            activeSaveData.Add(key, value.ToString());
    }

    public static void SetBool(string key, bool value)
    {
        if (activeSaveData.ContainsKey(key))
            activeSaveData[key] = value.ToString();
        else
            activeSaveData.Add(key, value.ToString());
    }

    public static string ConvertSaveToJson()
    {
        PlayerAcommSaveData saveData = new PlayerAcommSaveData();
        saveData.keys = activeSaveData.Keys.ToList();
        saveData.values = activeSaveData.Values.ToList();
        return JsonUtility.ToJson(saveData);
    }

    public static Dictionary<string, string> ConvertJsonToSave(string json)
    {
        PlayerAcommSaveData saveData = (PlayerAcommSaveData)JsonUtility.FromJson(json, typeof(PlayerAcommSaveData));
        Dictionary<string, string> result = new Dictionary<string, string>();
        for (int i = 0; i < saveData.keys.Count; i++)
            result.Add(saveData.keys[i], saveData.values[i]);
        return result;
    }

    public static void CommitSaveData() => CommitSaveData(saveSlots[activeSaveSlot]);

    public static void CommitSaveData(string saveName)
    {
        string filename = "save_" + saveName;
        string filepath = Path.Combine(Application.persistentDataPath, filename);
        File.WriteAllText(filepath, ConvertSaveToJson());
    }

    public static bool LoadSaveData() => LoadSaveData(saveSlots[activeSaveSlot]);

    public static bool LoadSaveData(int saveSlot) => LoadSaveData(saveSlots[saveSlot]);

    public static bool LoadSaveData(string saveName)
    {
        string filename = "save_" + saveName;
        string filepath = Path.Combine(Application.persistentDataPath, filename);
        if (!File.Exists(filepath))
            return false;

        activeSaveData = ConvertJsonToSave(File.ReadAllText(filepath));
        return true;
    }

    public static bool RenameSave(string originalName, string newName)
    {
        string filenameOld = "save_" + originalName;
        string filenameNew = "save_" + newName;
        string filepathOld = Path.Combine(Application.persistentDataPath, filenameOld);
        string filepathNew = Path.Combine(Application.persistentDataPath, filenameNew);
        if (!File.Exists(filepathOld) || File.Exists(filenameNew))
            return false;
        string jsonContents = File.ReadAllText(filepathOld);
        File.WriteAllText(filepathNew, jsonContents);
        File.Delete(filepathOld);
        return true;
    }
}

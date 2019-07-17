using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.IO;
using UnityEngine.Serialization;

public class SaveManager {

    public static Save SaveCurrent() {
        int n = GetSaves().Count;
        Debug.Log("Save count is "+n);
        Save saveData = new Save(BuilderKitConfig.SAVE_PREFIX+n);
        string path = Application.persistentDataPath+"/"+saveData.name+BuilderKitConfig.SAVE_EXTENSION;
        string jsonData = JsonUtility.ToJson(saveData);

        File.WriteAllText(path, jsonData);
        
        #if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
        #endif
        
        return saveData;
    }

    public static List<Save> GetSaves() {
        List<Save> allSaves = new List<Save>();
        DirectoryInfo directory = new DirectoryInfo(Application.persistentDataPath);
        FileInfo[] files = directory.GetFiles();
        for(int i=0;i<files.Length;i++) {
            if(files[i].Extension.Equals(BuilderKitConfig.SAVE_EXTENSION)) {
                StreamReader file = files[i].OpenText();
                string jsonData = file.ReadToEnd();
                file.Close();
                
                Save saveData = JsonUtility.FromJson<Save>(jsonData);
                allSaves.Add(saveData);
            }
        }
        return allSaves;
    }

}
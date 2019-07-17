using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
 * This script has to be appended to the Save Dialog Button in the side menu
 */
public class SaveDialogController : MonoBehaviour {
    GameObject saveDialog;
    Transform savesContainer;
    GameObject saveItemPrefab;


    void Start() {
        saveItemPrefab = (GameObject)Resources.Load("Prefabs/Menus/SaveItem");
        saveDialog = GameObject.Find("SaveDialog");
        GameObject saveDialogOpenButton = GameObject.Find("SaveDialogOpenButton");
        
        if(saveDialog!=null) {
            Transform saveList = GameObject.Find("SaveList").transform;
            savesContainer = saveList.Find("Viewport").Find("Content");
            if(saveDialogOpenButton!=null) {
                Button button = saveDialogOpenButton.GetComponent<Button>();
                button.onClick.AddListener(() => {
                if(!saveDialog.activeSelf) {
                    BuilderController.instance.modalOpen = true;
                    RefreshSaveList();
                    saveDialog.SetActive(true);
                }
                });
            } else {
                Debug.LogWarning("Save Dialog trigger button not found!");
            }

            AddDialogListeners();
            saveDialog.SetActive(false);

        } else {
            Debug.LogWarning("Save Dialog not found!");
        }
        
    }

    private void RefreshSaveList() {
        List<Save> saves = SaveManager.GetSaves();
        savesContainer.DetachChildren();

        if(saves!=null && saves.Count>0) {
            for(int i = saves.Count-1; i>=0;i--) {
                GameObject saveItem = GetSaveItem(saves[i]);
                saveItem.transform.SetParent(savesContainer.transform,false);
            }

            RectTransform containerRect = savesContainer.GetComponent<RectTransform>();
            Vector2 size = containerRect.sizeDelta;
            size.y = saves.Count*BuilderKitConfig.SAVE_ITEM_HEIGHT;
            containerRect.sizeDelta = size;
        }
        
    }

    private GameObject GetSaveItem(Save save) {
        GameObject saveItem = GameObject.Instantiate(saveItemPrefab);
        saveItem.name = save.name;
        saveItem.transform.Find("FilenameLabel").GetComponent<Text>().text = save.name;
        string dateTime = save.date.ToShortDateString()+" at "+save.date.ToShortTimeString();
        saveItem.transform.Find("SaveDateLabel").GetComponent<Text>().text = dateTime;

        saveItem.GetComponent<Button>().onClick.AddListener(() => {
            Save thisSave = save;
            thisSave.Load();
            BuilderController.instance.modalOpen = false;
            saveDialog.SetActive(false);
        });

        return saveItem;
    }

    private void AddDialogListeners() {
        Button saveButton = GameObject.Find("SaveButton").GetComponent<Button>();
        Button cancelLoadButton = GameObject.Find("CancelLoadButton").GetComponent<Button>();

        saveButton.onClick.AddListener(() => {
            BuilderController.instance.EnablePlacing();
            SaveManager.SaveCurrent();
            BuilderController.instance.modalOpen = false;
            saveDialog.SetActive(false);
        });

        cancelLoadButton.onClick.AddListener(() => {
            BuilderController.instance.EnablePlacing();
            BuilderController.instance.modalOpen = false;
            saveDialog.SetActive(false);
        });
    }


}
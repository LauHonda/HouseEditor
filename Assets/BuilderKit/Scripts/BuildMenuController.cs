using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/*
 * This script has to be appended to the Build Menu UI panel, otherwise 
 * OnPointerEnter() and OnPointerExit() will not work
 */
public class BuildMenuController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    GameObject baseMenu;
    List<GameObject> objectPanels;
    List<string> categoryNames;

    void Start()
    {
        baseMenu = GameObject.Find("BuildMenu");
        SetVisibilityButtons();
        SetFloorButtons();
        SetCategoryMenus();
    }

    private void SetVisibilityButtons()
    {
        SetVisibilityButton("VisibilityButton1", false, false);
        SetVisibilityButton("VisibilityButton2", false, true);
        SetVisibilityButton("VisibilityButton3", true, true);
    }

    private void SetVisibilityButton(string buttonName, bool isRoofVisible, bool areWallsVisible)
    {
        GameObject visibilityButton = GameObject.Find(buttonName);
        if (visibilityButton != null)
        {
            visibilityButton.GetComponent<Button>().onClick.AddListener(delegate ()
            {
                BuilderController.instance.ToggleVisibility(isRoofVisible, areWallsVisible);
            });
        }
        else
        {
            Debug.LogError("Missing visibility button! " + buttonName);
        }
    }

    private void SetFloorButtons()
    {
        GameObject floorUpButton = GameObject.Find("FloorUp");
        if (floorUpButton != null)
        {
            floorUpButton.GetComponent<Button>().onClick.AddListener(delegate ()
            {
                BuilderController.instance.GoUp();
            });
        }
        else
        {
            Debug.LogError("Missing FloorUp button! ");
        }

        GameObject floorDownButton = GameObject.Find("FloorDown");
        if (floorDownButton != null)
        {
            floorDownButton.GetComponent<Button>().onClick.AddListener(delegate ()
            {
                BuilderController.instance.GoDown();
            });
        }
        else
        {
            Debug.LogError("Missing FloorDown button! ");
        }
    }

    private void SetCategoryMenus()
    {
        Dictionary<string, GameObject[]> objects = ObjectLoader.GetObjectsByCategory();

        GameObject categoryMenu = Resources.Load("Prefabs/Menus/CategoryPanel") as GameObject;
        GameObject objectMenuItem = Resources.Load("Prefabs/Menus/ObjectMenuItem") as GameObject;

        objectPanels = new List<GameObject>();
        categoryNames = new List<string>();

        Sprite noThumbnailImage = Resources.Load<Sprite>("ObjectThumbnails/NoThumbnail");

        foreach (KeyValuePair<string, GameObject[]> data in objects)
        {
            GameObject category = GameObject.Instantiate(categoryMenu);
            category.GetComponentInChildren<Text>().text = data.Key;
            category.name = data.Key;

            category.GetComponentInChildren<Button>().onClick.AddListener(delegate ()
            {
                string categoryName = data.Key;
                SelectCategory(categoryName);
            });

            GameObject itemsContainer = category.transform.Find("CategoryObjects").gameObject;

            foreach (GameObject item in data.Value)
            {
                GameObject itemMenu = GameObject.Instantiate(objectMenuItem);

                PlaceableObject placeableObject = item.GetComponent<PlaceableObject>();

                Sprite thumbnail = Resources.Load<Sprite>("ObjectThumbnails/" + item.name);
                if (thumbnail == null)
                {
                    Debug.Log("Warning: Missing thumbnail for " + item.name);
                    thumbnail = noThumbnailImage;
                }
                itemMenu.transform.Find("Icon").GetComponent<Image>().sprite = thumbnail;

                itemMenu.name = placeableObject.objectName;

                //itemMenu.GetComponentInChildren<Button>().onClick.AddListener(delegate ()
                //{
                //    GameObject selectedItem = item;
                //    BuilderController.instance.SelectObject(selectedItem);
                //});         
                GameObject selectedItem = item;
                itemMenu.AddComponent<PrefabSpawnPoint>().SetPrefab(selectedItem);

                itemMenu.transform.SetParent(itemsContainer.transform, false);
            }
            objectPanels.Add(itemsContainer);
            categoryNames.Add(category.name);
            category.transform.SetParent(baseMenu.transform, false);
        }
        //Collapse all the menus
        SelectCategory(null);
    }

    private void SelectCategory(string selectedCategory)
    {
        for (int i = 0; i < categoryNames.Count; i++)
        {
            objectPanels[i].SetActive(categoryNames[i].Equals(selectedCategory));
        }
    }

    //Disables the placing of objects when the mouse is over the menu
    public void OnPointerEnter(PointerEventData eventData)
    {
        BuilderController.instance.placingEnabled = false;
        BuilderController.instance.HideSelectedObject();
        Cursor.visible = true;
    }

    //Reenables normal behaviour when the mouse leaves the menu area
    public void OnPointerExit(PointerEventData eventData)
    {
        BuilderController.instance.EnablePlacing();
    }

}

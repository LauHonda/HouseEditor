using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PrefabSpawnPoint : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    BuilderController builderController;
    public bool CanDrag = false;

    private HashSet<Transform> m_prefabInstanceTransforms;
    private Plane m_dragPlane;

    private GameObject m_prefab;
    
    protected virtual void Start()
    {
        
    }

    public void SetPrefab(GameObject prefab)
    {
       
        builderController = BuilderController.instance;
        if (prefab == null)
        {
            Debug.LogError("m_prefab is not set");
            return;
        }

        m_prefab = prefab;              
    }


    public void OnStart()
    {
       

    }

    protected virtual void OnDestroy()
    {

    }

    public virtual void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        BuilderController.instance.SelectObject(m_prefab);
        BuilderController.instance.OnDragUpdate = true;
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
        BuilderController.instance.selectedObjMove();
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {

    }
}




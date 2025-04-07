using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class point : MonoBehaviour
{
    private GraphicRaycaster _graphicRaycaster;
    private EventSystem _eventSystem;
    // Start is called before the first frame update
    void Start()
    {
        _graphicRaycaster = GetComponent<GraphicRaycaster>();
        _eventSystem = EventSystem.current;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Or any other input
            {
                RaycastUIElement();
            }
    }

    void RaycastUIElement()
    {
        // Create a new PointerEventData
        PointerEventData pointerEventData = new PointerEventData(_eventSystem);

        // Set the position of the event data to the mouse position
        pointerEventData.position = Input.mousePosition;

        // Create a list to store the results
        List<RaycastResult> results = new List<RaycastResult>();

        // Perform the raycast
        _graphicRaycaster.Raycast(pointerEventData, results);

        // Check if any UI element was hit
        if (results.Count > 0)
        {
            // The first result is the UI element that was hit
            GameObject hitObject = results[0].gameObject;
            Debug.Log("Raycast hit: " + hitObject.name);
        }
        else
        {
            Debug.Log("No UI element hit");
        }
    }
}


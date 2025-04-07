using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Tooltip : MonoBehaviour
{
    [SerializeField]
    Camera cam;
    [SerializeField]
    TMPro.TextMeshProUGUI text;

    public void Show(string text)
    {
        this.text.text = text;
        this.gameObject.SetActive(true);
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }

    private void Update()
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(transform.parent.GetComponent<RectTransform>(), Input.mousePosition, cam, out localPoint);
        transform.localPosition = localPoint;
    }
}

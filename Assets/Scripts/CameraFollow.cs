using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(PixelPerfectCamera))]
public class CameraFollowScript : MonoBehaviour
{
    Camera cam;
    PixelPerfectCamera pixelCam;

    [SerializeField]
    Transform target;

    [SerializeField]
    RectInt trackingPixelBounds;

    private void Start()
    {
        cam = GetComponent<Camera>();
        pixelCam = GetComponent<PixelPerfectCamera>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 screenPoint = cam.WorldToScreenPoint(target.position) / pixelCam.pixelRatio;
        int minX = (cam.pixelWidth / pixelCam.pixelRatio - trackingPixelBounds.width) / 2;
        int maxX = minX + trackingPixelBounds.width;
        int minY = (cam.pixelHeight / pixelCam.pixelRatio - trackingPixelBounds.height) / 2;
        int maxY = minY + trackingPixelBounds.height;
        Vector3 clampedScreenPoint = new Vector3(Mathf.Clamp(screenPoint.x, minX, maxX), Mathf.Clamp(screenPoint.y, minY, maxY), screenPoint.z);
        Vector3 delta = new Vector3(Mathf.RoundToInt(screenPoint.x - clampedScreenPoint.x), Mathf.RoundToInt(screenPoint.y - clampedScreenPoint.y), 0f);
        cam.transform.Translate(delta);
    }
}

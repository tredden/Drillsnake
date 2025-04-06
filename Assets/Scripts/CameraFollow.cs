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
        FixMapBounds();
    }

    void FixMapBounds()
    {
        MapGenerator map = MapGenerator.GetInstance();
        if (map == null) {
            return;
        }
        Vector3 minCam = cam.ScreenToWorldPoint(Vector3.zero);
        Vector3 maxCam = cam.ScreenToWorldPoint(new Vector3(cam.pixelWidth, cam.pixelHeight, 0f));
        map.UpdateCameraBounds(Mathf.Min(minCam.x, maxCam.x), Mathf.Max(minCam.x, maxCam.x), Mathf.Min(minCam.y, maxCam.y), Mathf.Max(minCam.y, maxCam.y));
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 screenPoint = cam.WorldToScreenPoint(target.position) / pixelCam.pixelRatio;
        int minX = trackingPixelBounds.x;
        int maxX = minX + trackingPixelBounds.width;
        int minY = trackingPixelBounds.y;
        int maxY = minY + trackingPixelBounds.height;
        Vector3 clampedScreenPoint = new Vector3(Mathf.Clamp(screenPoint.x, minX, maxX), Mathf.Clamp(screenPoint.y, minY, maxY), screenPoint.z);
        Vector3 delta = new Vector3(Mathf.RoundToInt(screenPoint.x - clampedScreenPoint.x), Mathf.RoundToInt(screenPoint.y - clampedScreenPoint.y), 0f);
        if (delta.magnitude > 0f) {
            cam.transform.Translate(delta);
        }
        FixMapBounds();
    }
}

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
    [SerializeField]
    bool floatWindow;
    [SerializeField]
    float windowFloatSpeed = 60f;

    float flminX;
    float flmaxX;
    float flminY;
    float flmaxY;

    float flx;
    float fly;


    private void Start()
    {
        cam = GetComponent<Camera>();
        pixelCam = GetComponent<PixelPerfectCamera>();
        FixMapBounds();

        flx = trackingPixelBounds.x;
        fly = trackingPixelBounds.y;

        float screenW = cam.pixelWidth / pixelCam.pixelRatio;
        flminX = Mathf.Min(trackingPixelBounds.xMin, screenW - trackingPixelBounds.xMax);
        flmaxX = screenW - flminX - trackingPixelBounds.width;

        float screenH = cam.pixelHeight / pixelCam.pixelRatio;
        flminY = Mathf.Min(trackingPixelBounds.yMin, screenH - trackingPixelBounds.yMax);
        flmaxY = screenH - flminY - trackingPixelBounds.height;
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

        if (floatWindow) {
            float dt = Time.deltaTime;
            // our specific sprite is down-forward vel for some reason
            float vx = -target.up.x * dt * windowFloatSpeed * -1f;
            float vy = -target.up.y * dt * windowFloatSpeed * -1f;
            flx = Mathf.Clamp(flx + vx, flminX, flmaxX);
            fly = Mathf.Clamp(fly + vy, flminY, flmaxY);
            trackingPixelBounds.x = Mathf.RoundToInt(flx);
            trackingPixelBounds.y = Mathf.RoundToInt(fly);
        }
    }
}

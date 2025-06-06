using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AspectRatio : MonoBehaviour
{
    void Start()
{
    float targetAspect = 1080f / 2160f; // Your target portrait aspect ratio
    float windowAspect = (float)Screen.width / (float)Screen.height;
    float scaleHeight = windowAspect / targetAspect;

    Camera camera = Camera.main;

    if (scaleHeight < 1.0f)
    {
        Rect rect = camera.rect;

        rect.width = 1.0f;
        rect.height = scaleHeight;
        rect.x = 0;
        rect.y = (1.0f - scaleHeight) / 2.0f;

        camera.rect = rect;
    }
    else // add letterboxing left/right
    {
        float scaleWidth = 1.0f / scaleHeight;

        Rect rect = camera.rect;

        rect.width = scaleWidth;
        rect.height = 1.0f;
        rect.x = (1.0f - scaleWidth) / 2.0f;
        rect.y = 0;

        camera.rect = rect;
    }
}
}

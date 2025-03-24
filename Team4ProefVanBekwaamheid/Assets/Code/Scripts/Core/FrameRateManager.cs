using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class FrameRateManager : MonoBehaviour
{
    int maxRate = 9999;
    [SerializeField] private int targetFrameRate = 60;
    float currentFrameTime;

    void Start()
    {
        QualitySettings.vSyncCount = 0; // Disable VSync to allow frame rate to go above target
        Application.targetFrameRate = maxRate; // Set target frame rate to max
        currentFrameTime = Time.realtimeSinceStartup; // Initialize current frame time
        StartCoroutine(WaitForNextFrame());

        IEnumerator WaitForNextFrame()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame(); // Wait for the end of the frame
                currentFrameTime += 1.0f / targetFrameRate;
                var t = Time.realtimeSinceStartup;
                var sleepTime = currentFrameTime - t - 0.01f;
                if(sleepTime > 0)
                {
                    Thread.Sleep((int)(sleepTime * 1000)); // Sleep for the calculated time
                }
                while(t < currentFrameTime)
                {
                    t = Time.realtimeSinceStartup; // Update the current time
                }
            }
        }
    }
}

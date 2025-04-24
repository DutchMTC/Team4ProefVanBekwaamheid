using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class FrameRateManager : MonoBehaviour
{

    [SerializeField] private int _targetFrameRate = 60;
    private float _currentFrameTime;
    private int _maxRate = 9999;

    void Start()
    {
        QualitySettings.vSyncCount = 0; // Disable VSync to allow frame rate to go above target
        Application.targetFrameRate = _maxRate; // Set target frame rate to max
        _currentFrameTime = Time.realtimeSinceStartup; // Initialize current frame time
        StartCoroutine(WaitForNextFrame());

        IEnumerator WaitForNextFrame()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame(); // Wait for the end of the frame
                _currentFrameTime += 1.0f / _targetFrameRate;
                var t = Time.realtimeSinceStartup;
                var sleepTime = _currentFrameTime - t - 0.01f;
                if(sleepTime > 0)
                {
                    Thread.Sleep((int)(sleepTime * 1000)); // Sleep for the calculated time
                }
                while(t < _currentFrameTime)
                {
                    t = Time.realtimeSinceStartup; // Update the current time
                }
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events; // Added for UnityEvent

public class Timer : MonoBehaviour
{
    [Tooltip("The GameObject whose transform will be rotated.")]
    public Transform objectToRotate;

    [Tooltip("The total time in seconds for one full rotation.")]
    public float seconds = 10f;

    public UnityEvent onTimerEnd;

    private Coroutine timerCoroutine;
    private const float DEGREES_IN_CIRCLE = 360f;

    void Start()
    {
        timerCoroutine = StartCoroutine(RotateOverTime());
    }
    public void StartTimer()
    {
        if (objectToRotate == null)
        {
            Debug.LogError("Timer: No object assigned to rotate.", this);
            return;
        }

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }

        objectToRotate.localEulerAngles = Vector3.zero;
        timerCoroutine = StartCoroutine(RotateOverTime());
    }

    private IEnumerator RotateOverTime()
    {
        float elapsedTime = 0f;

        while (elapsedTime < seconds)
        {
            // Prevent division by zero or negative time during the coroutine
            if (seconds <= 0)
            {
                yield break;
            }
             // Check again in case it becomes null during rotation
             if (objectToRotate == null)
            {
                Debug.LogError("Timer: Object to rotate became null during rotation.", this);
                yield break;
            }


            elapsedTime += Time.deltaTime;

            float timeProportion = elapsedTime / seconds;

            float rotationAngle = timeProportion * DEGREES_IN_CIRCLE;

            // Positive rotation for counter-clockwise movement
            objectToRotate.localEulerAngles = new Vector3(0, 0, rotationAngle);

            yield return null;
        }

        // Ensure the rotation ends exactly at 360 degrees (or 0)
        if (objectToRotate != null)
        {
            objectToRotate.localEulerAngles = new Vector3(0, 0, DEGREES_IN_CIRCLE);
        }


        timerCoroutine = null;
        onTimerEnd?.Invoke();
    }

    public void StopTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events; // Added for UnityEvent

public class Timer : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("The GameObject whose transform will be rotated.")]
    public Transform objectToRotate;

    [Tooltip("The total time in seconds for one full rotation.")]
    public float seconds = 10f;

    public UnityEvent onTimerEnd;

    [Header("Shake Settings")]
    [Tooltip("The GameObject to apply the shake effect to.")]
    [SerializeField] private GameObject objectToShake;
    [Tooltip("Curve controlling shake intensity over the timer's duration (Time 0 to 1).")]
    [SerializeField] private AnimationCurve shakeIntensityCurve = AnimationCurve.Linear(0, 0.1f, 1, 0.1f); // Default curve
    [Tooltip("How long each individual shake lasts in seconds.")]
    [SerializeField] private float shakeDuration = 0.15f;

    private Coroutine timerCoroutine;
    private Coroutine shakeCoroutine;
    private const float DEGREES_IN_CIRCLE = 360f;
    private float currentElapsedTime = 0f;
    private float timeSinceLastShake = 0f;
    private Vector3 shakeOriginalPosition;
    private bool isTimerRunning = false; 

    void Update()
    {
        // Check if the main timer is running and we have an object to shake
        if (isTimerRunning && objectToShake != null)
        {
            timeSinceLastShake += Time.deltaTime;

            // Trigger shake every second
            if (timeSinceLastShake >= 1.0f)
            {
                timeSinceLastShake -= 1.0f; // Subtract to maintain accuracy

                // Stop previous shake if it's still running
                if (shakeCoroutine != null)
                {
                    StopCoroutine(shakeCoroutine);
                    // Ensure position is reset before starting new shake
                    objectToShake.transform.position = shakeOriginalPosition;
                }
                shakeCoroutine = StartCoroutine(Shake());
            }
        }
        // Optional: Reset position if timer stopped and object is displaced
        else if (!isTimerRunning && objectToShake != null && shakeCoroutine == null) // Check shakeCoroutine too
        {
             if (objectToShake.transform.position != shakeOriginalPosition)
             {
                 objectToShake.transform.position = shakeOriginalPosition;
             }
        }
    }


    public void StartTimer()
    {
        if (objectToRotate == null)
        {
            Debug.LogError("Timer: No object assigned to rotate.", this);
            return;
        }

        StopTimer(); // Stop existing timers and shakes before starting anew

        // Initialize rotation
        objectToRotate.localEulerAngles = Vector3.zero;
        currentElapsedTime = 0f; // Reset elapsed time for rotation and curve
        timerCoroutine = StartCoroutine(RotateOverTime());

        // Initialize shake
        if (objectToShake != null)
        {
            shakeOriginalPosition = objectToShake.transform.position;
            timeSinceLastShake = 0f; // Reset shake timer
        }
        isTimerRunning = true; // Set flag
    }

    private IEnumerator RotateOverTime()
    {
        // elapsedTime is now a member variable: currentElapsedTime
        currentElapsedTime = 0f; // Ensure it starts at 0

        while (currentElapsedTime < seconds)
        {
            // Prevent division by zero or negative time during the coroutine
            if (seconds <= 0)
            {
                 isTimerRunning = false; // Stop timer logic
                 timerCoroutine = null;
                 yield break;
            }
             // Check again in case it becomes null during rotation
             if (objectToRotate == null)
            {
                Debug.LogError("Timer: Object to rotate became null during rotation.", this);
                isTimerRunning = false; // Stop timer logic
                timerCoroutine = null;
                yield break;
            }


            currentElapsedTime += Time.deltaTime;

            // Clamp elapsed time to prevent overshooting due to frame time
            currentElapsedTime = Mathf.Min(currentElapsedTime, seconds);

            float timeProportion = currentElapsedTime / seconds;

            float rotationAngle = timeProportion * DEGREES_IN_CIRCLE;

            // Positive rotation for counter-clockwise movement
            objectToRotate.localEulerAngles = new Vector3(0, 0, rotationAngle);

            yield return null;
        }

        // Ensure the rotation ends exactly at 360 degrees (or 0)
        if (objectToRotate != null)
        {
            objectToRotate.localEulerAngles = new Vector3(0, 0, 0); // End at 0 (same as 360)
        }


        timerCoroutine = null;
        isTimerRunning = false; // Clear flag
        onTimerEnd?.Invoke();
    }

    private IEnumerator Shake()
    {
        float elapsedShakeTime = 0.0f;

        // Calculate intensity based on the main timer's progress
        float curveTime = Mathf.Clamp01(currentElapsedTime / seconds);
        float currentIntensity = shakeIntensityCurve.Evaluate(curveTime);

        while (elapsedShakeTime < shakeDuration)
        {
            if (objectToShake == null) yield break; // Stop if object becomes null

            float x = Random.Range(-1f, 1f) * currentIntensity;
            float y = Random.Range(-1f, 1f) * currentIntensity;
            // Assuming 2D shake on XY plane. Modify if Z is needed.
            objectToShake.transform.position = shakeOriginalPosition + new Vector3(x, y, 0);

            elapsedShakeTime += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // Reset position precisely after shaking
        if (objectToShake != null)
        {
            objectToShake.transform.position = shakeOriginalPosition;
        }
        shakeCoroutine = null; // Mark coroutine as finished
    }


    public void StopTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
            // Reset position immediately when timer is stopped externally
            if (objectToShake != null)
            {
                 objectToShake.transform.position = shakeOriginalPosition;
            }
        }
        isTimerRunning = false; // Clear flag
    }
}

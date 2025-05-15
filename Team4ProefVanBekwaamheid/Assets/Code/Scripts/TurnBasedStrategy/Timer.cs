using UnityEngine;
using UnityEngine.Events;

public class Timer : MonoBehaviour
{
    public UnityEvent onTimerEnd;

    // This method can be linked to a UI Button's OnClick event in the Unity Editor
    public void EndPlayerTurn()
    {
        Debug.Log("Player turn ended by button press.");
        onTimerEnd?.Invoke();
    }
}

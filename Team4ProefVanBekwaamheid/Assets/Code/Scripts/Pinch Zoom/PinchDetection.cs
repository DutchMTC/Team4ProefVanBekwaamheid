using UnityEngine;
using UnityEngine.InputSystem;

public class PinchDetection : MonoBehaviour
{
    [SerializeField] private float speed = 0.01f;
    private int touchCount = 0;
    private float prevMagnitude;
    private CameraMovement cameraMovement;

    void Start()
    {
        cameraMovement = GetComponent<CameraMovement>();

        // mouse scroll
        var scrollAction = new InputAction(binding: "<Mouse>/scroll");
        scrollAction.Enable();
        scrollAction.performed += ctx => cameraMovement.CameraZoom(ctx.ReadValue<Vector2>().y * speed);

        // check if the screen is being touched
        var firstTouchContact = new InputAction(type: InputActionType.Button, binding: "<Touchscreen>/touch0/press");
        firstTouchContact.Enable();

        var secondTouchContact = new InputAction(type: InputActionType.Button, binding: "<Touchscreen>/touch0/press");
        secondTouchContact.Enable();

        firstTouchContact.performed += _ => touchCount++;
        secondTouchContact.performed += _ => touchCount++;

        firstTouchContact.canceled += _ =>
        {
            touchCount--;
            prevMagnitude = 0;
        };

          secondTouchContact.canceled += _ =>
        {
            touchCount--;
            prevMagnitude = 0;
        };

        // calculates the difference between the  positions where the screen is being touched 
        var firstTouchPos = new InputAction(type: InputActionType.Value, binding: "<Touchscreen>/touch0/position");
        firstTouchPos.Enable();

        var secondTouchPos = new InputAction(type: InputActionType.Value, binding: "<Touchscreen>/touch0/position");
        secondTouchPos.Enable();
        secondTouchPos.performed += _ =>
        {
            if(touchCount < 2 )
                return;

            var magnitude = (firstTouchPos.ReadValue<Vector2>() - secondTouchPos.ReadValue<Vector2>()).magnitude;

            if(prevMagnitude == 0)
                prevMagnitude = magnitude;
            
            var difference = magnitude - prevMagnitude;
            prevMagnitude = magnitude;
            cameraMovement.CameraZoom(-difference * speed);
        };
    } 
}
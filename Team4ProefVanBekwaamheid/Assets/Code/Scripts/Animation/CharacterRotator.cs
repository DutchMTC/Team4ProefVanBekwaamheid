using UnityEngine;
using System.Collections;

public class CharacterRotator : MonoBehaviour
{
    [SerializeField, Tooltip("Duration of the rotation towards the next tile")]
    private float _rotationDuration = 0.3f;
    [SerializeField, Tooltip("Curve defining the rotation animation")]
    private AnimationCurve _rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Default to an ease-in-out curve
    [SerializeField, Tooltip("Rotation offset for the player character (Euler angles)")]
    private Vector3 _playerRotationOffset = Vector3.zero;
    [SerializeField, Tooltip("Rotation offset for the enemy character (Euler angles)")]
    private Vector3 _enemyRotationOffset = Vector3.zero;
    [SerializeField, Tooltip("Optional: Specific transform for the enemy model to rotate (if different from the main enemy object)")]
    private Transform _enemyModelToRotate;

    public enum UserType // Copied from TileSelection for now, might need a more central place
    {
        Player,
        Enemy
    }

    // Removed 'offset' parameter, as it will now use the internal fields
    public IEnumerator RotateTowardsAsync(Transform entityTransform, Vector3 direction, UserType currentUserType, float duration)
    {
        Transform transformToRotate = entityTransform;
        Vector3 currentOffset = (currentUserType == UserType.Player) ? _playerRotationOffset : _enemyRotationOffset;

        if (currentUserType == UserType.Enemy && _enemyModelToRotate != null)
        {
            transformToRotate = _enemyModelToRotate;
        }

        Quaternion startRotation = transformToRotate.rotation;
        Quaternion targetLookRotation = Quaternion.LookRotation(direction);
        // Apply the internal offset to the target rotation
        Quaternion targetRotation = targetLookRotation * Quaternion.Euler(currentOffset);
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float curveValue = _rotationCurve.Evaluate(elapsedTime / duration);
            transformToRotate.rotation = Quaternion.Slerp(startRotation, targetRotation, curveValue);
            yield return null;
        }
        transformToRotate.rotation = targetRotation; // Ensure it ends exactly at the target rotation
    }

    public IEnumerator RotateTowardsTargetAsync(Transform entityTransform, Transform targetTransform, UserType currentUserType)
    {
        Vector3 directionToTarget = targetTransform.position - entityTransform.position;
        directionToTarget.y = 0; // Keep rotation on the Y axis (horizontal plane)

        if (directionToTarget != Vector3.zero)
        {
            Vector3 currentOffset = (currentUserType == UserType.Player) ? _playerRotationOffset : _enemyRotationOffset;
            Transform transformToRotate = entityTransform; 

            if (currentUserType == UserType.Enemy && _enemyModelToRotate != null)
            {
                transformToRotate = _enemyModelToRotate;
            }
            // Call the modified RotateTowardsAsync without the explicit offset
            yield return StartCoroutine(RotateTowardsAsync(transformToRotate, directionToTarget, currentUserType, _rotationDuration));
        }
    }
}
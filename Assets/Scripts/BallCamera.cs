using UnityEngine;

public class BallCamera : MonoBehaviour
{
    [Header("Camera Reference")]
    public Transform cameraTransform;

    [Header("Aim Camera Before Shot")]
    public Vector3 aimCameraOffset = new Vector3(0f, 1f, -5f);
    public float aimLookHeight = 2.2f;

    [Header("Dynamic Follow Camera After Shot")]
    public float followDistance = 8f;
    public float followSideOffset = 0f;

    [Header("Low Ball Camera")]
    public float lowBallCameraHeight = 5f;
    public float lowBallLookHeight = 0.5f;

    [Header("High Ball Camera")]
    public float highBallCameraHeight = -1f;
    public float highBallLookHeight = 2.2f;

    [Header("Ball Height Detection")]
    public float maxBallHeightForChange = 5f;

    [Header("Smooth Settings")]
    public float aimCameraSmoothSpeed = 5f;
    public float followPositionSmoothTime = 0.18f;
    public float followRotationSmoothSpeed = 6f;

    private BallMove ballMove;
    private Vector3 followCameraVelocity = Vector3.zero;

    void Start()
    {
        ballMove = GetComponent<BallMove>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null)
        {
            return;
        }

        if (ballMove == null)
        {
            return;
        }

        if (!ballMove.IsMoving)
        {
            UpdateAimCamera();
        }
        else
        {
            UpdateDynamicFollowCamera();
        }
    }

    void UpdateAimCamera()
    {
        Vector3 targetPosition = transform.position
            + transform.right * aimCameraOffset.x
            + Vector3.up * aimCameraOffset.y
            + transform.forward * aimCameraOffset.z;

        cameraTransform.position = Vector3.Lerp(
            cameraTransform.position,
            targetPosition,
            aimCameraSmoothSpeed * Time.deltaTime
        );

        Vector3 lookTarget = transform.position + Vector3.up * aimLookHeight;

        Quaternion targetRotation = Quaternion.LookRotation(
            lookTarget - cameraTransform.position
        );

        cameraTransform.rotation = Quaternion.Slerp(
            cameraTransform.rotation,
            targetRotation,
            aimCameraSmoothSpeed * Time.deltaTime
        );
    }

    void UpdateDynamicFollowCamera()
    {
        Vector3 horizontalVelocity = new Vector3(
            ballMove.Velocity.x,
            0f,
            ballMove.Velocity.z
        );

        Vector3 followDirection;

        if (horizontalVelocity.magnitude > 0.1f)
        {
            followDirection = horizontalVelocity.normalized;
        }
        else
        {
            followDirection = transform.forward;
        }

        Vector3 sideDirection = Vector3.Cross(Vector3.up, followDirection).normalized;

        float groundY = 0f;

        if (ballMove.TryGetGroundYAtPosition(transform.position, out float detectedGroundY))
        {
            groundY = detectedGroundY;
        }
        else
        {
            groundY = transform.position.y;
        }

        float ballHeightFromGround = transform.position.y - groundY;

        float heightRate = Mathf.InverseLerp(
            0f,
            maxBallHeightForChange,
            ballHeightFromGround
        );

        float dynamicCameraHeight = Mathf.Lerp(
            lowBallCameraHeight,
            highBallCameraHeight,
            heightRate
        );

        float dynamicLookHeight = Mathf.Lerp(
            lowBallLookHeight,
            highBallLookHeight,
            heightRate
        );

        Vector3 targetPosition = transform.position
            - followDirection * followDistance
            + sideDirection * followSideOffset
            + Vector3.up * dynamicCameraHeight;

        cameraTransform.position = Vector3.SmoothDamp(
            cameraTransform.position,
            targetPosition,
            ref followCameraVelocity,
            followPositionSmoothTime
        );

        Vector3 lookTarget = transform.position + Vector3.up * dynamicLookHeight;

        Quaternion targetRotation = Quaternion.LookRotation(
            lookTarget - cameraTransform.position
        );

        cameraTransform.rotation = Quaternion.Slerp(
            cameraTransform.rotation,
            targetRotation,
            followRotationSmoothSpeed * Time.deltaTime
        );
    }
}
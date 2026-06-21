using UnityEngine;

public class BallOrbit : MonoBehaviour
{
    [Header("Trajectory Prediction")]
    public LineRenderer trajectoryLine;
    public int trajectoryPointCount = 30;
    public float trajectoryTimeStep = 0.1f;

    private BallShot ballShot;
    private BallMove ballMove;

    void Start()
    {
        ballShot = GetComponent<BallShot>();
        ballMove = GetComponent<BallMove>();

        if (trajectoryLine == null)
        {
            trajectoryLine = GetComponent<LineRenderer>();
        }

        if (trajectoryLine != null)
        {
            trajectoryLine.useWorldSpace = true;
            trajectoryLine.positionCount = 0;
        }
    }

    void Update()
    {
        if (ballShot == null || ballMove == null || trajectoryLine == null)
        {
            return;
        }

        if (!ballShot.CanShowOrbit())
        {
            trajectoryLine.positionCount = 0;
            return;
        }

        UpdateTrajectoryLine();
    }

    void UpdateTrajectoryLine()
    {
        trajectoryLine.positionCount = trajectoryPointCount;

        Vector3 predictPosition = transform.position;

        float previewPower = ballShot.GetPreviewPower();
        float previewAngle = ballShot.GetAngle();

        float radianAngle = previewAngle * Mathf.Deg2Rad;

        Vector3 forwardVelocity = transform.forward * Mathf.Cos(radianAngle) * previewPower;
        Vector3 upwardVelocity = Vector3.up * Mathf.Sin(radianAngle) * previewPower;

        Vector3 predictVelocity = forwardVelocity + upwardVelocity;

        for (int i = 0; i < trajectoryPointCount; i++)
        {
            trajectoryLine.SetPosition(i, predictPosition);

            predictVelocity += ballMove.gravity * trajectoryTimeStep;

            if (ballMove.windDirection != Vector3.zero && ballMove.windStrength > 0f)
            {
                predictVelocity += ballMove.windDirection.normalized * ballMove.windStrength * trajectoryTimeStep;
            }

            predictPosition += predictVelocity * trajectoryTimeStep;

            if (ballMove.TryGetGroundYAtPosition(predictPosition, out float groundY))
            {
                if (predictPosition.y < groundY)
                {
                    predictPosition.y = groundY;
                }
            }
        }
    }
}
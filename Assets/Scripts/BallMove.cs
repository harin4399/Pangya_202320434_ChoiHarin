using UnityEngine;

public class BallMove : MonoBehaviour
{
    [Header("Physics Settings")]
    public Vector3 gravity = new Vector3(0, -9.8f, 0);
    public float ballRadius = 0.5f;
    public float stopSpeed = 0.2f;

    [Header("Ground Detection")]
    public LayerMask groundLayerMask = ~0;
    public float groundRayStartHeight = 50f;
    public float groundRayDistance = 100f;
    public float groundCheckOffset = 0.02f;

    [Header("Bounce Settings")]
    public float restitution = 0.45f;
    public float minBounceSpeed = 1.0f;

    [Header("Wind Settings")]
    public Vector3 windDirection = Vector3.zero;
    public float windStrength = 0f;

    [Header("Surface Friction Settings")]
    public float fairwayFriction = 2.0f;
    public float roughFriction = 4.0f;
    public float defaultFriction = 1.2f;

    [Header("Visual Rotation")]
    public Transform ballVisual;
    public float groundRollMultiplier = 0.3f;
    public float airSpinMultiplier = 0.08f;
    public float rollSmoothSpeed = 3f;

    public Vector3 Velocity { get; private set; }
    public bool IsMoving { get; private set; }

    private string currentSurfaceName = "";
    private float currentRollSpeed = 0f;

    void Start()
    {
        SnapBallToGround();
    }

    void Update()
    {
        if (IsMoving)
        {
            MoveBall();
        }
    }

    public void Launch(Vector3 startVelocity)
    {
        Velocity = startVelocity;
        IsMoving = true;
        currentSurfaceName = "";
        currentRollSpeed = 0f;
    }

    public void StopBall()
    {
        Velocity = Vector3.zero;
        IsMoving = false;
        currentRollSpeed = 0f;
    }

    void MoveBall()
    {
        bool hasGround = TryGetGroundYAtPosition(transform.position, out float currentGroundY);

        bool isAirborne =
            !hasGround ||
            transform.position.y > currentGroundY + groundCheckOffset ||
            Velocity.y > 0f;

        if (isAirborne)
        {
            Velocity += gravity * Time.deltaTime;

            if (windDirection != Vector3.zero && windStrength > 0f)
            {
                Velocity += windDirection.normalized * windStrength * Time.deltaTime;
            }
        }

        Vector3 nextPosition = transform.position + Velocity * Time.deltaTime;

        if (TryGetGroundYAtPosition(nextPosition, out float nextGroundY))
        {
            if (nextPosition.y <= nextGroundY && Velocity.y <= 0f)
            {
                nextPosition.y = nextGroundY;
                transform.position = nextPosition;

                float fallingSpeed = Mathf.Abs(Velocity.y);

                ApplyGroundFriction();

                if (fallingSpeed > minBounceSpeed)
                {
                    Velocity = new Vector3(
                        Velocity.x,
                        fallingSpeed * restitution,
                        Velocity.z
                    );

                    Debug.Log("바운스 발생 / 반발계수: " + restitution);
                }
                else
                {
                    Velocity = new Vector3(Velocity.x, 0f, Velocity.z);

                    Vector3 horizontalVelocity = new Vector3(Velocity.x, 0f, Velocity.z);

                    if (horizontalVelocity.magnitude <= stopSpeed)
                    {
                        StopBall();
                        Debug.Log("공 정지");
                    }
                }
            }
            else if (!isAirborne && Velocity.y <= 0f)
            {
                nextPosition.y = nextGroundY;
                transform.position = nextPosition;

                Velocity = new Vector3(Velocity.x, 0f, Velocity.z);

                ApplyGroundFriction();

                Vector3 horizontalVelocity = new Vector3(Velocity.x, 0f, Velocity.z);

                if (horizontalVelocity.magnitude <= stopSpeed)
                {
                    StopBall();
                    Debug.Log("공 정지");
                }
            }
            else
            {
                transform.position = nextPosition;
            }
        }
        else
        {
            transform.position = nextPosition;
        }

        RotateBallVisual(isAirborne);
    }

    void RotateBallVisual(bool isAirborne)
    {
        if (ballVisual == null)
        {
            return;
        }

        Vector3 horizontalVelocity = new Vector3(Velocity.x, 0f, Velocity.z);

        if (horizontalVelocity.magnitude <= 0.01f)
        {
            return;
        }

        float targetRollSpeed = horizontalVelocity.magnitude;

        currentRollSpeed = Mathf.Lerp(
            currentRollSpeed,
            targetRollSpeed,
            rollSmoothSpeed * Time.deltaTime
        );

        float moveDistance = currentRollSpeed * Time.deltaTime;

        float rotationAngle = (moveDistance / Mathf.Max(ballRadius, 0.01f)) * Mathf.Rad2Deg;

        if (isAirborne)
        {
            rotationAngle *= airSpinMultiplier;
        }
        else
        {
            rotationAngle *= groundRollMultiplier;
        }

        Vector3 rotationAxis = Vector3.Cross(Vector3.up, horizontalVelocity.normalized);

        ballVisual.Rotate(rotationAxis, rotationAngle, Space.World);
    }

    public bool TryGetGroundYAtPosition(Vector3 position, out float groundY)
    {
        groundY = ballRadius;

        Vector3 rayOrigin = new Vector3(
            position.x,
            position.y + groundRayStartHeight,
            position.z
        );

        Ray ray = new Ray(rayOrigin, Vector3.down);

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            groundRayStartHeight + groundRayDistance,
            groundLayerMask,
            QueryTriggerInteraction.Ignore
        );

        if (hits.Length == 0)
        {
            return false;
        }

        RaycastHit closestHit = new RaycastHit();
        bool foundGround = false;
        float closestDistance = Mathf.Infinity;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
            {
                continue;
            }

            if (hit.collider.gameObject == gameObject)
            {
                continue;
            }

            if (hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestHit = hit;
                foundGround = true;
            }
        }

        if (!foundGround)
        {
            return false;
        }

        groundY = closestHit.point.y + ballRadius;
        return true;
    }

    public void SnapBallToGround()
    {
        if (TryGetGroundYAtPosition(transform.position, out float groundY))
        {
            Vector3 pos = transform.position;
            pos.y = groundY;
            transform.position = pos;
        }
    }

    void ApplyGroundFriction()
    {
        float friction = GetCurrentSurfaceFriction();

        Vector3 horizontalVelocity = new Vector3(Velocity.x, 0f, Velocity.z);

        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            Vector3.zero,
            friction * Time.deltaTime
        );

        Velocity = new Vector3(
            horizontalVelocity.x,
            Velocity.y,
            horizontalVelocity.z
        );
    }

    float GetCurrentSurfaceFriction()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, Vector3.down);

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            5f,
            groundLayerMask,
            QueryTriggerInteraction.Ignore
        );

        if (hits.Length == 0)
        {
            ShowSurfaceLogOnlyWhenChanged("None", defaultFriction);
            return defaultFriction;
        }

        RaycastHit closestHit = new RaycastHit();
        bool foundSurface = false;
        float closestDistance = Mathf.Infinity;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
            {
                continue;
            }

            if (hit.collider.gameObject == gameObject)
            {
                continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestHit = hit;
                foundSurface = true;
            }
        }

        if (!foundSurface)
        {
            ShowSurfaceLogOnlyWhenChanged("Self Only", defaultFriction);
            return defaultFriction;
        }

        if (ColliderOrParentHasTag(closestHit.collider, "Fairway"))
        {
            ShowSurfaceLogOnlyWhenChanged("Fairway", fairwayFriction);
            return fairwayFriction;
        }

        if (ColliderOrParentHasTag(closestHit.collider, "Rough"))
        {
            ShowSurfaceLogOnlyWhenChanged("Rough", roughFriction);
            return roughFriction;
        }

        ShowSurfaceLogOnlyWhenChanged("Default", defaultFriction);
        return defaultFriction;
    }

    bool ColliderOrParentHasTag(Collider targetCollider, string tagName)
    {
        if (targetCollider.CompareTag(tagName))
        {
            return true;
        }

        Transform current = targetCollider.transform.parent;

        while (current != null)
        {
            if (current.CompareTag(tagName))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    void ShowSurfaceLogOnlyWhenChanged(string surfaceName, float friction)
    {
        if (currentSurfaceName != surfaceName)
        {
            currentSurfaceName = surfaceName;
            Debug.Log("현재 표면: " + surfaceName + " / 마찰: " + friction);
        }
    }
}
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class BallShot : MonoBehaviour
{
    [Header("Shot Settings")]
    public float power = 15f;
    public float angle = 35f;

    public float minPower = 5f;
    public float maxPower = 30f;

    public float minAngle = 10f;
    public float maxAngle = 70f;

    [Header("Wind Settings")]
    public float minWindStrength = 0f;
    public float maxWindStrength = 10f;

    [Header("Aim Settings")]
    public float aimRotateSpeed = 60f;

    [Header("Character Animation")]
    public Animator characterAnimator;
    public string swingTriggerName = "Swing";
    public string idleStateName = "Idle";
    public float shootDelay = 0.6f;

    [Header("Character Position")]
    public Transform characterRoot;
    public Vector3 characterOffsetFromBall = new Vector3(-1.2f, 0f, -0.8f);
    public Vector3 characterRotationOffset = Vector3.zero;
    public bool keepCharacterNearBallWhileAiming = true;

    [Header("Game UI")]
    public TextMeshProUGUI strokeText;
    public TextMeshProUGUI windText;
    public Image leftWindImage;
    public Image rightWindImage;

    [Header("Power Gauge UI")]
    public Slider powerGaugeSlider;
    public float gaugeMoveSpeed = 1.5f;

    [Header("Button Sound")]
    public AudioSource buttonAudioSource;
    public AudioClip powerButtonSound;
    public AudioClip shootButtonSound;

    private float gaugeValue = 0f;
    private bool gaugeGoingUp = true;
    private bool isGaugeRunning = true;
    private bool isPowerSelected = false;

    private int strokeCount = 0;
    private bool wasMoving = false;
    private bool isSwinging = false;

    private BallMove ballMove;

    void Start()
    {
        ballMove = GetComponent<BallMove>();

        if (ballMove != null)
        {
            ballMove.windDirection = Vector3.zero;
            ballMove.windStrength = 0f;
        }

        if (characterRoot == null && characterAnimator != null)
        {
            characterRoot = characterAnimator.transform;
        }

        if (powerGaugeSlider != null)
        {
            powerGaugeSlider.minValue = 0f;
            powerGaugeSlider.maxValue = 1f;
            powerGaugeSlider.value = gaugeValue;
            powerGaugeSlider.interactable = false;
        }

        PlaceCharacterNearBall(true);

        UpdateStrokeUI();
        ShowShotInfo();
        ShowWindInfo();
    }

    void Update()
    {
        if (ballMove == null)
        {
            return;
        }

        if (wasMoving && !ballMove.IsMoving)
        {
            isPowerSelected = false;
            isGaugeRunning = true;

            PlaceCharacterNearBall(true);
        }

        wasMoving = ballMove.IsMoving;

        if (!ballMove.IsMoving && !isSwinging)
        {
            ControlAimDirection();

            if (keepCharacterNearBallWhileAiming)
            {
                PlaceCharacterNearBall(false);
            }

            ControlShotSetting();
            ControlWindSetting();

            if (isGaugeRunning)
            {
                UpdatePowerGauge();
            }
        }
    }

    void ControlAimDirection()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            transform.Rotate(Vector3.up, -aimRotateSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            transform.Rotate(Vector3.up, aimRotateSpeed * Time.deltaTime);
        }
    }

    void ControlShotSetting()
    {
        bool changed = false;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            angle += 5f;
            changed = true;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            angle -= 5f;
            changed = true;
        }

        angle = Mathf.Clamp(angle, minAngle, maxAngle);

        if (changed)
        {
            ShowShotInfo();
        }
    }

    void UpdatePowerGauge()
    {
        if (gaugeGoingUp)
        {
            gaugeValue += gaugeMoveSpeed * Time.deltaTime;

            if (gaugeValue >= 1f)
            {
                gaugeValue = 1f;
                gaugeGoingUp = false;
            }
        }
        else
        {
            gaugeValue -= gaugeMoveSpeed * Time.deltaTime;

            if (gaugeValue <= 0f)
            {
                gaugeValue = 0f;
                gaugeGoingUp = true;
            }
        }

        if (powerGaugeSlider != null)
        {
            powerGaugeSlider.value = gaugeValue;
        }
    }

    public void OnClickSetPowerButton()
    {
        if (ballMove == null || ballMove.IsMoving || isSwinging)
        {
            return;
        }

        PlayButtonSound(powerButtonSound);

        SelectPowerByGauge();

        isPowerSelected = true;
        isGaugeRunning = false;

        Debug.Log("파워 설정 완료. 발사 버튼을 누르면 발사됩니다.");
    }

    public void OnClickShootButton()
    {
        if (ballMove == null || ballMove.IsMoving || isSwinging)
        {
            return;
        }

        if (!isPowerSelected)
        {
            Debug.Log("먼저 파워 설정 버튼을 눌러주세요.");
            return;
        }

        PlayButtonSound(shootButtonSound);

        StartCoroutine(SwingAndShoot());
    }

    IEnumerator SwingAndShoot()
    {
        isSwinging = true;

        if (characterAnimator != null)
        {
            characterAnimator.ResetTrigger(swingTriggerName);
            characterAnimator.SetTrigger(swingTriggerName);
        }

        yield return new WaitForSeconds(shootDelay);

        Shoot();

        isSwinging = false;
    }

    void SelectPowerByGauge()
    {
        power = Mathf.Lerp(minPower, maxPower, gaugeValue);
        Debug.Log("선택된 파워: " + power);
    }

    void Shoot()
    {
        strokeCount++;
        UpdateStrokeUI();

        float radianAngle = angle * Mathf.Deg2Rad;

        Vector3 forwardVelocity = transform.forward * Mathf.Cos(radianAngle) * power;
        Vector3 upwardVelocity = Vector3.up * Mathf.Sin(radianAngle) * power;

        Vector3 shotVelocity = forwardVelocity + upwardVelocity;

        ballMove.Launch(shotVelocity);

        isPowerSelected = false;
        isGaugeRunning = false;

        Debug.Log("현재 타수: " + strokeCount);
        Debug.Log("발사! 각도: " + angle + "도 / 파워: " + power);
    }

    void PlaceCharacterNearBall(bool forceIdle)
    {
        if (characterRoot == null && characterAnimator != null)
        {
            characterRoot = characterAnimator.transform;
        }

        if (characterRoot == null)
        {
            return;
        }

        Vector3 targetPosition =
            transform.position
            + transform.right * characterOffsetFromBall.x
            + Vector3.up * characterOffsetFromBall.y
            + transform.forward * characterOffsetFromBall.z;

        characterRoot.position = targetPosition;

        Quaternion targetRotation =
            Quaternion.LookRotation(transform.forward, Vector3.up)
            * Quaternion.Euler(characterRotationOffset);

        characterRoot.rotation = targetRotation;

        if (forceIdle && characterAnimator != null && !string.IsNullOrEmpty(idleStateName))
        {
            characterAnimator.ResetTrigger(swingTriggerName);
            characterAnimator.Play(idleStateName, 0, 0f);
        }
    }

    void ControlWindSetting()
    {
        if (ballMove == null)
        {
            return;
        }

        bool changed = false;

        if (Input.GetKeyDown(KeyCode.A))
        {
            ballMove.windDirection = Vector3.left;
            changed = true;
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            ballMove.windDirection = Vector3.right;
            changed = true;
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            ballMove.windStrength -= 1f;
            changed = true;
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            ballMove.windStrength += 1f;
            changed = true;
        }

        ballMove.windStrength = Mathf.Clamp(
            ballMove.windStrength,
            minWindStrength,
            maxWindStrength
        );

        if (changed)
        {
            ShowWindInfo();
        }
    }

    void UpdateStrokeUI()
    {
        if (strokeText != null)
        {
            strokeText.text = "Stroke : " + strokeCount;
        }
    }

    void ShowShotInfo()
    {
        Debug.Log("현재 각도: " + angle + "도 / 현재 파워: " + power);
    }

    void ShowWindInfo()
    {
        if (ballMove == null)
        {
            return;
        }

        bool hasWindDirection = ballMove.windDirection != Vector3.zero;

        string directionText = "없음";

        if (ballMove.windDirection == Vector3.left)
        {
            directionText = "왼쪽";
        }
        else if (ballMove.windDirection == Vector3.right)
        {
            directionText = "오른쪽";
        }

        Debug.Log("현재 바람 방향: " + directionText + " / 현재 바람 세기: " + ballMove.windStrength);

        if (windText != null)
        {
            if (hasWindDirection)
            {
                windText.text = ballMove.windStrength.ToString("0");
            }
            else
            {
                windText.text = "0";
            }
        }

        if (leftWindImage != null)
        {
            leftWindImage.enabled = ballMove.windDirection == Vector3.left;
        }

        if (rightWindImage != null)
        {
            rightWindImage.enabled = ballMove.windDirection == Vector3.right;
        }
    }

    void PlayButtonSound(AudioClip clip)
    {
        if (buttonAudioSource != null && clip != null)
        {
            buttonAudioSource.PlayOneShot(clip);
        }
    }

    public float GetAngle()
    {
        return angle;
    }

    public float GetPreviewPower()
    {
        if (isPowerSelected)
        {
            return power;
        }

        return Mathf.Lerp(minPower, maxPower, gaugeValue);
    }

    public bool CanShowOrbit()
    {
        if (ballMove == null)
        {
            return false;
        }

        return !ballMove.IsMoving && !isSwinging;
    }
}
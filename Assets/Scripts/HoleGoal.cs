using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class HoleGoal : MonoBehaviour
{
    [Header("Goal Settings")]
    public float restartDelay = 3f;

    [Header("Goal UI")]
    public GameObject goalUI;

    [Header("Sound")]
    public AudioSource goalAudioSource;

    [Header("Ball Option")]
    public bool stopBallOnGoal = false;

    private bool isGoal = false;

    void Start()
    {
        if (goalUI != null)
        {
            goalUI.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isGoal)
        {
            return;
        }

        BallMove ballMove = other.GetComponentInParent<BallMove>();

        if (ballMove == null)
        {
            return;
        }

        StartCoroutine(GoalRoutine(ballMove));
    }

    IEnumerator GoalRoutine(BallMove ballMove)
    {
        isGoal = true;

        if (stopBallOnGoal && ballMove != null)
        {
            ballMove.StopBall();
        }

        if (goalUI != null)
        {
            goalUI.SetActive(true);
        }

        if (goalAudioSource != null)
        {
            goalAudioSource.Play();
        }

        yield return new WaitForSeconds(restartDelay);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
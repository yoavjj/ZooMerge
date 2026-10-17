using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionLostPopup : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private Button continueButton;

    [Header("Triggers")]
    [SerializeField] private string tryAgainTrigger = "TryAgain";
    [SerializeField] private string connectedTrigger = "Connected";

    [Header("Connection Check")]
    [SerializeField, Min(0.25f)]
    private float autoCheckInterval = 1f;

    private bool isChecking;
    private bool isClosing;

    private Action connectedCallback;

    private Coroutine monitorRoutine;

    private void Awake()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinuePressed);
    }

    private void OnEnable()
    {
        monitorRoutine =
            StartCoroutine(
                MonitorConnection()
            );
    }

    private void OnDisable()
    {
        if (monitorRoutine != null)
        {
            StopCoroutine(monitorRoutine);
            monitorRoutine = null;
        }
    }

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinuePressed);
    }

    public void Initialize(
        Action onConnectionRestored)
    {
        connectedCallback =
            onConnectionRestored;
    }

    // --------------------------------
    // MANUAL BUTTON
    // --------------------------------

    private void OnContinuePressed()
    {
        if (isChecking || isClosing)
            return;

        StartCoroutine(
            CheckConnectionRoutine(
                showTryAgain: true
            )
        );
    }

    // --------------------------------
    // AUTOMATIC CONNECTION MONITOR
    // --------------------------------

    private IEnumerator MonitorConnection()
    {
        while (!isClosing)
        {
            if (!isChecking)
            {
                yield return StartCoroutine(
                    CheckConnectionRoutine(
                        showTryAgain: false
                    )
                );
            }

            if (isClosing)
                yield break;

            yield return new WaitForSecondsRealtime(
                autoCheckInterval
            );
        }
    }

    // --------------------------------
    // CONNECTION CHECK
    // --------------------------------

    private IEnumerator CheckConnectionRoutine(
        bool showTryAgain)
    {
        if (isChecking || isClosing)
            yield break;

        isChecking = true;

        bool connected = false;

        yield return InternetConnectionChecker.Check(
            result => connected = result
        );

        isChecking = false;

        if (isClosing)
            yield break;

        if (connected)
        {
            HandleConnectionRestored();
            yield break;
        }

        // Only play TryAgain when the USER pressed
        // the button while still offline.
        //
        // Automatic background checks should remain silent.
        if (showTryAgain &&
            animator != null)
        {
            animator.ResetTrigger(
                connectedTrigger
            );

            animator.ResetTrigger(
                tryAgainTrigger
            );

            animator.SetTrigger(
                tryAgainTrigger
            );
        }
    }

    private void HandleConnectionRestored()
    {
        if (isClosing)
            return;

        isClosing = true;

        if (continueButton != null)
            continueButton.interactable = false;

        if (monitorRoutine != null)
        {
            StopCoroutine(monitorRoutine);
            monitorRoutine = null;
        }

        Debug.Log(
            "[ConnectionLostPopup] Internet connection restored."
        );

        if (animator != null &&
            !string.IsNullOrEmpty(
                connectedTrigger
            ))
        {
            animator.ResetTrigger(
                tryAgainTrigger
            );

            animator.ResetTrigger(
                connectedTrigger
            );

            animator.SetTrigger(
                connectedTrigger
            );
        }
        else
        {
            FinishConnected();
        }
    }

    // Animation Event at the END
    // of the Connected animation.
    public void AE_ConnectedFinished()
    {
        FinishConnected();
    }

    private void FinishConnected()
    {
        Action callback =
            connectedCallback;

        connectedCallback = null;

        callback?.Invoke();

        Destroy(gameObject);
    }
}
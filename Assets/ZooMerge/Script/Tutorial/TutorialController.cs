using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class TutorialController : MonoBehaviour
{
    [Serializable]
    private class TutorialDialogueStep
    {
        [Tooltip(
            "ID inside TutorialDialogueData."
        )]
        public string dialogueId;

        [Min(0f)]
        [Tooltip(
            "Delay before showing this dialogue."
        )]
        public float delayBefore;
    }

    public static bool IsActive { get; private set; }
    public static bool TutorialEnemyEnabled { get; private set; }
    public static event System.Action<bool> TutorialStateChanged;   

    [Header("Tutorial Debug")]
    [SerializeField] private bool startTutorialOnBoot;

    [Header("Refs")]
    [SerializeField] private BallSpawner ballSpawner;
    [SerializeField] private BallSet ballSet;

    [Header("Tutorial Intro")]
    [SerializeField] private PrefabLibrary prefabLibrary;
    [SerializeField] private Transform tutorialPopupContainer;

    private const string TUTORIAL_INTRO_POPUP =
        "TutorialIntroPopup";

    private TutorialIntroPopup tutorialIntroInstance;

    private bool waitingForFirstDropPress;

    private Coroutine dialogueSequenceRoutine;

    [Header("Tutorial Dialogue")]
    [SerializeField]
    private TutorialDialogueView dialogueView;

    [SerializeField]
    private TutorialDialogueData dialogueDatabase;

    [Header("Tutorial Dialogue Sequence")]
    [SerializeField]
    private List<TutorialDialogueStep>
        dialogueSequence = new();

    [Header("Tutorial Hints")]
    [SerializeField]
    private TutorialFingerHint firstDropFingerHint;

    [SerializeField, Min(0)]
    private int firstDropFingerArtIndex;

    [Header("Tutorial Ball")]
    [SerializeField] private BallType tutorialBallType = BallType.Doggy;

    [SerializeField, Min(1)]
    private int tutorialBallLevel = 1;

    [Header("Tutorial Enemy")]
    [SerializeField, Min(1)]
    private int tutorialEnemyId = 1;

    [SerializeField]
    private bool delayTutorialEnemyEnter = true;

    [Header("Tutorial Enemy Debug")]
    [SerializeField] private bool blockTutorialEnemy;

    private void Awake()
    {
        IsActive = startTutorialOnBoot;

        TutorialEnemyEnabled =
            !blockTutorialEnemy;
    }

    private IEnumerator Start()
    {
        if (!IsActive)
            yield break;

        Debug.Log(
            "[TutorialController] Tutorial requested."
        );

        yield return new WaitUntil(
            () => FirebaseInitializer.BootComplete
        );

        yield return new WaitUntil(
            () =>
                CircleDragInput.Instance != null &&
                BallFactoryAddressables.Instance != null &&
                SessionManager.Instance != null &&
                EnemySpawner.Instance != null
        );

        CircleDragInput.Instance.DisableInput();

        yield return StartCoroutine(
            PlayTutorialIntro()
        );

        StartTutorial();
    }

    private void StartTutorial()
    {
        if (ballSpawner == null)
        {
            Debug.LogError(
                "[TutorialController] BallSpawner is missing."
            );

            return;
        }

        if (ballSet == null)
        {
            Debug.LogError(
                "[TutorialController] BallSet is missing."
            );

            return;
        }

        BallSet.Entry entry =
            ballSet.GetEntry(
                tutorialBallType,
                tutorialBallLevel
            );

        if (entry == null)
        {
            Debug.LogError(
                $"[TutorialController] Could not find " +
                $"{tutorialBallType} level {tutorialBallLevel}."
            );

            return;
        }

        BallSet.EnemyEntry enemyEntry =
    ballSet.GetEnemyEntryById(
        tutorialEnemyId
    );

        if (enemyEntry == null)
        {
            Debug.LogError(
                $"[TutorialController] Could not find " +
                $"tutorial enemy ID {tutorialEnemyId}."
            );

            return;
        }

        // --------------------------------
        // CLEAN NORMAL GAME STATE
        // --------------------------------

        CircleDragInput.Instance.DisableInput();
        CircleDragInput.Instance.ClearSpawnContainer();

        EnemySpawner.Instance?.ClearEnemy();

        BallEventManager.RaiseResetCounters(
            keepUI: false
        );

        // --------------------------------
        // FORCE TUTORIAL BALL
        // --------------------------------

        ballSpawner.SetForcedEntry(entry);

        ballSpawner.SetPreviewVisible(true);
        ballSpawner.EnsureSessionWarm();

        // --------------------------------
        // START TUTORIAL SESSION
        // --------------------------------

        ballSpawner.BeginSession();

        BallEventManager.ResetMidLevelLossFlag();

        BallEventManager.RaiseSessionStarted();

        // --------------------------------
        // FIRST TUTORIAL INSTRUCTION
        // --------------------------------

        dialogueSequenceRoutine =
            StartCoroutine(
                PlayTutorialDialogueSequence()
            );

        // --------------------------------
        // SPAWN TUTORIAL ENEMY
        // --------------------------------
        if (TutorialEnemyEnabled)
        {
            EnemySpawner.Instance.SpawnEnemy(
                tutorialEnemyId,
                delayTutorialEnemyEnter
            );
        }
        else
        {
            Debug.Log(
                "[TutorialController] Tutorial enemy and enemy UI are blocked."
            );
        }

        StartCoroutine(
            EnableInputNextFrame()
        );

        Debug.Log(
            $"[TutorialController] Tutorial started. " +
            $"Ball: {entry.type} L{entry.level}. " +
            $"Enemy ID: {tutorialEnemyId}."
        );
    }

    private IEnumerator PlayTutorialIntro()
    {
        if (prefabLibrary == null)
        {
            Debug.LogWarning(
                "[TutorialController] PrefabLibrary is missing. " +
                "Skipping tutorial intro."
            );

            yield break;
        }

        TutorialIntroPopup prefab =
            prefabLibrary.GetTutorialIntroPopup(
                TUTORIAL_INTRO_POPUP
            );

        if (prefab == null)
        {
            Debug.LogWarning(
                "[TutorialController] TutorialIntroPopup " +
                "was not found. Skipping intro."
            );

            yield break;
        }

        Transform parent =
            tutorialPopupContainer != null
                ? tutorialPopupContainer
                : transform;

        tutorialIntroInstance =
            Instantiate(
                prefab,
                parent
            );

        bool introFinished = false;

        void HandleFinished()
        {
            introFinished = true;
        }

        tutorialIntroInstance.Finished +=
            HandleFinished;

        tutorialIntroInstance.Open();

        Debug.Log(
            "[TutorialController] Tutorial intro started."
        );

        yield return new WaitUntil(
            () => introFinished
        );

        if (tutorialIntroInstance != null)
        {
            tutorialIntroInstance.Finished -=
                HandleFinished;
        }

        tutorialIntroInstance = null;

        Debug.Log(
            "[TutorialController] Tutorial intro finished. " +
            "Starting gameplay tutorial."
        );
    }

    private IEnumerator PlayTutorialDialogueSequence()
    {
        if (dialogueSequence == null ||
            dialogueSequence.Count == 0)
        {
            dialogueSequenceRoutine = null;
            yield break;
        }

        for (int i = 0;
             i < dialogueSequence.Count;
             i++)
        {
            TutorialDialogueStep step =
                dialogueSequence[i];

            if (step == null ||
                string.IsNullOrEmpty(
                    step.dialogueId))
            {
                continue;
            }

            // ========================================================
            // DELAY BEFORE THIS DIALOGUE
            // ========================================================

            if (step.delayBefore > 0f)
            {
                yield return
                    new WaitForSecondsRealtime(
                        step.delayBefore
                    );
            }

            // ========================================================
            // SHOW DIALOGUE
            // ========================================================

            PlayTutorialDialogue(
                step.dialogueId
            );

            // Give DialogueView one frame to activate
            // and prepare its UI.
            yield return null;

            // ========================================================
            // FIRST DIALOGUE SPECIAL BEHAVIOUR
            // ========================================================

            if (i == 0)
            {
                firstDropFingerHint?.Play(
                    CircleDragInput.Instance,
                    firstDropFingerArtIndex
                );

                StartWaitingForFirstDropPress();
            }

            // ========================================================
            // WAIT UNTIL DIALOGUE IS FULLY GONE
            // ========================================================

            if (dialogueView != null)
            {
                yield return
                    new WaitUntil(
                        () =>
                            !dialogueView.IsVisible
                    );
            }

            // ========================================================
            // FIRST STEP ALSO WAITS FOR FINGER
            // ========================================================

            if (i == 0 &&
                firstDropFingerHint != null)
            {
                yield return
                    new WaitUntil(
                        () =>
                            !firstDropFingerHint.IsVisible
                    );
            }
        }

        dialogueSequenceRoutine = null;
    }

    private void StartWaitingForFirstDropPress()
    {
        if (waitingForFirstDropPress)
            return;

        if (CircleDragInput.Instance == null)
            return;

        waitingForFirstDropPress =
            true;

        CircleDragInput.Instance.ValidPointerDown +=
            HandleFirstDropPointerDown;
    }

    private void StopWaitingForFirstDropPress()
    {
        if (!waitingForFirstDropPress)
            return;

        waitingForFirstDropPress =
            false;

        if (CircleDragInput.Instance != null)
        {
            CircleDragInput.Instance.ValidPointerDown -=
                HandleFirstDropPointerDown;
        }
    }

    private void HandleFirstDropPointerDown()
    {
        if (!waitingForFirstDropPress)
            return;

        // Only react once.
        StopWaitingForFirstDropPress();

        // Dialogue plays its Out animation.
        dialogueView?.Hide();

        // Do NOT hide the finger here.
        //
        // TutorialFingerHint is already subscribed
        // to CircleDragInput.ValidPointerDown and
        // automatically runs HideRoutine().
    }

    private IEnumerator EnableInputNextFrame()
    {
        yield return null;

        CircleDragInput.Instance?.EnableInput();
    }

    public void CompleteTutorial()
    {
        if (!IsActive)
            return;

        StopWaitingForFirstDropPress();

        if (dialogueSequenceRoutine != null)
        {
            StopCoroutine(
                dialogueSequenceRoutine
            );

            dialogueSequenceRoutine = null;
        }

        if (tutorialIntroInstance != null)
        {
            Destroy(
                tutorialIntroInstance.gameObject
            );

            tutorialIntroInstance = null;
        }

        ballSpawner?.ClearForcedEntry();

        firstDropFingerHint?.HideImmediate();
        dialogueView?.HideImmediate();

        SetTutorialActive(false);

        Debug.Log(
            "[TutorialController] Tutorial completed. " +
            "Normal game flow restored."
        );
    }

    private void SetTutorialActive(bool active)
    {
        if (IsActive == active)
            return;

        IsActive = active;

        TutorialStateChanged?.Invoke(
            active
        );
    }

    // ============================================================
    // TUTORIAL DIALOGUE
    // ============================================================

    public void PlayTutorialDialogue(
        string dialogueId)
    {
        PlayTutorialDialogue(
            dialogueId,
            0f
        );
    }

    private void PlayTutorialDialogue(
        string dialogueId,
        float delay)
    {
        if (dialogueView == null)
        {
            Debug.LogWarning(
                "[TutorialController] " +
                "TutorialDialogueView is missing."
            );

            return;
        }

        if (dialogueDatabase == null)
        {
            Debug.LogWarning(
                "[TutorialController] " +
                "TutorialDialogueData is missing."
            );

            return;
        }

        if (string.IsNullOrEmpty(
                dialogueId))
        {
            return;
        }

        dialogueView.Play(
            dialogueDatabase,
            dialogueId,
            delay
        );
    }

    private void OnDisable()
    {
        StopWaitingForFirstDropPress();

        if (dialogueSequenceRoutine != null)
        {
            StopCoroutine(
                dialogueSequenceRoutine
            );

            dialogueSequenceRoutine = null;
        }
    }
}
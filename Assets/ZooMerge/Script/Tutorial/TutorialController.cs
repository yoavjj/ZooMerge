using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class TutorialController : MonoBehaviour
{
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

    [Header("Tutorial Dialogue")]
    [SerializeField]
    private TutorialDialogueView dialogueView;

    [SerializeField]
    private TutorialDialogueData dialogueDatabase;

    [SerializeField]
    private string firstDropDialogueId =
        "First_Drop";

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

        StartCoroutine(
            PlayFirstDropInstruction()
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

    private IEnumerator PlayFirstDropInstruction()
    {
        // --------------------------------
        // DIALOGUE
        // --------------------------------

        if (dialogueView != null &&
            dialogueDatabase != null)
        {
            yield return StartCoroutine(
                dialogueView.Show(
                    dialogueDatabase,
                    firstDropDialogueId
                )
            );
        }

        // --------------------------------
        // FINGER
        // --------------------------------

        firstDropFingerHint?.Play(
            CircleDragInput.Instance,
            firstDropFingerArtIndex
        );
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
}
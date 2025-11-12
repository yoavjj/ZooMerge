using System.Linq;
using UnityEngine;
using static BallEventManager;

public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private GameObject mainMenuPopupPrefab;
    [SerializeField] private GameObject winLosePopupPrefab;
    [SerializeField] private BallSpawner ballSpawner;

    private GameObject mainMenuPopupInstance;
    private GameObject gameUIPopupInstance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (mainMenuPopupPrefab != null)
        {
            mainMenuPopupInstance = Instantiate(mainMenuPopupPrefab, transform);
            mainMenuPopupInstance.SetActive(true);
        }
    }

    private void OnEnable()
    {
        BallEventManager.OnGameOver += ShowEndPopup;
    }

    private void OnDisable()
    {
        BallEventManager.OnGameOver -= ShowEndPopup;
    }

    private void ShowEndPopup(BallInfo info, GameOverReason reason)
    {
        if (gameUIPopupInstance == null)
        {
            gameUIPopupInstance = Instantiate(winLosePopupPrefab, transform);
        }

        var winLoseScript = gameUIPopupInstance.GetComponent<WinLosePopup>();
        if (winLoseScript != null)
        {
            string msg = reason switch
            {
                GameOverReason.Won => "You Won!",
                GameOverReason.Lost => "Game Over",
                _ => "Game Over"
            };

            winLoseScript.SetMessage(msg);
            winLoseScript.SetLevelMessage(MergeLevelManager.CurrentLevelNumber, reason);

            // Show continue if not first enemy
            if (reason == GameOverReason.Lost && MergeLevelManager.CurrentEnemyIndex > 0)
            {
                winLoseScript.ShowContinueOption();
            }
        }

        if (reason == GameOverReason.Won)
        {
            MergeLevelManager.AdvanceLevel();
        }
    }


    public void ShowEnemyDefeatedMessage()
    {
        if (gameUIPopupInstance == null)
        {
            gameUIPopupInstance = Instantiate(winLosePopupPrefab, transform);
        }

        var winLoseScript = gameUIPopupInstance.GetComponent<WinLosePopup>();
        if (winLoseScript != null)
        {
            winLoseScript.SetMessage("Enemy Defeated!");
            winLoseScript.SetLevelMessage(MergeLevelManager.CurrentLevelNumber, GameOverReason.Won); // still in same level
            winLoseScript.SetTemporaryMessage();
        }
    }



    public void ShowMainMenu()
    {
        if (mainMenuPopupPrefab != null)
        {
            mainMenuPopupInstance = Instantiate(mainMenuPopupPrefab, transform);
            mainMenuPopupInstance.SetActive(true);
        }
    }

    public void OnPlayButtonPressed(bool newLevel)
    {
        if (mainMenuPopupInstance != null)
            mainMenuPopupInstance.SetActive(false);

        ballSpawner?.BeginSession();

        BallEventManager.RaiseSessionStarted();

        int nextEnemyId = MergeLevelManager.GetCurrentEnemyId();
        EnemySpawner.Instance?.ClearEnemy();
        EnemySpawner.Instance?.SpawnEnemy(nextEnemyId);

        if (!newLevel)
        {
            BallEventManager.RaiseEnemyAdvanced();
        }


        // ✅ Save state immediately after new session starts
        BallStateSaver.Instance.SaveState(BallRegistry.ActiveBalls.ToArray());

        BallEventManager.ResetMidLevelLossFlag();
    }
}


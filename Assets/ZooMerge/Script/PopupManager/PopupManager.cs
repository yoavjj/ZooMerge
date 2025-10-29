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

    private void ShowEndPopup(BallInfo _, GameOverReason reason)
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

    public void OnPlayButtonPressed()
    {
        if (mainMenuPopupInstance != null) mainMenuPopupInstance.SetActive(false);
        ballSpawner?.BeginSession();
    }
}


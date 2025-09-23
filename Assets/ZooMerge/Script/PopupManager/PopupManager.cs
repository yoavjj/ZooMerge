using UnityEngine;

public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private GameObject mainMenuPopupPrefab;
    [SerializeField] private GameObject gameUIPopupPrefab;
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

        if (gameUIPopupPrefab != null)
        {
            gameUIPopupInstance = Instantiate(gameUIPopupPrefab, transform);
            gameUIPopupInstance.SetActive(false);
        }
    }

    public void ShowMainMenu()
    {
        if (mainMenuPopupInstance != null) mainMenuPopupInstance.SetActive(true);
        if (gameUIPopupInstance != null) gameUIPopupInstance.SetActive(false);
    }

    public void OnPlayButtonPressed()
    {
        if (mainMenuPopupInstance != null) mainMenuPopupInstance.SetActive(false);
        if (gameUIPopupInstance != null) gameUIPopupInstance.SetActive(true);

        ballSpawner?.BeginSession();
    }
}


using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    private Player player;
    public GameObject pauseMenu;
    public GameObject optionsMenu;
    private bool paused = false;

    void Awake()
    {
        player = GameObject.Find("Player").GetComponent<Player>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !player.IsDead())
        {
            if (IsPaused())
            {
                Unpause();
            }
            else
            {
                paused = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                optionsMenu.SetActive(false);
                pauseMenu.SetActive(true);
            }
        }
    }

    public void Unpause()
    {
        paused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        pauseMenu.SetActive(false);
        optionsMenu.SetActive(false);
    }

    public void ShowOptionsMenu()
    {
        optionsMenu.SetActive(true);
    }

    public void SaveAndQuit()
    {
        GameObject.Find("ChunkManager").GetComponent<ChunkManager>().SaveAllChunksToFile();
        GameObject.Find("Player").GetComponent<Player>().SavePlayerData();
        GameObject.Find("WorldClock").GetComponent<WorldClock>().SaveWorldTime();
        SceneManager.LoadScene(0);
    }

    public bool IsPaused()
    {
        return paused;
    }
}

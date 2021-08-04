using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject menuCamera;
    private uint playerCount = 0;
    private AsyncOperation loading;
    private Game game;

    void Start()
    {
        game = GameObject.Find("Game").GetComponent<Game>();
    }

    void Update()
    {
        if (loading == null)
        {
            EventSystem eventSystem = GameObject.Find("EventSystem").GetComponent<EventSystem>();

            if (eventSystem.currentSelectedGameObject == null)
            {
                GameObject button1P = GameObject.Find("Button1P");
                eventSystem.SetSelectedGameObject(null);
                eventSystem.SetSelectedGameObject(button1P);
            }
        }
        else if (loading.isDone)
        {
            Debug.Log("Loading...Done!");
            loading = null;
            StartGame(playerCount);
        }
        else
        {
            float loadProgress = loading.progress;
            Debug.Log("Loading..." + loadProgress * 100.0f + " %%");
        }
    }

    public void StartGame1P()
    {
        StartGameAsync(1);
    }

    public void StartGame2P()
    {
        StartGameAsync(2);
    }

    public void StartGameAsync(uint _playerCount)
    {
        playerCount = _playerCount;

        if (loading == null)
            loading = SceneManager.LoadSceneAsync("Level 1");
    }

    public void StartGame(uint _playerCount)
    {
        game.NewGame(_playerCount);
        menuCamera.SetActive(false);
    }
}

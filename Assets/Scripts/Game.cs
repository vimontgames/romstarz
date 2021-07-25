using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[Serializable]
public class CharacterInfo
{
    public string name;
    public Texture face;
    public string model;
}

[Serializable]
public class WeaponInfo
{
    public string name;
    public Texture icon;
    public string model;
    public string coloured;
    public List<Color> colors;
}

public class Game : MonoBehaviour
{
    public float gravity = -9.81f;
    public bool paused = true;
    public CharacterInfo[] characters = new CharacterInfo[0];
    public WeaponInfo[] weapons = new WeaponInfo[0];
    public GameObject player;
    public GameObject menu;

    private static Game instance;
    private GameObject mainMenu;
    private Transform players;

    public static Game Instance
    {
        get
        {
            if (instance == null)
            {
                instance = GameObject.Find("Game").GetComponent<Game>();
            }

            return instance;
        }
    }

    public CharacterInfo[] Characters
    {
        get { return characters; }
    }

    public WeaponInfo[] Weapons
    {
        get { return weapons; }
    }

    // Start is called before the first frame update
    void Start()
    {
        instance = this;

        mainMenu = menu.transform.Find("Main").gameObject;
        mainMenu.SetActive(true);
    }
   
    private void SetupAvatar(GameObject P, uint _padIndex, uint _modelIndex)
    {
        // enable player model
        var avatar = P.transform.Find("Avatar");
        var playerModel = avatar.Find(characters[_modelIndex].model);
        var playerName = characters[_modelIndex].name;

        P.name = playerName;
        var player = P.transform.GetComponent<Character>();
        player.padIndex = _padIndex;
        player.modelIndex = _modelIndex;

        playerModel.gameObject.SetActive(true); 
                 
        playerModel.transform.localPosition = new Vector3(0, 0, 0);
        
        Animator animator = playerModel.gameObject.GetComponent<Animator>();
        animator.runtimeAnimatorController = (RuntimeAnimatorController)Instantiate(playerModel.GetComponent<Animator>().runtimeAnimatorController);
        animator.avatar = (Avatar)Instantiate(playerModel.GetComponent<Animator>().avatar);

        // disable other models in player prefab
        for (int i = 0; i < characters.Length; ++i)
        {
            if (i != _modelIndex)
            {
                var otherModel = P.transform.Find("Avatar").Find(characters[i].model);
                if (null != otherModel)
                {
                    Destroy(otherModel.gameObject);
                }
            }

        }

        switch (_padIndex)
        {
            case 0:
                avatar.transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, 1));
                break;

            case 1:
                avatar.transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, 1));
                break;
        }

        if (_padIndex == 0xFF)
        {
            avatar.transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, -1));
            player.SetHuman(false);
        }
        else
            player.SetHuman(true);
    }

    public void NewGame(uint _playerCount)
    {
        var players = GameObject.FindGameObjectsWithTag("Player");

        foreach (var player in players)
        {
            Destroy(player);
        }

        switch (_playerCount)
        {
            case 1:
                {
                    // Human players
                    GameObject P1 = Instantiate(player, new Vector3(0.0f, 0.0f, -8.0f), Quaternion.identity);

                    SetupAvatar(P1, 0, 0);

                    Camera C1 = P1.GetComponentInChildren<Camera>();
                    C1.rect = new Rect(new Vector2(0, 0.0f), new Vector2(1.0f, 1.0f));
                }
                break;

            case 2:
                {
                    GameObject P1 = Instantiate(player, new Vector3(-1.0f, 00.0f, -8.0f), Quaternion.identity);

                    SetupAvatar(P1, 0, 0);

                    Camera C1 = P1.GetComponentInChildren<Camera>();
                    C1.rect = new Rect(new Vector2(0, 0), new Vector2(0.5f, 1));

                    GameObject P2 = Instantiate(player, new Vector3(+1.0f, 0.0f, -8.0f), Quaternion.identity);

                    SetupAvatar(P2, 1, 1);

                    Camera C2 = P2.GetComponentInChildren<Camera>();
                    C2.rect =  new Rect(new Vector2(0.5f, 0), new Vector2(0.5f, 1));

                    GameObject M2 = P2.transform.Find("Hud").transform.Find("Canvas").gameObject;
                    M2.GetComponent<RectTransform>().anchoredPosition = new Vector2(960.0f, 0.0f);

                    P2.GetComponentInChildren<AudioListener>().enabled = false;
                }
                break;
        }

        // AI players
        GameObject E1 = Instantiate(player, new Vector3(-1, 0, 8), Quaternion.identity);
        SetupAvatar(E1, 0xFF, 2);

        GameObject E2 = Instantiate(player, new Vector3(+1, 0, 8), Quaternion.identity);
        SetupAvatar(E2, 0xFF, 2);

        hideMenu();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void showMenu()
    {
        mainMenu.SetActive(true);

        var background = GameObject.Find("Background");
        if (background)
            background.gameObject.SetActive(true);

        paused = true;

        EventSystem eventSystem = GameObject.Find("EventSystem").GetComponent<EventSystem>();
        GameObject button1P = GameObject.Find("Button1P");
        eventSystem.SetSelectedGameObject(null);
        eventSystem.SetSelectedGameObject(button1P);

        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in players)
            player.SetActive(false);
    }

    public void hideMenu()
    {
        mainMenu.SetActive(false);
        paused = false;

        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in players)
            player.SetActive(true);
    }
}

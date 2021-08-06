using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

[Serializable]
public class CharacterInfo
{
    public string name;

    [Header("Display")]
    public Texture face;
    public string model;

    [Header("Sound")]
    public AudioSource jump;
    public AudioSource punch;
    public AudioSource slash;
    public AudioSource damage;
    public AudioSource die;
}

[Serializable]
public class WeaponInfo
{
    public string name;

    [Header("Gameplay")]
    public ProjectileType projectile = ProjectileType.None;
    public float rate = 2.0f;
    public uint damage = 0;

    [Header("Display")]
    public Texture icon;
    public string model;
    public string coloured;
    public List<Color> colors;

    [Header("Sound")]
    public float delay = 0.0f;
}

[Serializable]
public class ProjectileInfo
{
    public string name;

    [Header("Gameplay")]
    public int damage = 1;
    public float force = 1.0f;
    public float delay = 0.0f;
    public float lifeTime = 8.0f;
    public float offset = 1.0f;

    [Header("Display")]
    public Texture icon;
    public string model;
}

public class Game : MonoBehaviour
{
    public bool debug = false;
    public bool paused = true;
    public float gravity = -9.81f;
    public CharacterInfo[] characters = new CharacterInfo[0];
    public WeaponInfo[] weapons = new WeaponInfo[0];
    public ProjectileInfo[] projectiles = new ProjectileInfo[0];
    public GameObject player;
    public GameObject menu;
    public GameObject projectilesPrefab;

    private static Game instance;
    private GameObject mainMenu;
    private Transform players;

    public bool Debug
    {
        get { return debug; }
    }

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
        GameObject E1 = Instantiate(player, new Vector3(-2, 0, 8), Quaternion.identity);
        SetupAvatar(E1, 0xFF, 2);

        GameObject E2 = Instantiate(player, new Vector3(-1, 0, 8), Quaternion.identity);
        SetupAvatar(E2, 0xFF, 2);

        GameObject E3 = Instantiate(player, new Vector3(+1, 0, 8), Quaternion.identity);
        SetupAvatar(E3, 0xFF, 2);

        GameObject E4 = Instantiate(player, new Vector3(+2, 0, 8), Quaternion.identity);
        SetupAvatar(E4, 0xFF, 2);

        hideMenu();
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.dKey.wasPressedThisFrame)
            debug = !debug;

        if (Keyboard.current.kKey.wasPressedThisFrame)
            killAllEnnemies();
    }

    private void killAllEnnemies()
    {
        var characters = GameObject.FindGameObjectsWithTag("Player");
        foreach (var chara in characters)
        {
            if (!chara.GetComponent<Character>().Human)
                Destroy(chara);
        }
    }

    public void showMenu()
    {
        mainMenu.SetActive(true);
        mainMenu.GetComponent<MainMenu>().menuCamera.SetActive(true);

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

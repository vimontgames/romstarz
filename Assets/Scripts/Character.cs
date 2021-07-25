using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;



public class Character : MonoBehaviour
{
    public enum State 
    {
        Idle,
        Walk,
        Jump,
        Hit
    };

    public uint padIndex = 0;
    public uint modelIndex = 0;
    public float walkSpeed = 3.0f;
    public float runSpeed = 6.0f;
    public float accelleration = 10.0f;
    public float friction = 3.5f;
    public float jumpHeight = 1.0f;
    public int lifes = 3;
    public uint coins = 0;

    [Header("Sound")]
    public AudioSource jump;
    public AudioSource swish;

    [Header("Debug")]
    [ReadOnly]
    public float currentSpeed = 0.0f;

    private GameObject avatar;
    private State state = State.Idle;
    private bool human = false;
    private Vector3 start = new Vector3();
    private Camera playerCam;
    private Vector3 playerCamOffset;
    private CharacterController characterController;
    private bool grounded = true;
    private Vector3 velocity = new Vector3(0, 0, 0);
    private GameObject postprocess;
    private GameObject hud;
    private Volume postProcess;
    private ColorAdjustments colorAdustments;
    private float targetCamHeight = 0.0f;
    private float currentCamHeight = 0.0f;
    private float waitUntil = 0.0f;
    private float lastSlashTime = 0.0f;
    public GameObject rightHandWeapon;

    private Transform rightHand;

    private Vector2 camLimitsZ = new Vector2(-2.0f, 1.5f);

    public void SetHuman(bool _isHuman)
    {
        human = _isHuman;
    }

    public GameObject Avatar
    {
        get { return avatar; }
    }

    public GameObject Model
    {
        get { return Avatar.transform.Find(ModelName).gameObject; }
    }

    public bool Human
    {
        get { return human; }
    }

    public bool Dead
    {
        get { return lifes < 0; }
    }

    public string ModelName
    {
        get { return Game.Instance.Characters[modelIndex].model; }
    }

    public Animator Anim
    {
        get { return Model.GetComponent<Animator>(); }
    }

    public Transform RightHand
    {
        get { return rightHand; }
    }

    public GameObject RightHandWeapon
    {
        get { return rightHandWeapon; }
        set { rightHandWeapon = value; }
    }

    void Start()
    {
        avatar = transform.Find("Avatar").gameObject;
        start = avatar.transform.position;

        playerCam = transform.Find("Camera").gameObject.GetComponent<Camera>();
        postprocess = transform.Find("PostProcess").gameObject;
        hud = transform.Find("Hud").transform.Find("Canvas").gameObject;

        if (human)
        {
            playerCamOffset = new Vector3(0, 27, -15);
            Vector3 camPos = new Vector3(avatar.transform.position.x + playerCamOffset.x, playerCamOffset.y + currentCamHeight, avatar.transform.position.z + playerCamOffset.z);
            camPos.x = 0.0f;
            camPos.z = Mathf.Clamp(camPos.z, playerCamOffset.z + camLimitsZ.x, playerCamOffset.z + camLimitsZ.y);
            playerCam.transform.position = camPos;
        }
        else
        {
            Destroy(playerCam.gameObject);
            Destroy(postprocess);
            Destroy(hud);

            walkSpeed = 0.25f;
            runSpeed = 1;
        }

        characterController = avatar.GetComponent<CharacterController>();

        lifes = 3;
        coins = 0;

        StartPostProcess();
        UpdateUIOnce();

        waitUntil = Time.realtimeSinceStartup + UnityEngine.Random.Range(1.0f, 3.0f);

        rightHand = Model.transform.Find("mixamorig:Hips")
                                   .Find("mixamorig:Spine")
                                   .Find("mixamorig:Spine1")
                                   .Find("mixamorig:Spine2")
                                   .Find("mixamorig:RightShoulder")
                                   .Find("mixamorig:RightArm")
                                   .Find("mixamorig:RightForeArm")
                                   .Find("mixamorig:RightHand");
    }

    public void StartPostProcess()
    {
        if (!human)
            return;

        // Match camera and postprocess layer
        var layerName = "Player " + (padIndex+1).ToString();
        var layerIndex = LayerMask.NameToLayer(layerName);

        postprocess.gameObject.layer = layerIndex;
        transform.Find("Camera").GetComponent<UniversalAdditionalCameraData>().volumeLayerMask = 1 << layerIndex;

        postProcess = postprocess.gameObject.GetComponent<Volume>();

        for (int i = 0; i<postProcess.profile.components.Count; i++)
        {
            switch(postProcess.profile.components[i].name)
            {
                case "ColorAdjustments(Clone)":
                    colorAdustments = (ColorAdjustments) postProcess.profile.components[i];
                    break;
            }
        }

        colorAdustments.saturation.value = 0.0f;
    }

    public void Die()
    {
        lifes--;

        Debug.Log("Player " + padIndex.ToString() + " die. " + lifes.ToString() + " remaining");

        if (lifes >= 0)
        {
            avatar.transform.position = start;
        }
        else
        {
            colorAdustments.saturation.value = -100.0f;
        }
    }

    void UpdateUIOnce()
    {
        Game game = Game.Instance;

        hud.transform.Find("Name").GetComponent<Text>().text = game.characters[modelIndex].name;
        hud.transform.Find("Head").GetComponent<RawImage>().texture = game.characters[modelIndex].face;

        UpdateUI();
    }

    void UpdateUI()
    {
        hud.transform.Find("Lifes").GetComponent<Text>().text = " x " + math.max(0,lifes).ToString();
        hud.transform.Find("Coins").GetComponent<Text>().text = " x " + coins.ToString();

        AnimatorStateInfo info = Anim.GetCurrentAnimatorStateInfo(0);
        AnimatorClipInfo[] clips = Anim.GetCurrentAnimatorClipInfo(0);

        if (clips.Length > 0)
        {
            hud.transform.Find("Debug").GetComponent<Text>().text = "State: " + state.ToString() + "\n";
            hud.transform.Find("Debug").GetComponent<Text>().text += "Anim: " + clips[0].clip.name + " " + info.normalizedTime.ToString() + "\n";
        }
    }

    void Update()
    {
        Game game = Game.Instance;

        if (game.paused)
            return;

        float time = Time.realtimeSinceStartup;

        grounded = characterController.isGrounded;
        if (grounded && velocity.y < 0.0f)
            velocity.y = 0.0f;

        RaycastHit hit;
        if (grounded && Physics.Raycast(avatar.gameObject.transform.position, -Vector3.up, out hit))
        {
            Debug.DrawLine(avatar.gameObject.transform.position, hit.point, Color.white);
            targetCamHeight = hit.point.y;
        }
        currentCamHeight = Mathf.Lerp(currentCamHeight, targetCamHeight, Mathf.Clamp01(Time.deltaTime));
        
        Vector2 leftStick = new Vector2(0,0), rightStick = new Vector2(0,0);
        bool running = false, jumping = false, slash = false, drop = false;

        if (human)
        {
            var pads = Gamepad.all;

            if (padIndex < pads.Count)
            {
                var pad = pads[(int)padIndex];

                if (pad.startButton.isPressed == true)
                    game.showMenu();

                leftStick = pad.leftStick.ReadValue();
                rightStick = pad.rightStick.ReadValue();

                running = pad.buttonSouth.isPressed && grounded;
                jumping = pad.buttonNorth.isPressed && grounded;
                slash = pad.buttonEast.isPressed && grounded;
                drop = rightHandWeapon != null && pad.dpad.y.ReadValue() < 0;
            }
        }
        else
        {
            if (time < waitUntil)
                return;

            var players = GameObject.FindGameObjectsWithTag("Player");

            float minDist = 99999.99f;

            Character character = null;

            foreach (var go in players)
            {
                var player = go.GetComponent<Character>();

                if (player.Human)
                {
                    GameObject avatar = player.Avatar;

                    float dist = math.distance(avatar.transform.position, Avatar.transform.position);

                    if (dist < minDist)
                    {
                        minDist = dist;
                        character = player;
                    }
                }
            }

            if (character != null)
            {
                Vector3 delta = character.Avatar.transform.position - Avatar.transform.position;

                leftStick.x = delta.x;
                leftStick.y = delta.z;

                leftStick.Normalize();
            }
        }

        if (drop)
        {
            if (rightHandWeapon)
            {
                rightHandWeapon.GetComponent<Weapon>().DetachWeapon();
            }
        }

        if (slash && state != State.Hit && (time - lastSlashTime) > 1.5f)
        {
            swish.PlayDelayed(0.4f);
            lastSlashTime = time;
            state = State.Hit;
        }

        AnimatorStateInfo info = Anim.GetCurrentAnimatorStateInfo(0);
        AnimatorClipInfo[] clips = Anim.GetCurrentAnimatorClipInfo(0);

        // Don't move while hitting
        if (clips.Length > 0 && clips[0].clip.name == "Slash")
        {
            leftStick.Set(0, 0);
            velocity.Set(0, velocity.y, 0);
        }

        // bool slashing = clips[0].clip.name == "Slash" && waitForSlash >= time;

        if (Mathf.Abs(leftStick.x) < 0.01f)
            leftStick.x = 0.0f;
        if (Mathf.Abs(leftStick.y) < 0.01f)
            leftStick.y = 0.0f;

        if (Dead)
        {
            leftStick = new Vector2(0, 0);
            rightStick = new Vector2(0, 0);
            velocity = new Vector3(0, velocity.y, 0);
        }

        Vector3 dir = new Vector3(leftStick.x, 0.0f, leftStick.y).normalized;
        
        float speed = running ? runSpeed : walkSpeed;
        float accel = running ? accelleration * 2.0f : accelleration;

        velocity.x = Mathf.Clamp((velocity.x + leftStick.x * accel * Time.deltaTime), -speed, speed);
        velocity.z = Mathf.Clamp((velocity.z + leftStick.y * accel * Time.deltaTime), -speed, speed);

        velocity.x *= Mathf.Clamp01(1.0f - friction * Time.deltaTime);
        velocity.z *= Mathf.Clamp01(1.0f - friction * Time.deltaTime);

        if (jumping)
        {
            if (jump != null)
                jump.Play();

            velocity.y -= jumpHeight * game.gravity;

            state = State.Jump;
        }
        else
        {
            if (grounded && state == State.Jump)
                state = State.Idle;
        }

        velocity.y += game.gravity * Time.deltaTime;

        CollisionFlags collFlags = characterController.Move((dir+velocity) * Time.deltaTime);

        if (0 != (collFlags & CollisionFlags.Sides))
        {
            velocity.x = 0;
            velocity.z = 0;
        }

        if (state == State.Hit)
        {
            if ((time-lastSlashTime) > 1.0f)
                state = State.Idle;
        }

        if (state == State.Idle)
        {
            if (Mathf.Abs(velocity.x) > 0.01f || Mathf.Abs(velocity.z) > 0.01f)
                state = State.Walk;
        }

        if (state == State.Walk)
        {
            if (Mathf.Abs(velocity.x) < 0.01f && Mathf.Abs(velocity.z) < 0.01f)
                state = State.Idle;
        }

        if (0 != (collFlags & CollisionFlags.Above))
            velocity.y = game.gravity * Time.deltaTime;

        if (dir != Vector3.zero)
            avatar.transform.rotation = Quaternion.LookRotation(dir);
        
        if (playerCam != null)
        {
            Vector3 camPos = new Vector3(avatar.transform.position.x + playerCamOffset.x, playerCamOffset.y + currentCamHeight, avatar.transform.position.z + playerCamOffset.z);

            camPos.x = 0.0f;
            camPos.z = Mathf.Clamp(camPos.z, playerCamOffset.z + camLimitsZ.x, playerCamOffset.z + camLimitsZ.y);

            playerCam.transform.position = camPos;
        }

        if (!Dead)
        {
            if (avatar.transform.position.y < -10)
                Die();
        }

        currentSpeed = Mathf.Sqrt(velocity.x*velocity.x + velocity.z * velocity.z);

        if (Anim)
        {
            Anim.SetFloat("Speed", currentSpeed);

            if (Human)
                Anim.SetBool("Slash", state == State.Hit);
        }

        if (Human)
        {
            UpdateUI();
        }

        Debug.DrawLine(Avatar.transform.position, Avatar.transform.position + dir.normalized);
    }

    public void AddCoin(uint _count)
    {
        coins += _count;
    }
}

using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;



public class Player : MonoBehaviour
{
    public uint padIndex = 0;
    public float walkSpeed = 3.0f;
    public float runSpeed = 6.0f;
    public float accelleration = 10.0f;
    public float friction = 3.5f;
    public float jump = 1.0f;
    public int lifes = 3;
    public uint coins = 0;
    public float curSpeed = 0.0f;
    
    private GameObject avatar;
    private Vector3 start = new Vector3();
    private Camera playerCam;
    private Vector3 playerCamOffset;
    private CharacterController characterController;
    private bool grounded = true;
    private Vector3 velocity = new Vector3(0, 0, 0);
    private GameObject hud;
    private Volume postProcess;
    private ColorAdjustments colorAdustments;
    private float targetCamHeight = 0.0f;
    private float currentCamHeight = 0.0f;

    private Vector2 camLimitsZ = new Vector2(-2.0f, 1.5f);

    void Start()
    {
        avatar = transform.Find("Avatar").gameObject;
        start = avatar.transform.position;
        playerCam = transform.Find("Camera").gameObject.GetComponent<Camera>();
        playerCamOffset = new Vector3(0, 27, -15);
        characterController = avatar.GetComponent<CharacterController>();
        hud = transform.Find("Hud").transform.Find("Canvas").gameObject;

        lifes = 3;
        coins = 0;

        Vector3 camPos = new Vector3(avatar.transform.position.x + playerCamOffset.x, playerCamOffset.y + currentCamHeight, avatar.transform.position.z + playerCamOffset.z);
        camPos.x = 0.0f;
        camPos.z = Mathf.Clamp(camPos.z, playerCamOffset.z + camLimitsZ.x, playerCamOffset.z + camLimitsZ.y);
        playerCam.transform.position = camPos;

        StartPostProcess();

        UpdateUIOnce();
    }

    public void StartPostProcess()
    {
        // Match camera and postprocess layer
        var layerName = "Player " + (padIndex+1).ToString();
        var layerIndex = LayerMask.NameToLayer(layerName);

        transform.Find("PostProcess").gameObject.layer = layerIndex;
        transform.Find("Camera").GetComponent<UniversalAdditionalCameraData>().volumeLayerMask = 1 << layerIndex;

        postProcess = transform.Find("PostProcess").gameObject.GetComponent<Volume>();

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
        Game game = Game.get();

        hud.transform.Find("Name").GetComponent<Text>().text = game.playerInfos[padIndex].name;
        hud.transform.Find("Head").GetComponent<RawImage>().texture = game.playerInfos[padIndex].face;

        UpdateUI();
    }

    void UpdateUI()
    {
        hud.transform.Find("Lifes").GetComponent<Text>().text = " x " + math.max(0,lifes).ToString();
        hud.transform.Find("Coins").GetComponent<Text>().text = " x " + coins.ToString();
    }

    public bool IsDead()
    {
        return lifes < 0;
    }

    void Update()
    {
        Game game = Game.get();

        if (game.paused)
            return;

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

        var pads = Gamepad.all;

        if (padIndex >= pads.Count)
        {
            velocity.y += game.gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);
            return;
        }

        var pad = pads[(int)padIndex];

        if (pad.startButton.isPressed == true)
            game.showMenu();

        var leftStick = pad.leftStick.ReadValue();
        var rightStick = pad.rightStick.ReadValue();

        if (Mathf.Abs(leftStick.x) < 0.01f)
            leftStick.x = 0.0f;
        if (Mathf.Abs(leftStick.y) < 0.01f)
            leftStick.y = 0.0f;

        if (IsDead())
        {
            leftStick = new Vector2(0, 0);
            rightStick = new Vector2(0, 0);
            velocity = new Vector3(0, velocity.y, 0);
        }

        Vector3 dir = new Vector3(leftStick.x, 0.0f, leftStick.y).normalized;

        bool running = pad.buttonSouth.isPressed && grounded;

        float speed = running ? runSpeed : walkSpeed;
        float accel = running ? accelleration * 2.0f : accelleration;

        velocity.x = Mathf.Clamp((velocity.x + leftStick.x * accel * Time.deltaTime), -speed, speed);
        velocity.z = Mathf.Clamp((velocity.z + leftStick.y * accel * Time.deltaTime), -speed, speed);

        velocity.x *= Mathf.Clamp01(1.0f - friction * Time.deltaTime);
        velocity.z *= Mathf.Clamp01(1.0f - friction * Time.deltaTime);

        if (pad.buttonEast.isPressed && grounded)
            velocity.y -= jump * game.gravity;

        velocity.y += game.gravity * Time.deltaTime;

        CollisionFlags collFlags = characterController.Move((dir+velocity) * Time.deltaTime);

        if (0 != (collFlags & CollisionFlags.Sides))
        {
            velocity.x = 0;
            velocity.z = 0;
        }

        if (0 != (collFlags & CollisionFlags.Above))
            velocity.y = game.gravity * Time.deltaTime;

        if (dir != Vector3.zero)
            avatar.transform.rotation = Quaternion.LookRotation(dir);

        Vector3 camPos = new Vector3(avatar.transform.position.x + playerCamOffset.x, playerCamOffset.y + currentCamHeight, avatar.transform.position.z + playerCamOffset.z);

        camPos.x = 0.0f;
        camPos.z = Mathf.Clamp(camPos.z, playerCamOffset.z + camLimitsZ.x, playerCamOffset.z + camLimitsZ.y);

        playerCam.transform.position = camPos;

        if (!IsDead())
        {
            if (avatar.transform.position.y < -10)
                Die();
        }

        curSpeed = Mathf.Sqrt(velocity.x*velocity.x + velocity.z * velocity.z);

        var model = avatar.gameObject.transform.Find(game.playerInfos[padIndex].model);
        var anim = model.GetComponent<Animator>();
        if (anim)
        {
            anim.SetFloat("Speed", curSpeed);
        }

        UpdateUI();        
    }

    public void AddCoin(uint _count)
    {
        coins += _count;
    }
}

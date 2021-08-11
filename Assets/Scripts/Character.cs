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
        Slash,
        Punch,
        Damage,
        Die
    };

    public uint padIndex = 0;
    public uint modelIndex = 0;
    public float walkSpeed = 3.0f;
    public float runSpeed = 6.0f;
    public float accelleration = 10.0f;
    public float friction = 3.5f;
    public float jumpHeight = 1.0f;
    public float health = 1000.0f;
    public float recoveryPerSecond = 5;
    private float startHealth;
    public float turbo = 100.0f;
    public int lifes = 3;
    public uint coins = 0;

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
    private Vignette vignette;
    private float targetCamHeight = 0.0f;
    private float currentCamHeight = 0.0f;
    private float waitUntil = 0.0f;
    private float lastFireTime = 0.0f;
    private GameObject rightHandWeapon;
    private GameObject statusBar;
    private Vector3 direction = new Vector3(0, 0, 0);
    private Vector3 lastNormalizedDir = new Vector3(0, 0, 0);
    private Transform rightHand;
    private Vector2 camLimitsZ = new Vector2(-2.0f, 1.5f);
    private float lastDamageTime = 0.0f;
    private Image healthBarImg;
    private Vector3 previousPos = new Vector3(0, 0, 0);
    private bool moving = false;
    private float lastRespawnTime = 0.0f;

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
        get { return Avatar.transform.Find(CharInfo.model).gameObject; }
    }

    public bool Human
    {
        get { return human; }
    }

    public bool Dead
    {
        get { return lifes < 0; }
    }

    public Character.State CharState
    {
        get { return state; }
    }

    public CharacterInfo CharInfo
    {
        get { return Game.Instance.Characters[modelIndex]; }
    }

    public WeaponType CurWeaponType
    {
        get
        {
            if (RightHandWeapon)
                return RightHandWeapon.GetComponent<Weapon>().weaponType;
            else
                return WeaponType.Fists;
        }
}

    public WeaponInfo WeapInfo
    {
        get { return Game.Instance.Weapons[(uint)CurWeaponType]; }
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
        previousPos = transform.position;

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

        statusBar = gameObject.transform.Find("Status").gameObject;

        characterController = avatar.GetComponent<CharacterController>();

        lifes = Human ? 3 : 0;
        coins = 0;

        startHealth = health;

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

        DetectJoystickType();
    }

    void DetectJoystickType()
    {
        if (padIndex < Gamepad.all.Count)
        {
            var pad = Gamepad.all[(int)padIndex];
            string name = pad.name;
        }
    }

    public void StartPostProcess()
    {
        if (Human)
        {
            // Match camera and postprocess layer
            var layerName = "Player " + (padIndex + 1).ToString();
            var layerIndex = LayerMask.NameToLayer(layerName);

            postprocess.gameObject.layer = layerIndex;
            transform.Find("Camera").GetComponent<UniversalAdditionalCameraData>().volumeLayerMask = 1 << layerIndex;

            postProcess = postprocess.gameObject.GetComponent<Volume>();

            for (int i = 0; i < postProcess.profile.components.Count; i++)
            {
                switch (postProcess.profile.components[i].name)
                {
                    case "ColorAdjustments(Clone)":
                        colorAdustments = (ColorAdjustments)postProcess.profile.components[i];
                        break;

                    case "Vignette(Clone)":
                        vignette = (Vignette)postProcess.profile.components[i];
                        break;
                }
            }

            colorAdustments.saturation.value = 0.0f;
            vignette.intensity.value = 0.0f;
        }
    }

    private IEnumerator Respawn(float delay)
    {
        yield return new WaitForSeconds(delay);

        avatar.transform.position = start;
        health = 1000;
        state = State.Idle;
        lastRespawnTime = Time.realtimeSinceStartup;

        coins = 0;
    }

    public void Die()
    {
        health = 0;
        lifes--;

        Debug.Log("Character " + padIndex.ToString() + " dies. " + lifes.ToString() + " remaining");

        state = State.Die;

        if (CharInfo.die != null)
            CharInfo.die.Play();

        if (lifes >= 0)
        {
            lastDamageTime = Time.realtimeSinceStartup;
        }
        else
        {
            characterController.enabled = false;

            if (Human)
            {
                Avatar.SetActive(false);
                colorAdustments.saturation.value = -100.0f;
            }

            coins = 0;
        }

        if (coins > 0)
        {
            for (int i = 0; i < coins; ++i)
            {
                Vector3 offset = new Vector3(UnityEngine.Random.Range(-1.0f, 1.0f), 0.5f, UnityEngine.Random.Range(-1.0f, 1.0f));
                StartCoroutine(DropCoin(1.0f + 0.125f * (float)i, Avatar.transform.position + offset));
            }
        }
    }

    private IEnumerator DropCoin(float delay, Vector3 pos)
    {
        yield return new WaitForSeconds(delay);
        
        GameObject droppedCoin = Instantiate(Game.Instance.coin, pos, Quaternion.identity);
        Coin coin = droppedCoin.GetComponentInChildren<Coin>();
        coin.Visible = true;
        coin.sound.Play();
    }

    void UpdateUIOnce()
    {
        Game game = Game.Instance;

        hud.transform.Find("Name").GetComponent<Text>().text = game.characters[modelIndex].name;
        hud.transform.Find("Head").GetComponent<RawImage>().texture = game.characters[modelIndex].face;

        healthBarImg = statusBar.transform.Find("HealthBar").GetComponent<Image>();

        UpdateUI();
    }

    void UpdateUI()
    {
        hud.transform.Find("Lifes").GetComponent<Text>().text = " x " + math.max(0,lifes).ToString();
        hud.transform.Find("Coins").GetComponent<Text>().text = " x " + coins.ToString();

        string dbgText = "";
        if (Game.Instance.Debug)
        {
            AnimatorStateInfo info = Anim.GetCurrentAnimatorStateInfo(0);
            AnimatorClipInfo[] clips = Anim.GetCurrentAnimatorClipInfo(0);

            if (clips.Length > 0)
            {
                dbgText = "State: " + state.ToString() + "\n";
                dbgText += "Anim: " + clips[0].clip.name + " " + info.normalizedTime.ToString() + "\n";
            }
        }
  
        hud.transform.Find("Debug").GetComponent<Text>().text = dbgText;
    }

    void UpdatePostProcess()
    {
        if (Human)
        {
            float intensity = 0.31f;
            float damageDelay = 1.0f;

            float t = Time.realtimeSinceStartup - lastDamageTime;

            if (state == State.Die)
                vignette.intensity.value = intensity;
            else if (lastDamageTime > 0.0f && t < damageDelay)
                vignette.intensity.value = intensity * Mathf.Cos(t * Mathf.PI / damageDelay);
            else
                vignette.intensity.value = 0.0f;
        }
    }

    void UpdateStatus()
    {
        statusBar.transform.position = Avatar.transform.position + new Vector3(0.0f, 2.5f, 0.0f);
        statusBar.transform.rotation = new Quaternion(0.5f, 0.0f, 0.0f, 0.9f); // Quaternion.LookRotation(this.playerCam.transform.position);

        healthBarImg.fillAmount = health / startHealth;

        string text;
        int healthInt = (int)health;
        text = healthInt.ToString() + "\n";

        if (Game.Instance.Debug)
        {
            text += state.ToString();
        }

        statusBar.transform.Find("HealthText").GetComponent<Text>().text = text;

        if (health > 0)
            statusBar.GetComponent<Canvas>().enabled = true;
        else
            statusBar.GetComponent<Canvas>().enabled = false;
    }

    public bool IsBlinking
    {
        get { return lastRespawnTime != 0.0f && Time.realtimeSinceStartup < lastRespawnTime + 2.0f; }
    }

    void Blink()
    {
        if (IsBlinking)
        {
            float blinkingTime = Time.realtimeSinceStartup - lastRespawnTime;
            Model.SetActive(((int)(blinkingTime * 15.0f) & 1) == 1 ? true : false);
        }
        else
            Model.SetActive(true);
    }

    void Update()
    {
        Game game = Game.Instance;

        if (game.paused)
            return;

        Blink();

        moving = (Avatar.transform.position - previousPos).magnitude > 1.0f * Time.deltaTime;

        previousPos = Avatar.transform.position;

        if (Human && state != State.Die)
        {
            health = Mathf.Min(startHealth, health + Time.deltaTime * recoveryPerSecond);
        }

        UpdateStatus();

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
        bool running = false, jumping = false, fire = false, drop = false, start = false;
        
        if (human)
        {
            var pads = Gamepad.all;
            bool canFire = grounded;
            bool canDrop = rightHandWeapon != null;

            if (padIndex < pads.Count)
            {
                var pad = pads[(int)padIndex];

                start = pad.startButton.isPressed;
                    
                leftStick = pad.leftStick.ReadValue();
                rightStick = pad.rightStick.ReadValue();

                running = pad.buttonSouth.isPressed && grounded;
                jumping = pad.buttonNorth.isPressed && grounded;
                fire = pad.buttonEast.isPressed && canFire;
                drop = canDrop && pad.dpad.y.ReadValue() < 0;
            }
            else
            {
                // Emulate missing joystick with keyboard
                if (padIndex == pads.Count)
                {
                    start = Keyboard.current.escapeKey.wasPressedThisFrame;

                    leftStick.x = Keyboard.current.leftArrowKey.isPressed ? -1.0f : 0.0f;
                    leftStick.x = Keyboard.current.rightArrowKey.isPressed ? +1.0f : leftStick.x;

                    leftStick.y = Keyboard.current.upArrowKey.isPressed ? +1.0f : 0.0f;
                    leftStick.y = Keyboard.current.downArrowKey.isPressed ? -1.0f : leftStick.y;

                    running = Keyboard.current.sKey.isPressed && grounded;
                    jumping = Keyboard.current.aKey.isPressed && grounded;
                    fire = Keyboard.current.spaceKey.isPressed && canFire;
                    drop = canDrop && Keyboard.current.dKey.isPressed;
                }
            }

            if (start)
                game.showMenu();
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

                float shootDist;
                if (RightHandWeapon == null)
                    shootDist = 2.0f;
                else
                    shootDist = 10.0f;

                if (minDist < shootDist && character.health > 0)
                {
                    fire = true;
                    //waitUntil = Time.realtimeSinceStartup + UnityEngine.Random.Range(3.0f, 5.0f);
                }
            }
        }

        if (state == State.Die)
        {
            running = false;
            jumping = false;
            fire = false;
            drop = rightHandWeapon != null;
        }

        if (drop)
        {
            if (rightHandWeapon)
            {
                rightHandWeapon.GetComponent<Weapon>().DetachWeapon();
            }
        }

        if (fire && state != State.Slash && state != State.Punch && (time - lastFireTime) > WeapInfo.rate)
        {
            switch (CurWeaponType)
            {
                case WeaponType.Fists:
                    if (CharInfo.punch != null)
                        CharInfo.punch.PlayDelayed(WeapInfo.delay);
                    break;

                case WeaponType.Racket:
                    if (CharInfo.slash != null)
                        CharInfo.slash.PlayDelayed(WeapInfo.delay);
                    break;
            }
            lastFireTime = time;
            
            // rotate towards closest ennemy if not aiming
            if (leftStick == Vector2.zero)
            {
                var avatars = GameObject.FindGameObjectsWithTag("Avatar");
                GameObject closest = null;
                float dist = 9999.0f;
                for (int i = 0; i < avatars.Length; ++i)
                {
                    Character character = avatars[i].transform.parent.GetComponent<Character>();
                    if (!character.Human && character.health > 0)
                    {
                        float d = (character.Avatar.gameObject.transform.position - Avatar.transform.position).magnitude;
                        if (d < dist)
                        {
                            dist = d;
                            closest = character.Avatar.gameObject;
                        }
                    }
                }
                if (null != closest)
                {
                    this.lastNormalizedDir = (closest.transform.position - Avatar.transform.position).normalized;
                    this.direction = lastNormalizedDir;
                }
            }

            GameObject projPrefab = Game.Instance.projectile; ;

            if (RightHandWeapon)
            {
                state = State.Slash;
            }
            else
            {
                state = State.Punch;
            }

            float err = 0.05f;
            float errY = 0.025f;
            Vector3 shootDir = new Vector3(lastNormalizedDir.x + UnityEngine.Random.Range(-err, err), UnityEngine.Random.Range(-errY, errY), lastNormalizedDir.z + UnityEngine.Random.Range(-err, err)).normalized;
            StartCoroutine(SendProjectile(projPrefab, WeapInfo.projectile, shootDir));

            avatar.transform.rotation = Quaternion.LookRotation(lastNormalizedDir);
        }

        if (state == State.Die && lifes >= 0 && Time.realtimeSinceStartup > lastDamageTime + 4.0f)
        {
            StartCoroutine(Respawn(2.0f));
            return;
        }

        AnimatorStateInfo info = Anim.GetCurrentAnimatorStateInfo(0);
        AnimatorClipInfo[] clips = Anim.GetCurrentAnimatorClipInfo(0);

        bool canMove;
        switch (state)
        {
            //case State.Slash:
            //case State.Punch:
            case State.Damage:
            case State.Die:
                canMove = false;
                break;

            default:
                canMove = true;
                break;
        }

        // Don't move while hitting
        if (!canMove)
        {
            leftStick.Set(0, 0);
            velocity.Set(0, velocity.y, 0);
        }

        if (Mathf.Abs(leftStick.x) < 0.05f)
            leftStick.x = 0.0f;
        if (Mathf.Abs(leftStick.y) < 0.05f)
            leftStick.y = 0.0f;

        if (Dead)
        {
            leftStick = new Vector2(0, 0);
            rightStick = new Vector2(0, 0);
            velocity = new Vector3(0, velocity.y, 0);
        }

        if (leftStick.magnitude > 0.0f)
            direction = new Vector3(leftStick.x, 0.0f, leftStick.y).normalized;
        
        float speed = running ? runSpeed : walkSpeed;
        float accel = running ? accelleration * 2.0f : accelleration;

        speed *= leftStick.magnitude;

        velocity.x = Mathf.Clamp((velocity.x + leftStick.x * accel * Time.deltaTime), -speed, speed);
        velocity.z = Mathf.Clamp((velocity.z + leftStick.y * accel * Time.deltaTime), -speed, speed);

        velocity.x *= Mathf.Clamp01(1.0f - friction * Time.deltaTime);
        velocity.z *= Mathf.Clamp01(1.0f - friction * Time.deltaTime);

        if (jumping)
        {
            if (CharInfo.jump != null)
                CharInfo.jump.Play();

            velocity.y -= jumpHeight * game.gravity;

            state = State.Jump;
        }
        else
        {
            if (grounded && state == State.Jump)
                state = State.Idle;
        }

        velocity.y += game.gravity * Time.deltaTime;

        CollisionFlags collFlags = CollisionFlags.None;

        Vector3 totalMove = direction * leftStick.magnitude + velocity;

        if (characterController.enabled)
            collFlags = characterController.Move((totalMove) * Time.deltaTime);

        if (0 != (collFlags & CollisionFlags.Sides))
        {
            velocity.x = 0;
            velocity.z = 0;

            leftStick.x = 0;
            leftStick.y = 0;

            waitUntil = Time.realtimeSinceStartup + UnityEngine.Random.Range(1.0f, 2.0f);
        }

        if (state == State.Slash || state == State.Punch)
        {
            if ((time-lastFireTime) > 1.0f)
                state = State.Idle;
        }

        if (state == State.Damage && Time.realtimeSinceStartup > lastDamageTime + 1.0f)
            state = State.Idle;

        if (state == State.Idle)
        {
            if (Mathf.Abs(totalMove.x) > 0.001f || Mathf.Abs(totalMove.z) > 0.001f)
                state = State.Walk;
        }

        if (state == State.Walk)
        {
            if (Mathf.Abs(totalMove.x) < 0.001f && Mathf.Abs(totalMove.z) < 0.001f)
                state = State.Idle;
        }

        if (0 != (collFlags & CollisionFlags.Above))
            velocity.y = game.gravity * Time.deltaTime;

        if (direction != Vector3.zero && state != State.Slash)
        {
            lastNormalizedDir = direction.normalized;
            avatar.transform.rotation = Quaternion.LookRotation(lastNormalizedDir);
        }

        if (playerCam != null)
        {
            Vector3 camPos = new Vector3(avatar.transform.position.x + playerCamOffset.x, playerCamOffset.y + currentCamHeight, avatar.transform.position.z + playerCamOffset.z);

            camPos.x = 0.0f;
            camPos.z = Mathf.Clamp(camPos.z, playerCamOffset.z + camLimitsZ.x, playerCamOffset.z + camLimitsZ.y);

            playerCam.transform.position = camPos;
        }

        if (!Dead)
        {
            if (avatar.transform.position.y < -16)
            {
                Die();

                if (lifes >= 0)
                    StartCoroutine(Respawn(1.0f));
            }
        }

        currentSpeed = Mathf.Sqrt(totalMove.x * totalMove.x + totalMove.z*totalMove.z);

        if (Anim)
        {
            switch (state)
            {
                default:
                    Anim.SetLayerWeight(1, 0.0f);
                    break;

                case State.Idle:
                case State.Punch:
                case State.Slash:
                case State.Jump:
                    Anim.SetLayerWeight(1, 1.0f);
                    break;

            }

            Anim.SetFloat("Speed", state == State.Idle || !moving ? 0.0f : currentSpeed);
            Anim.SetBool("Slash", state == State.Slash);
            Anim.SetBool("Punch", state == State.Punch);
            Anim.SetBool("Damage", state == State.Damage);
            Anim.SetBool("Die", state == State.Die);
        }

        if (Human)
        {
            UpdateUI();
        }

        UpdatePostProcess();

        Debug.DrawLine(Avatar.transform.position, Avatar.transform.position + direction.normalized);
    }

    public void AddCoin(uint _count)
    {
        coins += _count;
    }

    private IEnumerator SendProjectile(GameObject projPrefab, ProjectileType projType, Vector3 dir)
    {
        ProjectileInfo projInfo = Game.Instance.projectiles[(int)projType];

        yield return new WaitForSeconds(projInfo.delay);
        
        GameObject projModel = projPrefab.transform.Find(projInfo.model).gameObject;

        GameObject projObj = Instantiate(projModel, Avatar.transform.position + dir * projInfo.offset + new Vector3(0, grounded ? 1.25f : 1.5f, 0), Quaternion.identity);
        Projectile proj = projObj.GetComponentInChildren<Projectile>();
        proj.projectileType = projType;
        proj.Owner = gameObject;

        float force = grounded ? projInfo.force : projInfo.forceJump;

        projObj.GetComponent<Rigidbody>().AddForce(dir * force, ForceMode.Impulse);
    }

    public int takeHit(GameObject from)
    {
        if (state == State.Die)
            return 0;

        int damage = 0;

        var projectile = from.GetComponent<Projectile>();
        if (projectile != null)
            damage = projectile.projInfo.damage;

        var chara = from.GetComponent<Character>();
        if (chara != null)
            damage = (int)chara.WeapInfo.damage;

        this.health -= damage;

        //characterController.AddForce((transform.position - from.transform.position).normalized, ForceMode.Impulse);

        AudioSource sound = null;

        if (health <= 0)
        {
            health = 0;
            Die();   
        }
        else
        {
            state = State.Damage;
            sound = CharInfo.damage;
        }

        if (sound != null)
            sound.Play();

        lastDamageTime = Time.realtimeSinceStartup;

        return damage;
    }
}

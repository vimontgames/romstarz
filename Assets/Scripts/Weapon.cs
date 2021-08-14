using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum WeaponType
{
    Fists,
    Racket,
    Sword
}

public class Weapon : CustomPrefab
{
    public WeaponType weaponType = WeaponType.Racket;
    public float timeBeforePick = 3.0f;

    [Header("Sound")]
    public AudioSource pickSound;
    public AudioSource dropSound;

    private bool dropped = true;
    private float nextPickTime = 0.0f;
    private GameObject owner = null;

    public WeaponType WeapType
    {
        get { return weaponType; }
        set { weaponType = value; }
    }

    // Start is called before the first frame update
    void Start()
    {
        WeaponInfo info = null;

        if ((int)weaponType < Game.Instance.Weapons.Length)
            info = Game.Instance.Weapons[(int)weaponType];

        foreach (Transform child in transform)
        {
            if (info != null && child.gameObject.name == info.model)
            {
                child.gameObject.SetActive(true);

                // random color
                if (info.colors.Count > 0)
                {
                    var colorPart = child.gameObject.transform.Find(info.coloured);
                    if (colorPart != null)
                    {
                        var mat = colorPart.GetComponent<Renderer>().material;
                        mat.color = info.colors[Random.Range(0, info.colors.Count - 1)];
                    }
                }
            }
            else
            {
                Destroy(child.gameObject);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!Pickable)
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                var obj = contact.otherCollider.gameObject;

                if (obj.tag == "Avatar")
                {
                    Character player = obj.GetComponentsInParent<Character>()[0];

                    if (player.RightHand)
                    {
                        transform.parent = player.RightHand.transform;

                        transform.localPosition = new Vector3(-0.0001f, 0.000150000007f, 0);
                        transform.localRotation = new Quaternion(0, 0, 0.707106829f, 0.707106829f);

                        dropped = false;

                        player.GetComponent<Character>().RightHandWeapon = this.gameObject;
                    }
                }
            }
        }
    }

    public bool Pickable
    {
        get { return dropped == true && Time.realtimeSinceStartup > nextPickTime; }
    }

    public bool tryAttachWeapon(GameObject character)
    {
        if (Pickable)
        {
            Character player = character.GetComponentsInParent<Character>()[0];

            if (player.RightHandWeapon == null && player.RightHand)
            {
                owner = character;

                transform.parent = player.RightHand.transform;

                transform.localPosition = new Vector3(-0.0001f, 0.000150000007f, 0);
                transform.localRotation = new Quaternion(0, 0, 0.707106829f, 0.707106829f);

                dropped = false;

                player.RightHandWeapon = this.gameObject;

                GetComponent<Rigidbody>().detectCollisions = false;
                GetComponent<Rigidbody>().useGravity = false;

                pickSound.Play();

                return true;
            }
        }

        return false;
    }

    public void DetachWeapon(GameObject from)
    {
        dropped = true;
        nextPickTime = Time.realtimeSinceStartup + this.timeBeforePick;

        gameObject.transform.parent = null;

        Character player = from.GetComponent<Character>();
        player.RightHandWeapon = null;

        var rb = GetComponent<Rigidbody>();
        rb.detectCollisions = true;
        rb.useGravity = true;

        dropSound.Play();
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.tag == "Avatar")
        {
            tryAttachWeapon(col.gameObject);
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (IsPrefab || Application.isPlaying)
            return;

        //WeaponInfo info = null;
        //
        //if ((int)weaponType < Game.Instance.Weapons.Length)
        //    info = Game.Instance.Weapons[(int)weaponType];
        //
        //foreach (Transform child in transform)
        //{
        //    if (info != null && child.gameObject.name == info.model)
        //    {
        //        child.gameObject.SetActive(true);
        //    }
        //    else
        //    {
        //        child.gameObject.SetActive(false);
        //    }
        //}
    }
#endif
}

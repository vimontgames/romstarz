using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum WeaponType
{
    Racket,
    Sword
}

public class Weapon : MonoBehaviour
{
    public WeaponType weaponType = WeaponType.Racket;
    private bool available = true;

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

    void OnValidate()
    {
        WeaponInfo info = null;
        
        if ((int)weaponType < Game.Instance.Weapons.Length)
            info = Game.Instance.Weapons[(int)weaponType];

        foreach (Transform child in transform)
        {
            if (info != null && child.gameObject.name == info.model)
            {
                child.gameObject.SetActive(true);
            }
            else
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.tag == "Avatar")
        {
            Character player = col.gameObject.GetComponentsInParent<Character>()[0];

            if (available)
            {
                if (player.RightHand)
                {
                    transform.parent = player.RightHand.transform;

                    transform.localPosition = new Vector3(-0.0001f, 0.000150000007f, 0);
                    transform.localRotation = new Quaternion(0, 0, 0.707106829f, 0.707106829f);

                    available = false;
                }
            }
        }
    }
}

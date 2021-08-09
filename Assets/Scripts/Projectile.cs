using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum ProjectileType
{
    None,
    TennisBall
};

public class Projectile : MonoBehaviour
{
    public ProjectileType projectileType;
    public GameObject impact;

    private float lastDamageTime = 0.0f;
    private GameObject lastDamagedObject;
    private GameObject owner;
    
    public ProjectileInfo projInfo
    {
        get { return Game.Instance.projectiles[(int)projectileType]; }
    }

    public GameObject Owner
    {
        set { owner = value; }
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DestroyProjectile(projInfo.lifeTime));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnCollideOrTrigger(GameObject target, Vector3 pos)
    {
        Character character = target.GetComponent<Character>();

        if (character.IsBlinking)
            return;

        if (owner != target && (lastDamagedObject != target || Time.realtimeSinceStartup > lastDamageTime + 1.0f))
        {
            lastDamagedObject = target;
            lastDamageTime = Time.realtimeSinceStartup;
            int damage = character.takeHit(gameObject);

            if (impact)
            {
                GameObject imp = Instantiate(impact, pos + new Vector3(0,0,-0.25f), new Quaternion(0.5f, 0.0f, 0.0f, 0.9f));
                imp.GetComponent<Impact>().DamageAmount = damage;
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Avatar")
            OnCollideOrTrigger(collision.gameObject.transform.parent.gameObject, collision.GetContact(0).point);
    }

    void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.tag == "Avatar")
            OnCollideOrTrigger(collider.gameObject.transform.parent.gameObject, collider.bounds.center);
    }

    private IEnumerator DestroyProjectile(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}

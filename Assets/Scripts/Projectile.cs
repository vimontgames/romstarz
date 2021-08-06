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
    private float lastDamageTime = 0.0f;
    private GameObject lastDamagedObject = null;
    public GameObject owner = null;

    public ProjectileInfo projInfo
    {
        get { return Game.Instance.projectiles[(int)projectileType]; }
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

    void OnCollideOrTrigger(GameObject target)
    {
        Character character = target.GetComponent<Character>();

        if (owner != target && (lastDamagedObject != target || Time.realtimeSinceStartup > lastDamageTime + 1.0f))
        {
            lastDamagedObject = target;
            lastDamageTime = Time.realtimeSinceStartup;
            character.takeHit(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Avatar")
            OnCollideOrTrigger(collision.gameObject.transform.parent.gameObject);
    }

    void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.tag == "Avatar")
            OnCollideOrTrigger(collider.gameObject.transform.parent.gameObject);
    }

    private IEnumerator DestroyProjectile(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}

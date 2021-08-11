using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{
    public float speed = 24;
    public AudioSource sound;
    public ParticleSystem particle;
    private bool destroying = false;
    private float destroyTime;
    private Transform mesh;

    // Start is called before the first frame update
    void Start()
    {
        particle.Stop();
        mesh = transform.Find("Cylinder002");
    }

    // Update is called once per frame
    void Update()
    {
        Game game = Game.Instance;

        if (game.paused)
            return;
     
        mesh.Rotate(Vector3.up * speed * Time.deltaTime, Space.World);

        if (destroying)
        {
            if (Time.realtimeSinceStartup < destroyTime)
            {
                speed += 256.0f * Time.deltaTime;
            }
            else
            {
                Destroy(this.gameObject);
            }
        }
    }

    public bool Visible
    {
        set { mesh.GetComponent<MeshRenderer>().enabled = value; }
    }

    void OnTriggerEnter(Collider col)
    {
        if (destroying)
            return;

        if (col.gameObject.tag == "Avatar")
        {
            // Avatar collided but we want main player AI
            Character player = col.gameObject.GetComponentsInParent<Character>()[0];

            if (!player.Dead && player.CharState != Character.State.Die)
            {
                player.AddCoin(1);

                particle.Play();
                sound.Play();

                Visible = false;

                destroyTime = Time.realtimeSinceStartup + 5.0f;
                destroying = true;
            }
        }
    }
}

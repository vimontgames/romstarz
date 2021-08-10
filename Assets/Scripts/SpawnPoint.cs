using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public enum SpawnType
{
    None,
    Zombie,
    Player,
    Racket
};

public class SpawnPoint : CustomPrefab
{
    public SpawnType spawnType = SpawnType.None;
    public GameObject characters;
    public GameObject weapons;

    // Start is called before the first frame update
    void Start()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }        
    }

    // Update is called once per frame
    void Update()
    {
       
    }
    
    // Change color according to spawn type
#if UNITY_EDITOR
    void OnValidate()
    {
        if (IsPrefab || Application.isPlaying)
            return;

        if (transform.childCount > 0)
        {
            foreach (Transform child in transform)
            {
                if (child.gameObject.name != "Cube")
                {
                    UnityEditor.EditorApplication.delayCall += () =>
                    {
                        if (child != null && child.gameObject != null)
                            DestroyImmediate(child.gameObject);
                    };
                }
            }

            var mat = GetComponentInChildren<MeshRenderer>().material;

            GameObject model = null;
            Color color = new Color(0, 0, 0);
            float scale = 1.0f;
            float verticalOffset = 0.0f;

            switch (spawnType)
            {
                default:
                    break;

                case SpawnType.Player:
                    color = new Color(1, 0, 0);
                    model = characters.transform.Find("Avatar").transform.Find("Pablo").gameObject;
                    scale = 3;
                    break;

                case SpawnType.Zombie:
                    color = new Color(0, 1, 0);
                    model = characters.transform.Find("Avatar").transform.Find("Zombie").gameObject;
                    scale = 3;
                    break;

                case SpawnType.Racket:
                    color = new Color(1.0f, 0.5f, 0);
                    model = weapons.transform.Find("Racket").gameObject;
                    verticalOffset = 0.5f;
                    break;
            }

            if (mat != null)
                mat.color = color;

            if (model != null)
            {
                GameObject go = Instantiate(model, transform);
                go.transform.localPosition = new Vector3(0, verticalOffset, 0);
                go.transform.localScale = new Vector3(scale, scale, scale);
            }
        }
    }
#endif
}

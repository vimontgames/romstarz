using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public enum SpawnType
{
    None,
    Zombie,
    Player
};

public class SpawnPoint : MonoBehaviour
{
    public SpawnType spawnType = SpawnType.None;
    public GameObject characters;

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

    public bool IsPrefab
    {
        #if UNITY_EDITOR
        get { return PrefabUtility.GetPrefabParent(gameObject) == null && PrefabUtility.GetPrefabObject(gameObject) != null; }
        #else
        get { return false; }
        #endif
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
                        if (child.gameObject != null)
                            DestroyImmediate(child.gameObject);
                    };
                }
            }

            var mat = GetComponentInChildren<MeshRenderer>().material;

            GameObject model = null;
            Color color = new Color(0, 0, 0);

            switch (spawnType)
            {
                default:
                    break;

                case SpawnType.Player:
                    color = new Color(1, 0, 0);
                    model = characters.transform.Find("Avatar").transform.Find("Pablo").gameObject;
                    break;

                case SpawnType.Zombie:
                    color = new Color(0, 1, 0);
                    model = characters.transform.Find("Avatar").transform.Find("Zombie").gameObject;
                    break;
            }

            if (mat != null)
                mat.color = color;

            if (model != null)
            {
                GameObject go = Instantiate(model, transform);
                go.transform.localPosition = new Vector3(0, 0, 0);
                go.transform.localScale = new Vector3(3, 3, 3);
            }
        }
    }
#endif
}

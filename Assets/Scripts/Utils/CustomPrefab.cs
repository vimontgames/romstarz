using UnityEngine;
using UnityEditor;

public class CustomPrefab : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Returns 'true' if this is a prefab opened in editor
    public bool IsPrefab
    {
        #if UNITY_EDITOR
        get { return PrefabUtility.GetPrefabParent(gameObject) == null && PrefabUtility.GetPrefabObject(gameObject) != null; }
        #else
        get { return false; }
        #endif
    }
}

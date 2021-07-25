using UnityEngine;
using UnityEngine.UI;

public class Version : MonoBehaviour
{
    public Text text;

    public void Update()
    {
        text.text = Application.version;
    }
}
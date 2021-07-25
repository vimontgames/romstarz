using UnityEngine;
using UnityEngine.UI;

public class FPSCounter : MonoBehaviour
{
    public Text text;

    public void Update()
    {
        int fps = (int)(1f / Time.unscaledDeltaTime);
        text.text = fps.ToString() + " FPS";
    }
}
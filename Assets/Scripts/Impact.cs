using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Impact : MonoBehaviour
{
    public float lifetime = 3.0f;
    public float finalScale = 2.0f;

    private float startTime;
    private Vector3 startPos;
    private Image image;
    private Text text;
    private float damageAmount = 0;

    public float DamageAmount
    {
        set { damageAmount = value; }
    }

    // Start is called before the first frame update
    void Start()
    {
        startTime = Time.realtimeSinceStartup;
        startPos = transform.position;
        image = transform.GetComponentInChildren<Image>();
        text = transform.GetComponentInChildren<Text>();
        StartCoroutine(DestroyImpact(lifetime));
    }

    // Update is called once per frame
    void Update()
    {
        float alpha = (Time.realtimeSinceStartup - startTime) / lifetime;
        float scale = 1 + alpha;
        transform.localScale = new Vector3(scale, scale, scale);
        transform.localPosition = startPos + new Vector3(0, alpha*2.0f, 0);
        float opacity = Mathf.Pow(Mathf.Cos(alpha * Mathf.PI), 0.25f);

        if (image)
            image.color = new Color(1,1,1, opacity);

        if (text)
        {
            int damageAmountInt = (int)damageAmount;

            text.text = "-" + damageAmountInt.ToString();
            text.color = new Color(1, 1, 1, opacity);
        }
    }

    private IEnumerator DestroyImpact(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}

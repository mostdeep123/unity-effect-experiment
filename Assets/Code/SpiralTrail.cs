using UnityEngine;

public class SpiralTrail : MonoBehaviour
{
    public float radius = 1f;
    public float speed = 2f;
    public float height = 0.5f;
    private float angle;

    void Update()
    {
        angle += speed * Time.deltaTime;
        float x = Mathf.Cos(angle) * radius;
        float y = Mathf.Sin(angle) * radius;
        transform.localPosition = new Vector3(x, y, angle * height * 0.05f);
    }
}

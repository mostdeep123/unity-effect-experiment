using UnityEngine;

public class FlameController : MonoBehaviour
{
    public ParticleSystem fireTrail;
    public float stopThreshold = 0.01f; 
    public float fadeOutTime = 0.2f;    

    private Vector3 lastMousePos;
    private float timeSinceMove = 0f;
    private bool isPlaying = false;

    void Update()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        float distance = Vector3.Distance(mouseWorld, lastMousePos);

        // Kalau mouse bergerak lebih dari threshold
        if (distance > stopThreshold)
        {
            timeSinceMove = 0f;

            if (!isPlaying)
            {
                fireTrail.Play();
                isPlaying = true;
            }
        }
        else
        {
            timeSinceMove += Time.deltaTime;

            if (timeSinceMove >= fadeOutTime && isPlaying)
            {
                fireTrail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                isPlaying = false;
            }
        }

        lastMousePos = mouseWorld;
    }
}

using UnityEngine;

public class Trail : MonoBehaviour
{
    public Camera mainCam;
    public float moveSpeed = 20f;

    void Update()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f; 
        Vector3 target = mainCam.ScreenToWorldPoint(mousePos);
        transform.position = Vector3.Lerp(transform.position, target, moveSpeed * Time.deltaTime);
    }
}

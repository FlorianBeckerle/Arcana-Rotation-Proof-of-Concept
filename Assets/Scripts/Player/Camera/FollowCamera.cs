using System;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{

    [Header("Camera Settings")] 
    [SerializeField] private float cameraDistance = 10f;
    [SerializeField] private float cameraLerpSpeed = 2f;
    [SerializeField] private float maxCameraLerpSpeed = 20f;
    [SerializeField] private float maxOffset = 10f;

    [SerializeField] private GameObject playerBody;


    private void Awake()
    {
        this.transform.position = new Vector3(playerBody.transform.position.x, playerBody.transform.position.y, playerBody.transform.position.z  - cameraDistance);
    }

    void Update()
    {
        Vector3 current = this.transform.position;
        Vector3 target = new Vector3(playerBody.transform.position.x, playerBody.transform.position.y, playerBody.transform.position.z  - cameraDistance);
        
        float t = Mathf.Clamp01(Vector3.Distance(current, target) / maxOffset);

        // Ramp speed from minSpeed to maxSpeed based on t (distance fraction)
        float speed = Mathf.Lerp(cameraLerpSpeed, maxCameraLerpSpeed, Mathf.Pow(t, 2f)); // pow makes curve steeper

        // Lerp toward target using speed * deltaTime
        this.transform.position = Vector3.Lerp(current, target, speed * Time.deltaTime);
        
    }
}

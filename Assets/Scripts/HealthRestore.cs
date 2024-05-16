using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthRestore : MonoBehaviour
{
    [SerializeField]
    float RestoreAmount = 25.0f;
    float RotateSpeed = 25.0f;
    void Update()
    {
        transform.Rotate(transform.up, RotateSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        other.gameObject.SendMessage("ChangeHealth", RestoreAmount, SendMessageOptions.DontRequireReceiver);
        Destroy(gameObject);
    }
}

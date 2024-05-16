using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
    [SerializeField]
    Camera PlayerCamera;
    float AttackDamage = 20;
    AudioSource audioSource;
    float ShootDelay = 0.5f;
    float LastShootDelay = 0.0f;
    Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }
    void Update()
    {
        LastShootDelay = Mathf.Clamp(LastShootDelay - Time.deltaTime, 0, ShootDelay);

        if (Input.GetMouseButton(0) && LastShootDelay == 0)
        {
            LastShootDelay = ShootDelay;
            Shoot();
        }
    }

    void Shoot()
    {
        animator.SetTrigger("Shoot");
        audioSource.Play();
        Vector3 pos = new Vector3(PlayerCamera.pixelWidth / 2, PlayerCamera.pixelHeight / 2, 0);
        Ray ray = PlayerCamera.ScreenPointToRay(pos);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000))
        {
            if (hit.collider.GetComponent<AI_Enemy>() != null)
            {
                hit.collider.SendMessage("ChangeHealth", -AttackDamage, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

}

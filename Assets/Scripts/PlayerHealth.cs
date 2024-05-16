using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    float Health = 100;
    public SimpleHealthBar healthBar;
    void Start()
    {
        healthBar.UpdateBar(Health, 100); 
    }
    public void ChangeHealth(float Amount)
    {
        Health += Amount;

        healthBar.UpdateBar(Health, 100);
    }
}

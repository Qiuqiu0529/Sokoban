using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PossessedController : MonoBehaviour
{
    public float speed = 3.0f;

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(horizontal, 0, vertical) * speed * Time.deltaTime;
        transform.Translate(movement);
    }
}
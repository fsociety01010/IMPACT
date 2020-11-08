using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Surface : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnCollisionStay(Collision collision) {
        Debug.Log("ayo");
    }

    void OnTriggerStay(Collider other) {
        Debug.Log("ayo");
    }

    void OnTriggerEnter(Collider other){
        Debug.Log("ayo Called");
    }

    void OnCollisionEnter(Collision other){
        Debug.Log("ayo Called");
    }
}

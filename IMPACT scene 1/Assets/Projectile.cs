using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour {

    private Material trailMaterial;
    private Rigidbody rigidBody;
    
    void Start()
    {
        this.trailMaterial = new Material(Shader.Find("Specular"));
        this.trailMaterial.color = Color.red;
        this.rigidBody = GetComponent<Rigidbody>();
        this.rigidBody.velocity = new Vector3(20,0,0);
    }

    void Update()
    {

    }

    void OnTriggerStay(Collider other) {
        Vector3 contact = other.ClosestPoint(transform.position);

        /*if (Vector3.Distance(contact, transform.position) < 0.2f){
            LeaveTrail(contact, 0.1f, this.trailMaterial);
        }*/
    }

    void OnTriggerEnter(Collider other){
        Vector3 contact = other.ClosestPoint(transform.position);
        
        //LeaveTrail(contact, 0.2f, this.trailMaterial);
        
    }

    //Ca ça marche
    private void LeaveTrail(Vector3 point, float scale, Material material)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.localScale = Vector3.one * scale;
        sphere.transform.position = point;
        sphere.transform.parent = transform.parent;
        sphere.GetComponent<Collider>().enabled = false;
        sphere.GetComponent<Renderer>().material = material;
        Destroy(sphere, 10f);
    }
}

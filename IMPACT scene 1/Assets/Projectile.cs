using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour {

    private Material trailMaterial;

    // Start is called before the first frame update
    void Start()
    {
        trailMaterial = new Material(Shader.Find("Specular"));
        trailMaterial.color = Color.red;
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerStay(Collider other) {
        Debug.Log("OnTriggerStay");
        Vector3 contact = other.ClosestPoint(transform.position);

        if (Vector3.Distance(contact, transform.position) < 0.2f){
            LeaveTrail(contact, 0.1f, trailMaterial);
        }
    }

    void OnTriggerEnter(Collider other){
        Debug.Log("OnTriggerEnter");
        Vector3 contact = other.ClosestPoint(transform.position);
        
        LeaveTrail(contact, 0.2f, trailMaterial);
        
    }

    //Ca ça marche
    private void LeaveTrail(Vector3 point, float scale, Material material)
    {
        Debug.Log("HIT");
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.localScale = Vector3.one * scale;
        sphere.transform.position = point;
        sphere.transform.parent = transform.parent;
        sphere.GetComponent<Collider>().enabled = false;
        sphere.GetComponent<Renderer>().material = material;
        Destroy(sphere, 10f);
    }
}

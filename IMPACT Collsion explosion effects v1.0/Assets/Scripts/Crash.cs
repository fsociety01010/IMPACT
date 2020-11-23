using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Crash : MonoBehaviour
{
    public float objectSize = 0.2f;
    public int objectNombre = 5;

    float objectPivotDistance;
    Vector3 objectPivot;

    public float explosionForce = 100f;
    public float explosionRadius = 11f;
    public float explosionUpward = 1f;

    // Start is called before the first frame update
    void Start()
    {
        // pivot distance
        objectPivotDistance = objectSize * objectNombre / 2;

        // create pivot vector
        objectPivot = new Vector3(objectPivotDistance, objectPivotDistance, objectPivotDistance);
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "Attack"){
            explode(other);
        }
    }

    public void explode(Collider other)
    {
        // disappear the object
        gameObject.SetActive(false);
        if(objectNombre > 15)
        {
            objectNombre = 15;
        }
        for(int x = 0; x < objectNombre; x++)
        {   
            for(int y = 0 ; y < objectNombre; y++)
            {
                for (int z = 0; z < objectNombre; z++)
                {
                    createPiece(x, y, z);
                }
            }
        }

        // get position for explosion

        Vector3 explosionPos = transform.position + new Vector3(other.transform.position.x, 0f, 0f);

        // get colliders in certain pos and radius
        Collider[] colliders = Physics.OverlapSphere(explosionPos, explosionRadius);
        // add explosion force to all colliders in the overlap sphere
        foreach(Collider hit in colliders)
        {
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if(rb != null)
            {
                //add force to collider
                rb.AddExplosionForce(explosionForce, explosionPos, explosionRadius, explosionUpward); 
            }
        }
    }

    public void createPiece(int x,int y, int z)
    {
        // create piece
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);

        //set position and scale piece
        piece.transform.position = transform.position + new Vector3(objectSize * x, objectSize * y, objectSize * z) - objectPivot;
        piece.transform.localScale = new Vector3(objectSize, objectSize, objectSize);

        //add rigid body and mass
        piece.AddComponent<Rigidbody>().mass = objectSize*2;

    }
}

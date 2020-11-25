using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Surface : MonoBehaviour
{
    private static int counter = 0;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator BreakSurface(){
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();
        Mesh baseMesh = mf.mesh;

        foreach (var meshID in Enumerable.Range(0,baseMesh.subMeshCount)){
            int[] allPointIdsList = baseMesh.GetTriangles(meshID); // Tout les points, ordonnés par triangle
            for (int triangleID = 0; triangleID < allPointIdsList.Length; triangleID += 3){
                
                Mesh newMesh = new Mesh();
                
                newMesh.vertices = baseMesh.vertices.Skip(allPointIdsList[triangleID]).Take(3).Select(ver => Vector3.Scale(ver, transform.localScale)).ToArray();
                newMesh.normals = baseMesh.normals.Skip(allPointIdsList[triangleID]).Take(3).ToArray();
                newMesh.uv = baseMesh.uv.Skip(allPointIdsList[triangleID]).Take(3).ToArray();

                newMesh.triangles = new int[] { 0, 1, 2, 2, 1, 0}; //front face + back face

                GameObject GO = new GameObject("Fragment Triangle " + (triangleID / 3));
                GO.transform.position = transform.position;
                if(triangleID % 2 == 0){
                    GO.transform.rotation = Quaternion.Inverse(transform.rotation);
                }else{
                    GO.transform.rotation = transform.rotation;
                }
                GO.AddComponent<MeshRenderer>().material = mr.materials[meshID];
                GO.AddComponent<MeshFilter>().mesh = newMesh;
                GO.AddComponent<BoxCollider>();
                GO.AddComponent<Rigidbody>().AddExplosionForce(100, transform.position, 30);

                Destroy(GO, 5 + UnityEngine.Random.Range(0.0f, 5.0f));
            }
        }

        mr.enabled = false;
        Time.timeScale = 0.2f;  //pour ralentir la scène
        yield return new WaitForSeconds(0.8f);
        Time.timeScale = 1.0f;
        Destroy(gameObject);
        counter--;
    }


    /*void OnTriggerEnter(Collider other){
        if(counter == 0){
            Vector3 impactPoint = other.gameObject.GetComponent<Collider>().ClosestPointOnBounds(transform.position);
            Debug.Log(impactPoint);
            StartCoroutine(BreakSurface());
            counter++;
        }
        
    }*/
    private void OnCollisionEnter(Collision collision)
    {
        if (counter == 0)
        {
            Vector3 impactPoint = collision.GetContact(0).point;
            Debug.Log(impactPoint);
            StartCoroutine(BreakSurface());
            counter++;
        }
    }
}

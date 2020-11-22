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
                Vector3[] newVertices = new Vector3[3];
                Vector3[] newNormals = new Vector3[3];
                Vector2[] newUvs = new Vector2[3];

                Array.Copy(baseMesh.vertices, allPointIdsList[triangleID], newVertices, 0, 3);
                Array.Copy(baseMesh.normals, allPointIdsList[triangleID], newNormals, 0, 3);
                Array.Copy(baseMesh.uv, allPointIdsList[triangleID], newUvs, 0, 3);

                newMesh.vertices = newVertices;
                newMesh.normals = newNormals;
                newMesh.uv = newUvs;

                newMesh.triangles = new int[] { 0, 1, 2, 2, 1, 0 };

                GameObject GO = new GameObject("Triangle " + (triangleID / 3));
                GO.transform.position = transform.position;
                GO.transform.rotation = transform.rotation;
                GO.AddComponent<MeshRenderer>().material = mr.materials[meshID];
                GO.AddComponent<MeshFilter>().mesh = newMesh;
                GO.AddComponent<BoxCollider>();
                GO.AddComponent<Rigidbody>().AddExplosionForce(100, transform.position, 30);

                Destroy(GO, 5 + UnityEngine.Random.Range(0.0f, 5.0f));
            }
        }

        mr.enabled = false;
        //Time.timeScale = 0.2f;  //pour ralentir la scène
        yield return new WaitForSeconds(0.8f);
        //Time.timeScale = 1.0f;
        Destroy(gameObject);

    }


    void OnTriggerEnter(Collider other){
        if(counter == 0){
            Vector3 impactPoint = other.gameObject.GetComponent<Collider>().ClosestPointOnBounds(transform.position);
            Debug.Log(impactPoint);
            StartCoroutine(BreakSurface());
            counter++;
        }
        
    }
}

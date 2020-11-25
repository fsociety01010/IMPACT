using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Surface : MonoBehaviour
{
    private static int counter = 0;
    public float fractureToughness;
    public float elasticLimit;

    //rajouter et réflechir informations d'épaisseur par rapport à angle impact
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

            int[] allPointIdsList = baseMesh.GetTriangles(meshID); // Tout les points de chaque triangle
            Debug.Log(allPointIdsList.Length);
            for (int triangleID = 0; triangleID < allPointIdsList.Length; triangleID += 3){
                
                Mesh newMesh = new Mesh();
                
                newMesh.vertices = baseMesh.vertices.Skip(allPointIdsList[triangleID]).Take(3).Select(ver => Vector3.Scale(ver, transform.localScale)).ToArray();
                newMesh.normals = baseMesh.normals.Skip(allPointIdsList[triangleID]).Take(3).ToArray();
                newMesh.uv = baseMesh.uv.Skip(allPointIdsList[triangleID]).Take(3).ToArray();

                if(newMesh.vertices.Length == 3){
                    newMesh.triangles = new int[] { 0, 1, 2, 2, 1, 0}; //front face + back face
                }
                

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
        Time.timeScale = 0.3f;  //pour ralentir la scène
        yield return new WaitForSeconds(0.8f);
        Time.timeScale = 1.0f;
        Destroy(gameObject);

    }
    
    /*
    TODO remplacer calcul en fonction de x,y,z global par calcul sur x,y,z avec origine = point de contact, z = inverseNormaleFaceDeContacte
    */
    IEnumerator BreakSurfaceV2(Vector3 epiCenter_){

        float getAngleFromEpiCenter(Vector3 vertex){
            return 180f * Mathf.Atan2(vertex.z-epiCenter_.z, vertex.y-epiCenter_.y) / Mathf.PI;
        }

        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();
        Mesh baseMesh = mf.mesh;

        foreach (var meshID in Enumerable.Range(0,baseMesh.subMeshCount)){            
            //tout le calcul à faire est là en fait

            //calcul primitif, ne marchera qu'avec une surface parfaitement parallèle (sur l'axe X) en terme de nombre de point
            Vector3[] frontVertices = baseMesh.vertices.Where(v => v.x < 0).OrderBy(getAngleFromEpiCenter).Select(ver => Vector3.Scale(ver, transform.localScale)).ToArray();
            Vector3[] backVertices = baseMesh.vertices.Where(v => v.x >= 0).OrderBy(getAngleFromEpiCenter).Select(ver => Vector3.Scale(ver, transform.localScale)).ToArray();

            for (int vIndex = 0; vIndex < frontVertices.Length; vIndex += 3){
                
                Mesh newMesh = new Mesh();

                if(vIndex == 0){
                    newMesh.vertices = new Vector3[] {frontVertices[vIndex+3], frontVertices[vIndex], new Vector3(frontVertices[0].x, epiCenter_.y, epiCenter_.z), backVertices[vIndex+3], backVertices[vIndex], new Vector3(backVertices[0].x, epiCenter_.y, epiCenter_.z)};              
                }else{
                    if(vIndex+3 >= frontVertices.Length){
                        newMesh.vertices = new Vector3[] {frontVertices[vIndex], frontVertices[vIndex-3], new Vector3(frontVertices[0].x, epiCenter_.y, epiCenter_.z), backVertices[vIndex], backVertices[vIndex-3], new Vector3(backVertices[0].x, epiCenter_.y, epiCenter_.z)};
                    }else{
                        newMesh.vertices = new Vector3[] {frontVertices[vIndex+3], frontVertices[vIndex-3], new Vector3(frontVertices[0].x, epiCenter_.y, epiCenter_.z), backVertices[vIndex+3], backVertices[vIndex-3], new Vector3(backVertices[0].x, epiCenter_.y, epiCenter_.z)};
                    }
                }
                
                
                newMesh.triangles = new int[] { 0,1,2, 2,5,3, 3,0,2, 2,1,5, 5,1,4, 4,1,0, 0,3,4, 4,3,5};
                

                GameObject GO = new GameObject("Fragment Triangle " + (vIndex / 3));
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
        Time.timeScale = 1.0f;
        Destroy(gameObject);
    }
    


    void OnTriggerEnter(Collider other){
        if(counter == 0 ){
            counter++;
            Vector3 impactPoint = other.gameObject.GetComponent<Collider>().ClosestPointOnBounds(transform.position);
            Debug.Log(impactPoint);
            StartCoroutine(BreakSurfaceV2(impactPoint));
        }
        
    }
}

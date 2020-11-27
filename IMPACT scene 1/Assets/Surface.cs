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

    private float distance(Vector3 reference_,Vector3[] vertices_){
        return vertices_.Select(v => Vector3.Distance(reference_, v)).Sum();
    }

    private Matrix4x4 getLocalImpactSpace(Mesh mesh_, Vector3 projectilePosition_){
        int closestTriangle = 0;
        Vector3[] closestVertices = new Vector3[3] {mesh_.vertices[0], mesh_.vertices[1], mesh_.vertices[2]};
        float closestDistance = distance(projectilePosition_, closestVertices);
        float currentDistance;
        for (int vIndex = 3; vIndex < mesh_.vertices.Length; vIndex += 3){
            currentDistance = distance(projectilePosition_, mesh_.vertices.Skip(vIndex).Take(3).ToArray());
            if(currentDistance < closestDistance){
                closestDistance = currentDistance;
                closestTriangle = vIndex;
            }
        }
        Array.Copy(mesh_.vertices, closestTriangle, closestVertices, 0,3);
        Vector3 x = Vector3.Normalize(mesh_.normals.Skip(closestTriangle).Take(3).Aggregate(Vector3.zero, (acc,val) => acc+ val)/3);
        Vector3 y = Vector3.Normalize(closestVertices[0] - closestVertices[1]);
        Vector3 z = -Vector3.Cross(x, y);
        
        return new Matrix4x4(
            new Vector4(x.x, x.y, x.z, projectilePosition_.x),
            new Vector4(y.x, y.y, y.z, projectilePosition_.y),
            new Vector4(z.x, z.y, z.z, projectilePosition_.z),
            new Vector4(0, 0, 0, 1)
        );
    }

    /*
    TODO remplacer calcul en fonction de x,y,z global par calcul sur x,y,z avec origine = point de contact, z = inverseNormaleFaceDeContacte
    */
    IEnumerator BreakSurfaceV2(Vector3 epiCenter_){
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();
        Mesh baseMesh = mf.mesh;
        Matrix4x4 localImpactSpace = getLocalImpactSpace(baseMesh, epiCenter_);
        Vector3 localisedEpiCenter = localImpactSpace * epiCenter_;
        Debug.Log(epiCenter_);
        Debug.Log(localisedEpiCenter);

        float getAngleFromEpiCenter(Vector3 vertex){
            Vector3 translatedVertex = localImpactSpace * vertex;
            return 180f * Mathf.Atan2(translatedVertex.z-localisedEpiCenter.z, translatedVertex.y-localisedEpiCenter.y) / Mathf.PI;
        }

        foreach (var meshID in Enumerable.Range(0,baseMesh.subMeshCount)){            
            //tout le calcul à faire est là en fait

            //calcul primitif, ne marchera qu'avec une surface parfaitement parallèle en terme de nombre de point
            Vector3[] frontVertices = baseMesh.vertices.Where(v => (localImpactSpace * v).x < 0).OrderBy(getAngleFromEpiCenter).Select(ver => Vector3.Scale(ver, transform.localScale)).ToArray();
            Vector3[] backVertices = baseMesh.vertices.Where(v => (localImpactSpace * v).x >= 0).OrderBy(getAngleFromEpiCenter).Select(ver => Vector3.Scale(ver, transform.localScale)).ToArray();

            for (int vIndex = 0; vIndex < frontVertices.Length; vIndex += 3){
                
                Mesh newMesh = new Mesh();

                if(vIndex == 0){
                    newMesh.vertices = new Vector3[] {frontVertices[vIndex+3], frontVertices[vIndex], new Vector3(frontVertices[0].x, localisedEpiCenter.y, localisedEpiCenter.z), backVertices[vIndex+3], backVertices[vIndex], new Vector3(backVertices[0].x, localisedEpiCenter.y, localisedEpiCenter.z)};              
                }else{
                    if(vIndex+3 >= frontVertices.Length){
                        newMesh.vertices = new Vector3[] {frontVertices[vIndex], frontVertices[vIndex-3], new Vector3(frontVertices[0].x, localisedEpiCenter.y, localisedEpiCenter.z), backVertices[vIndex], backVertices[vIndex-3], new Vector3(backVertices[0].x, localisedEpiCenter.y, localisedEpiCenter.z)};
                    }else{
                        newMesh.vertices = new Vector3[] {frontVertices[vIndex+3], frontVertices[vIndex-3], new Vector3(frontVertices[0].x, localisedEpiCenter.y, localisedEpiCenter.z), backVertices[vIndex+3], backVertices[vIndex-3], new Vector3(backVertices[0].x, localisedEpiCenter.y, localisedEpiCenter.z)};
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
        Time.timeScale = 0.2f;  //pour ralentir la scène
        yield return new WaitForSeconds(0.8f);
        Time.timeScale = 1.0f;
        Destroy(gameObject);
    }
    


    void OnTriggerEnter(Collider other){
        if(counter == 0 ){
            counter++;
            Vector3 impactPoint = other.gameObject.GetComponent<Collider>().ClosestPointOnBounds(transform.position);
            //Debug.Log(impactPoint);
            StartCoroutine(BreakSurfaceV2(impactPoint));
        }
        
    }
}

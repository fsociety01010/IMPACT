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
    private Material trailMaterial;

    //rajouter et réflechir informations d'épaisseur par rapport à angle impact
    // Start is called before the first frame update
    void Start()
    {
        this.trailMaterial = new Material(Shader.Find("Specular"));
        this.trailMaterial.color = Color.red;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private float distance(Vector3 reference_, Vector3[] vertices_){
        float getArea(float[] abc_){
            float s = abc_.Sum() /2;
            return Mathf.Sqrt(s * abc_.Aggregate(0f, (acc,val) => acc + s-val));
        }
        float[] abc = new float[]{
            (vertices_[0] - vertices_[1]).magnitude,
            (vertices_[1] - vertices_[2]).magnitude,
            (vertices_[2] - vertices_[0]).magnitude
        };
        float baseArea = getArea(abc);

        float distanceArea = Enumerable.Range(0,3).Sum(index => getArea(
            new float[]{
                abc[index], 
                (vertices_[index] - reference_).magnitude, 
                (vertices_[(index+1)%3] - reference_).magnitude
            }
        ));

        Debug.Log(Mathf.Abs(distanceArea - baseArea));
        return Mathf.Abs(distanceArea - baseArea);

    }

    private Vector3 transfomToNewSpace(Vector3 vertex_, Matrix4x4 newSpace_){
        Vector3 fromOrigin = vertex_ - (Vector3)newSpace_.GetColumn(3);
        Vector3 newX = Vector3.Scale((Vector3)newSpace_.GetColumn(0), fromOrigin);
        Vector3 newY = Vector3.Scale((Vector3)newSpace_.GetColumn(1), fromOrigin);
        Vector3 newZ = Vector3.Scale((Vector3)newSpace_.GetColumn(2), fromOrigin);
        return newX + newY + newZ;
    }

    private Matrix4x4 getLocalImpactSpace(Mesh mesh_, Vector3 projectilePosition_){
        Matrix4x4 localToWorld = this.transform.localToWorldMatrix;
        Matrix4x4 worldToLocal = this.transform.worldToLocalMatrix;
        Vector3 epicenterInLocal = worldToLocal.MultiplyPoint3x4(projectilePosition_);

        int closestTriangle = mesh_.triangles
            .Where((_,i) => i%3==0)
            .OrderBy(pointer => this.distance(epicenterInLocal, mesh_.vertices.Skip(pointer).Take(3).ToArray()))
            .First();

        Vector3[] closestVertices = mesh_.vertices.Skip(closestTriangle).Take(3).Select(ver => localToWorld.MultiplyPoint3x4(ver)).ToArray();

        LeaveTrail(closestVertices[0], 0.5f, this.trailMaterial);
        LeaveTrail(closestVertices[1], 0.5f, this.trailMaterial);
        LeaveTrail(closestVertices[2], 0.5f, this.trailMaterial);

        //ptit trix mathématique tsais
        Vector3 y = Vector3.Normalize(closestVertices[0] - closestVertices[1]);
        Vector3 z = Vector3.Normalize(closestVertices[0] - closestVertices[2]);
        Vector3 x = Vector3.Normalize(Vector3.Cross(y, z));
        z = Vector3.Cross(y,x);
        Vector3 origin = projectilePosition_;
        
        return new Matrix4x4(
            new Vector4(x.x, x.y, x.z, 0),
            new Vector4(y.x, y.y, y.z, 0),
            new Vector4(z.x, z.y, z.z, 0),
            new Vector4(origin.x, origin.y, origin.z, 1)
        );
    }

    /*
    TODO remplacer calcul en fonction de x,y,z global par calcul sur x,y,z avec origine = point de contact, z = inverseNormaleFaceDeContacte
    */
    IEnumerator BreakSurfaceV2(Vector3 epiCenter_){
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();
        Mesh baseMesh = mf.mesh;

        Matrix4x4 localImpactSpace = this.getLocalImpactSpace(baseMesh, epiCenter_);
        Matrix4x4 worldToLocal = this.transform.worldToLocalMatrix;
        
        this.debugMatrix(localImpactSpace);

        float getAngleFromEpiCenter(Vector3 vertex){
            Vector3 translatedVertex = transfomToNewSpace(vertex, localImpactSpace);
            Vector3 translatedEpicenter = transfomToNewSpace(epiCenter_, localImpactSpace);
            return 180f * Mathf.Atan2(translatedVertex.z-translatedEpicenter.z, translatedVertex.y-translatedEpicenter.y) / Mathf.PI;
        }

        Vector3 localisedEpicenter = Vector3.Scale(worldToLocal.MultiplyPoint3x4(epiCenter_), this.transform.localScale);
        LeaveTrail(epiCenter_, 0.1f, this.trailMaterial);

        foreach (var meshID in Enumerable.Range(0,baseMesh.subMeshCount)){            
            IEnumerable<Vector3> objectVertices = baseMesh.vertices.OrderBy(getAngleFromEpiCenter);
            Vector3[] frontVertices = objectVertices.Where(v => v.x < 0).Select(ver => Vector3.Scale(ver, this.transform.localScale)).ToArray();
            Vector3[] backVertices = objectVertices.Where(v => v.x >= 0).Select(ver => Vector3.Scale(ver, this.transform.localScale)).ToArray();

            for (int vIndex = 0; vIndex < frontVertices.Length; vIndex += 3){
                
                Mesh newMesh = new Mesh();

                if(vIndex == 0){
                    newMesh.vertices = new Vector3[] {frontVertices[vIndex+3], frontVertices[vIndex], new Vector3(frontVertices[0].x, localisedEpicenter.y, localisedEpicenter.z), backVertices[vIndex+3], backVertices[vIndex], new Vector3(backVertices[0].x, localisedEpicenter.y, localisedEpicenter.z)};              
                }else{
                    if(vIndex+3 >= frontVertices.Length){
                        newMesh.vertices = new Vector3[] {frontVertices[vIndex], frontVertices[vIndex-3], new Vector3(frontVertices[0].x, localisedEpicenter.y, localisedEpicenter.z), backVertices[vIndex], backVertices[vIndex-3], new Vector3(backVertices[0].x, localisedEpicenter.y, localisedEpicenter.z)};
                    }else{
                        newMesh.vertices = new Vector3[] {frontVertices[vIndex+3], frontVertices[vIndex-3], new Vector3(frontVertices[0].x, localisedEpicenter.y, localisedEpicenter.z), backVertices[vIndex+3], backVertices[vIndex-3], new Vector3(backVertices[0].x, localisedEpicenter.y, localisedEpicenter.z)};
                    }
                }
                
                
                newMesh.triangles = new int[] { 0,1,2, 2,5,3, 3,0,2, 2,1,5, 5,1,4, 4,1,0, 0,3,4, 4,3,5};
                

                GameObject GO = new GameObject("Fragment Triangle " + (vIndex / 3));
                GO.transform.position = this.transform.position;
                GO.transform.rotation = this.transform.rotation;
                GO.AddComponent<MeshRenderer>().material = mr.materials[meshID];
                GO.AddComponent<MeshFilter>().mesh = newMesh;
                GO.AddComponent<BoxCollider>();
                GO.AddComponent<Rigidbody>().AddExplosionForce(100, this.transform.position, 30);

                Destroy(GO, 5 + UnityEngine.Random.Range(0.0f, 5.0f));
            }
        }
        mr.enabled = false;
        //Time.timeScale = 0.01f;  //pour ralentir la scène
        yield return new WaitForSeconds(0.8f);
        Time.timeScale = 1.0f;
        Destroy(gameObject);
    }

    void OnCollisionEnter(Collision col){
        if(counter == 0 ){
            counter++;
            Vector3 impactPoint = col.GetContact(0).point;
            //Debug.Log(impactPoint);
            StartCoroutine(this.BreakSurfaceV2(impactPoint));
        }
    }

    private void debugMatrix(Matrix4x4 matrix_){
        Vector3 origin = matrix_.GetColumn(3);
        Vector3 x = matrix_.GetColumn(0);
        Vector3 y = matrix_.GetColumn(1);
        Vector3 z = matrix_.GetColumn(2);
        Debug.DrawLine(
            origin,
            origin + x, Color.red, 5f
        );
        Debug.DrawLine(
            origin,
            origin + y, Color.green, 5f
        );
        Debug.DrawLine(
            origin,
            origin + z, Color.blue, 5f
        );
        //Debug.Log(x);
        //Debug.Log(y);
        //Debug.Log(z);
        //Debug.Log(origin);
    }

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

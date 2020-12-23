using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
    Classe définissant le comportement à l'impacte de l'objet impacté
*/
public class Surface : MonoBehaviour
{
    private static int counter = 0;
    public float fractureToughness;
    public float elasticLimit;
    private Material trailMaterial;

    /*
    Code Oleksandr*/
    public float explosionForce = 100f;
    public float explosionRadius = 11f;
    public float explosionUpward = 1f;

    void Start()
    {
        this.trailMaterial = new Material(Shader.Find("Specular"));
        this.trailMaterial.color = Color.red;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Fourni un nombre qui se rapproche de 0 lorsque le point reference se trouve dans le triangle créé par les 3 points de vertices_.
    /// Sert à trouver quel triangle est le plus susceptible d'intégrer un point donné.
    /// NE DOIT PAS ÊTRE UTILISE POUR TROUVER UNE DISTANCE, LE CALCUL EST MATHEMATIQUEMENT INEXACTE (pour optimisation).
    /// </summary>
    private float distanceToTriangle(Vector3 reference_, Vector3[] vertices_){
        float getArea(float[] abc_){
            float s = abc_.Sum() /2;
            return s * abc_.Aggregate(0f, (acc,val) => acc + s-val);
        }
        float[] abc = new float[]{
            (vertices_[0] - vertices_[1]).sqrMagnitude,
            (vertices_[1] - vertices_[2]).sqrMagnitude,
            (vertices_[2] - vertices_[0]).sqrMagnitude
        };
        float baseArea = getArea(abc);

        float distanceArea = Enumerable.Range(0,3).Sum(index => getArea(
            new float[]{
                abc[index], 
                (vertices_[index] - reference_).sqrMagnitude , 
                (vertices_[(index+1)%3] - reference_).sqrMagnitude
            }
        ));
        return Mathf.Abs(distanceArea - baseArea);
    }

    /// <summary>
    /// Permet d'obtenir un repère orthogonal et normal ayant pour origine le point "projectilePosition_".
    /// Le X de ce repère correspondant à la normale de surface du triangle sur lequel l'impact a été détecté.
    /// A utiliser avec transfomToNewSpace.
    /// </summary>
    private Matrix4x4 getLocalImpactSpace(Mesh mesh_, Vector3 projectilePosition_){
        Matrix4x4 localToWorld = this.transform.localToWorldMatrix;
        Matrix4x4 worldToLocal = this.transform.worldToLocalMatrix;
        Vector3 epicenterInLocal = worldToLocal.MultiplyPoint3x4(projectilePosition_);

        int closestTriangle = mesh_.triangles
            .Where((_,i) => i%3==0)
            .OrderBy(pointer => this.distanceToTriangle(epicenterInLocal, mesh_.vertices.Skip(pointer).Take(3).ToArray()))
            .First();

        Vector3[] closestVertices = mesh_.vertices.Skip(closestTriangle).Take(3).Select(ver => localToWorld.MultiplyPoint3x4(ver)).ToArray();

        //ptit trix mathématique tsais
        Vector3 y = Vector3.Normalize(closestVertices[0] - closestVertices[1]);
        Vector3 z = Vector3.Normalize(closestVertices[0] - closestVertices[2]);
        Vector3 x = Vector3.Normalize(Vector3.Cross(y, z));
        z = Vector3.Cross(y,x);
        Vector3 origin = projectilePosition_;
        
        return new Matrix4x4(
            new Vector4(x.x, y.x, z.x, 0),
            new Vector4(x.y, y.y, z.y, 0),
            new Vector4(x.z, y.z, z.z, 0),
            new Vector4(origin.x, origin.y, origin.z, 1)
        );
    }

    private void explode(Vector3 epicenter_, Rigidbody targetForExplosion_){
        targetForExplosion_.AddExplosionForce(explosionForce, epicenter_, explosionRadius, explosionUpward, ForceMode.Impulse);
    }

    /// <summary>
    /// Subdivise la mesh de base plusieurs nouvelles mesh, les mesh vont des vertex existant vers le vertex correspondant au point d'impacte (+ le même point mais du côté opposé de la surface)
    /// </summary>
    private void BreakSurface(Vector3 worldSpaceEpiCenter_, Vector3[] impactSideVertices_, Vector3[] oppositeSideVertices_, int currentMeshID_, MeshRenderer mr_){
        List<GameObject> fragments = new List<GameObject>();
        Vector3[] impactSideVertices =  impactSideVertices_.Select(ver => Vector3.Scale(ver, this.transform.localScale)).ToArray();
        Vector3[] oppositeSideVertices =  oppositeSideVertices_.Select(ver => Vector3.Scale(ver, this.transform.localScale)).ToArray();

        Vector3 impactDirectionThickness = oppositeSideVertices[0] - impactSideVertices[0];
        Vector3 localisedEpicenter = Vector3.Scale(this.transform.worldToLocalMatrix.MultiplyPoint3x4(worldSpaceEpiCenter_),  this.transform.localScale);

        Vector3 impactSideEpicenter;
        Vector3 oppositeSideEpicenter;

        print(impactDirectionThickness);

        if(Mathf.Abs(impactDirectionThickness.x) > 0){
            impactSideEpicenter = new Vector3(impactSideVertices[0].x, localisedEpicenter.y, localisedEpicenter.z);
            oppositeSideEpicenter = new Vector3(oppositeSideVertices[0].x, localisedEpicenter.y, localisedEpicenter.z);
        }else if(Mathf.Abs(impactDirectionThickness.y) > 0){
            impactSideEpicenter = new Vector3(localisedEpicenter.x, impactSideVertices[0].y, localisedEpicenter.z);
            oppositeSideEpicenter = new Vector3(localisedEpicenter.x, oppositeSideVertices[0].y, localisedEpicenter.z);
        }else{
            impactSideEpicenter = new Vector3(localisedEpicenter.x, localisedEpicenter.y, impactSideVertices[0].z);
            oppositeSideEpicenter = new Vector3(localisedEpicenter.x, localisedEpicenter.y, oppositeSideVertices[0].z);
        }
        //ATTENTION, le cas ou un cube à une épaisseur nul n'est pas supporté

        for (int vIndex = 0; vIndex < impactSideVertices_.Length; vIndex ++){
            
            Mesh newMesh = new Mesh();

            if(vIndex+1 < impactSideVertices.Length){
                newMesh.vertices = new Vector3[] {
                    impactSideVertices[vIndex+1], impactSideVertices[vIndex], impactSideEpicenter,
                    oppositeSideVertices[vIndex+1], oppositeSideVertices[vIndex], oppositeSideEpicenter
                };              
            }else{
                newMesh.vertices = new Vector3[] {
                    impactSideVertices[vIndex], impactSideVertices[0], impactSideEpicenter,
                    oppositeSideVertices[vIndex], oppositeSideVertices[0], oppositeSideEpicenter
                };
            }
            
            newMesh.triangles = new int[] { 0,1,2, 2,5,3, 3,0,2, 2,1,5, 5,1,4, 4,1,0, 0,3,4, 4,3,5};
            
            GameObject GO = new GameObject("Fragment Triangle " + vIndex);
            GO.transform.position = this.transform.position;
            GO.transform.rotation = this.transform.rotation;
            GO.AddComponent<MeshRenderer>().material = mr_.materials[currentMeshID_];
            GO.AddComponent<MeshFilter>().mesh = newMesh;
            GO.AddComponent<BoxCollider>();
            GO.AddComponent<Rigidbody>();

            fragments.Add(GO);

        }
        Destroy(gameObject);
        
        foreach (var go in fragments){
            this.explode(worldSpaceEpiCenter_, go.GetComponent<Rigidbody>());
        }
    }

    /// <summary>
    /// Génere l'ensemble des déformations liées au point d'impacte
    /// </summary>
    private IEnumerator Impact(Vector3 epiCenter_){
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();
        Mesh baseMesh = mf.mesh;

        Matrix4x4 localImpactSpace = this.getLocalImpactSpace(baseMesh, epiCenter_);
        Matrix4x4 worldToLocal = this.transform.worldToLocalMatrix;
        Matrix4x4 localToWorld = this.transform.localToWorldMatrix;

        float getAngleFromEpiCenter(Vector3 vertex){
            Vector3 translatedVertex = localImpactSpace.MultiplyPoint3x4(localToWorld.MultiplyPoint3x4(vertex));
            return 180f * Mathf.Atan2(translatedVertex.z, translatedVertex.y) / Mathf.PI;
        }

        LeaveTrail(epiCenter_, 0.1f, this.trailMaterial);

        foreach (var meshID in Enumerable.Range(0,baseMesh.subMeshCount)){

            Vector3[] objectVertices = baseMesh.vertices
                .OrderBy(v => localImpactSpace.MultiplyPoint3x4(localToWorld.MultiplyPoint3x4(v)).x)
                .ToArray();
            
            Vector3[] impactSideVertices = objectVertices
                .Take(objectVertices.Length /2)
                .OrderBy(getAngleFromEpiCenter)
                .Where((_,i) => i%3==0)
                .ToArray(); //pas scale et pas en world coordinates attention

            Vector3[] oppositeSideVertices = objectVertices
                .Skip(objectVertices.Length /2)
                .OrderBy(getAngleFromEpiCenter)
                .Where((_,i) => i%3==0)
                .ToArray(); //pas scale et pas en world coordinates attention

            //TODO créations de points à la périphérie du points d'impacte
            //TODO appliquer déformation sur ces points
            BreakSurface(epiCenter_, impactSideVertices, oppositeSideVertices, meshID, mr);
        }

        //mr.enabled = false;
        Time.timeScale = 0.01f;  //pour ralentir la scène
        yield return new WaitForSeconds(0.8f);
        Time.timeScale = 1.0f;
        
    }

    void OnCollisionEnter(Collision col){
        if(counter == 0 ){
            counter++;
            Vector3 impactPoint = col.GetContact(0).point;
            //Debug.Log(impactPoint);
            StartCoroutine(this.Impact(impactPoint));
        }
    }

    /// <summary>
    /// Fonction utilitaire qui permet d'observer un repère
    /// </summary>
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
        /*
        Debug.Log(x);
        Debug.Log(y);
        Debug.Log(z);
        Debug.Log(origin);
        */
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

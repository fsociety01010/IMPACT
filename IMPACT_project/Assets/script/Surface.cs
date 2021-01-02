﻿using System;
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

    [Range(0, 8)]
    public int addedObliqueSplit = 2; // combien de split pour chaque split fait avec les 4 sommets de base de la face impactée

    [Range(0, 8)]
    public int addedTangentSplit = 2;

    [Range(0, 1)]
    public float impactConfinementFactor = 1.0f;

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
    /// </summary>
    private Matrix4x4 getLocalImpactSpace(Mesh mesh_, Vector3 projectilePosition_){
        Matrix4x4 localToWorld = this.transform.localToWorldMatrix;

        int closestTriangle = mesh_.triangles
            .Where((_,i) => i%3==0)
            .OrderBy(pointer => this.distanceToTriangle(projectilePosition_, mesh_.vertices.Skip(pointer).Take(3).ToArray()))
            .First();

        Vector3[] closestVertices = mesh_.vertices.Skip(closestTriangle).Take(3).ToArray();

        Vector3 x;
        Vector3 y;
        Vector3 z;

        if(closestVertices[0].x == closestVertices[1].x && closestVertices[1].x == closestVertices[2].x){
            x = new Vector3(1,0,0);
            y = new Vector3(0,1,0);
            z = new Vector3(0,0,1);
        }else if(closestVertices[0].y == closestVertices[1].y && closestVertices[1].y == closestVertices[2].y){
            x = new Vector3(0,1,0);
            y = new Vector3(0,0,1);
            z = new Vector3(1,0,0);
        }else if(closestVertices[0].z == closestVertices[1].z && closestVertices[1].z == closestVertices[2].z){
            x = new Vector3(0,0,1);
            y = new Vector3(1,0,0);
            z = new Vector3(0,1,0);
        }else{
            print("La face touchée n'a pas été bien calculée ou l\'objet touché n'est pas un cube");
            x = new Vector3(1,0,0);
            y = new Vector3(0,1,0);
            z = new Vector3(0,0,1);
        }// marchera que pour des cubes

        Vector3 origin = projectilePosition_;

        this.LeaveTrail(localToWorld.MultiplyPoint3x4(mesh_.vertices[closestTriangle]), 0.1f, this.trailMaterial);
        this.LeaveTrail(localToWorld.MultiplyPoint3x4(mesh_.vertices[closestTriangle+1]), 0.1f, this.trailMaterial);
        this.LeaveTrail(localToWorld.MultiplyPoint3x4(mesh_.vertices[closestTriangle+2]), 0.1f, this.trailMaterial);
        
        return new Matrix4x4(
            new Vector4(x.x, y.x, z.x, 0),
            new Vector4(x.y, y.y, z.y, 0),
            new Vector4(x.z, y.z, z.z, 0),
            new Vector4(-origin.x, -origin.y, -origin.z, 1)
        );
    }

    private void explode(Vector3 epicenter_, Rigidbody targetForExplosion_){
        targetForExplosion_.AddExplosionForce(explosionForce, epicenter_, explosionRadius, explosionUpward, ForceMode.Impulse);
    }

    /// <summary>
    /// Renvoie le vecteur au côté opposé du point d'impact, nécéssite deux vecteur, un de la face du point d'impact, et l'autre de la face opposée, en plus du point d'impact en lui même (en coordonnées locale)
    /// Les deux premiers vecteur sont idéalement parallèle.
    /// ATTENTION, le cas ou un cube à une épaisseur nulle n'est pas supporté
    /// </summary>
    private Vector3 getOppositeSideVertex(Vector3 localSpaceEpicenter_,Vector3 anyImpactSideVertex_, Vector3 anyOppositeSideVertex_){
        Vector3 impactDirectionThickness = anyOppositeSideVertex_ - anyImpactSideVertex_;

        if(Mathf.Abs(impactDirectionThickness.x) > 0){
            return new Vector3(anyOppositeSideVertex_.x, localSpaceEpicenter_.y, localSpaceEpicenter_.z);
        }else if(Mathf.Abs(impactDirectionThickness.y) > 0){
            return new Vector3(localSpaceEpicenter_.x, anyOppositeSideVertex_.y, localSpaceEpicenter_.z);
        }else{
            return new Vector3(localSpaceEpicenter_.x, localSpaceEpicenter_.y, anyOppositeSideVertex_.z);
        }
    }

    /// <summary>
    /// Subdivise la mesh de base plusieurs nouvelles mesh, les mesh vont des vertex existant vers le vertex correspondant au point d'impacte (+ le même point mais du côté opposé de la surface)
    /// </summary>
    private void BreakSurface(Vector3 worldSpaceEpiCenter_, Vector3[] impactSideVertices_, Vector3[] oppositeSideVertices_, int currentMeshID_, MeshRenderer mr_){
        List<GameObject> fragments = new List<GameObject>();
        Vector3[] impactSideVertices =  impactSideVertices_.Select(ver => Vector3.Scale(ver, this.transform.localScale)).ToArray();
        Vector3[] oppositeSideVertices =  oppositeSideVertices_.Select(ver => Vector3.Scale(ver, this.transform.localScale)).ToArray();

        Vector3 localisedEpicenter = Vector3.Scale(this.transform.worldToLocalMatrix.MultiplyPoint3x4(worldSpaceEpiCenter_),  this.transform.localScale);
        Vector3 oppositeSideEpicenter = this.getOppositeSideVertex(localisedEpicenter, impactSideVertices[0], oppositeSideVertices[0]);

        for (int vIndex = 0; vIndex < impactSideVertices_.Length; vIndex ++){
            
            Mesh newMesh = new Mesh();

            if(vIndex+1 < impactSideVertices.Length){
                newMesh.vertices = new Vector3[] {
                    impactSideVertices[vIndex+1], impactSideVertices[vIndex], localisedEpicenter,
                    oppositeSideVertices[vIndex+1], oppositeSideVertices[vIndex], oppositeSideEpicenter
                };
            }else{
                newMesh.vertices = new Vector3[] {
                    impactSideVertices[vIndex], impactSideVertices[0], localisedEpicenter,
                    oppositeSideVertices[vIndex], oppositeSideVertices[0], oppositeSideEpicenter
                };
            }
            
            newMesh.triangles = new int[] {0,1,2, 2,5,3, 3,0,2, 2,1,5, 5,1,4, 4,1,0, 0,3,4, 4,3,5};
            
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

        Matrix4x4 worldToLocal = this.transform.worldToLocalMatrix;
        Matrix4x4 localToWorld = this.transform.localToWorldMatrix;
        Matrix4x4 localImpactSpace = this.getLocalImpactSpace(baseMesh, worldToLocal.MultiplyPoint3x4(epiCenter_));

        Vector3 localisedEpicenter = this.transform.worldToLocalMatrix.MultiplyPoint3x4(epiCenter_);

        Vector3 oppositeSideEpicenter;

        float getAngleFromEpiCenter(Vector3 vertex){
            Vector3 translatedVertex = localImpactSpace.MultiplyPoint3x4(vertex);
            return 180f * Mathf.Atan2(translatedVertex.z, translatedVertex.y) / Mathf.PI;
        }

        Vector3 NOfTheWayBetween(Vector3 start, Vector3 stop, float n){
            return n * stop + (1-n) * start;
        }

        LeaveTrail(epiCenter_, 0.1f, this.trailMaterial);

        foreach (var meshID in Enumerable.Range(0,baseMesh.subMeshCount)){

            Vector3[] objectVertices = baseMesh.vertices
                .OrderBy(v => localImpactSpace.MultiplyPoint3x4(v).x)
                .ToArray();
            
            Vector3[] existingImpactSideVertices = objectVertices
                .Take(objectVertices.Length /2)
                .OrderBy(getAngleFromEpiCenter)
                .Where((_,i) => i%3==0)
                .ToArray(); //pas scale et pas en world coordinates attention

            Vector3[] existingOppositeSideVertices = objectVertices
                .Skip(objectVertices.Length /2)
                .OrderBy(getAngleFromEpiCenter)
                .Where((_,i) => i%3==0)
                .ToArray(); //pas scale et pas en world coordinates attention

            oppositeSideEpicenter = this.getOppositeSideVertex(localisedEpicenter, existingImpactSideVertices[0], existingOppositeSideVertices[0]);

            int totalAmountOfObliqueSplits = (existingImpactSideVertices.Length) + this.addedObliqueSplit*(existingImpactSideVertices.Length);

            Vector3[] outerLayerImpactSideVertices = new Vector3[totalAmountOfObliqueSplits];
            Vector3[] outerLayerOppositeSideVertices = new Vector3[totalAmountOfObliqueSplits];

            Vector3[,] innerLayerImpactSideVertices = new Vector3[totalAmountOfObliqueSplits, this.addedTangentSplit];
            Vector3[,] innerLayerOppositeSideVertices = new Vector3[totalAmountOfObliqueSplits, this.addedTangentSplit];

            for (int i=0; i < outerLayerImpactSideVertices.Length; i ++){
                if(i % (this.addedObliqueSplit+1) == 0){ //les point à attribuer existent déjà
                    outerLayerImpactSideVertices[i] = existingImpactSideVertices[i/(this.addedObliqueSplit+1)];
                    outerLayerOppositeSideVertices[i] = existingOppositeSideVertices[i/(this.addedObliqueSplit+1)];
                }else{ //les points à attribuer doivent être extrapolé des point existants
                    float nOfTheWay = (float)(i%(this.addedObliqueSplit+1)/(float)(this.addedObliqueSplit+1));
                    int startIndex;
                    int stopIndex;

                    if((startIndex = i/(this.addedObliqueSplit+1)) < existingImpactSideVertices.Length-1){
                        stopIndex = startIndex +1;
                    }else{
                        startIndex = existingImpactSideVertices.Length-1;
                        stopIndex = 0;
                    }

                    outerLayerImpactSideVertices[i] = NOfTheWayBetween(existingImpactSideVertices[startIndex], existingImpactSideVertices[stopIndex], nOfTheWay);
                    outerLayerOppositeSideVertices[i] = NOfTheWayBetween(existingOppositeSideVertices[startIndex], existingOppositeSideVertices[stopIndex], nOfTheWay);
                }

                //pour i allant de 0 à addedTangentSplit+1, rajouter un split ...
                for(int j=1; j<this.addedTangentSplit+1; j++){
                    float n = (this.impactConfinementFactor*j)/(this.addedTangentSplit+1.0f);
                    innerLayerImpactSideVertices[i,j-1] = NOfTheWayBetween(localisedEpicenter, outerLayerImpactSideVertices[i], n);
                    innerLayerOppositeSideVertices[i, j-1] =  NOfTheWayBetween(oppositeSideEpicenter, outerLayerOppositeSideVertices[i], n);
                }
            }

            for(int x=0; x<innerLayerImpactSideVertices.GetLength(0); x++){
                for(int y=0; y<innerLayerImpactSideVertices.GetLength(1); y++){
                    this.LeaveTrail(this.transform.localToWorldMatrix.MultiplyPoint3x4(innerLayerImpactSideVertices[x,y]), 0.5f, this.trailMaterial);
                    this.LeaveTrail(this.transform.localToWorldMatrix.MultiplyPoint3x4(innerLayerOppositeSideVertices[x,y]), 0.5f, this.trailMaterial);
                    //print("Between " + localisedEpicenter + " and " + outerLayerImpactSideVertices[x] + " ==> " + innerLayerImpactSideVertices[x,y]);
                }
            }

            //TODO créations de points à la périphérie du points d'impacte
            //TODO appliquer déformation sur ces points
            BreakSurface(epiCenter_, outerLayerImpactSideVertices, outerLayerOppositeSideVertices, meshID, mr);
        }

        //mr.enabled = false;
        Time.timeScale = 0.1f;  //pour ralentir la scène
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

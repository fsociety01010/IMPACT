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

    /*
    public float fractureToughness;
    public float elasticLimit;
    */
    private Material trailMaterial;

    // Maintenant, on prends les donnees dynamiques
    private float explosionRadius = 1f;
    private float explosionUpward = 1f;    
    private float explosionForce = 1f;

    [Range(0, 8)]
    public int addedObliqueSplit = 2; // combien de split pour chaque split fait avec les 4 sommets de base de la face impactée

    [Range(0, 8)]
    public int addedTangentSplit = 2;

    [Range(0, 1)]
    public float impactConfinementFactor = 0.7f;

    public bool isImpact = true;


    //TODO à renomer
    [Range(0, 0.5f)]
    public float deformRadius = 0.2f;
    [Range(0, 10)]
    public float maxDeform = 0.1f;
    [Range(0, 1)]
    public float damageFalloff = 1;
    [Range(0, 10)]
    public float damageMultiplier = 1;
    [Range(0, 100000)]
    public float minDamage = 1;



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

        //this.LeaveTrail(localToWorld.MultiplyPoint3x4(mesh_.vertices[closestTriangle]), 0.1f, this.trailMaterial);
        //this.LeaveTrail(localToWorld.MultiplyPoint3x4(mesh_.vertices[closestTriangle+1]), 0.1f, this.trailMaterial);
        //this.LeaveTrail(localToWorld.MultiplyPoint3x4(mesh_.vertices[closestTriangle+2]), 0.1f, this.trailMaterial);
        
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
    private void calcForceExplosion(Collision colObject)
    {
        this.explosionUpward = colObject.impulse.z;
        this.explosionForce = (float)(colObject.rigidbody.mass * (Math.Abs(colObject.rigidbody.velocity.x) + Math.Abs(colObject.rigidbody.velocity.x) + Math.Abs(colObject.rigidbody.velocity.x)) * 0.3f);
        Debug.Log("Explosion Force - " + explosionForce);
        this.explosionRadius = (float)(colObject.rigidbody.mass * Math.Sqrt((Math.Abs(colObject.rigidbody.velocity.x) + Math.Abs(colObject.rigidbody.velocity.x) + Math.Abs(colObject.rigidbody.velocity.x)) * 0.2f));
        Debug.Log("Explosion Radius - " + explosionRadius);
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
    /// Sert à convertire les coordonnées des vecteurs d'impacte contenu dans une 2D array en localSpace
    /// </summary>
    private Vector3[,] convertSideToLocalSpace(Vector3[,] sideVertices_) {
        Vector3[,] result = new Vector3[sideVertices_.GetLength(0),sideVertices_.GetLength(1)];
        for (int row = 0; row < sideVertices_.GetLength(0); row++) {
            for (int col = 0; col < sideVertices_.GetLength(1); col++) {
                result[row,col] = Vector3.Scale(sideVertices_[row,col], this.transform.localScale);
            }
        }
        return result;
    }

    /// <summary>
    /// Subdivise la mesh de base plusieurs nouvelles mesh, les mesh vont des vertex existant vers le vertex correspondant au point d'impacte (+ le même point mais du côté opposé de la surface)
    /// </summary>
    private void BreakSurface(Vector3 worldSpaceEpiCenter_, Vector3[,] impactSideVertices_, Vector3[,] oppositeSideVertices_, int currentMeshID_, MeshRenderer mr_, Collision colObject){
        Destroy(this.gameObject);
        List<GameObject> fragments = new List<GameObject>();
        Vector3[,] impactSideVertices = convertSideToLocalSpace(impactSideVertices_);
        Vector3[,] oppositeSideVertices =  convertSideToLocalSpace(oppositeSideVertices_);
        
        Vector3 localisedEpicenter = Vector3.Scale(this.transform.worldToLocalMatrix.MultiplyPoint3x4(worldSpaceEpiCenter_),  this.transform.localScale);
        Vector3 oppositeSideEpicenter = this.getOppositeSideVertex(localisedEpicenter, impactSideVertices[0, this.addedTangentSplit], oppositeSideVertices[0, this.addedTangentSplit]);

        for (int obliqueIndex = 0; obliqueIndex < impactSideVertices.GetLength(0); obliqueIndex ++){
            for (int tangentIndex = 0; tangentIndex < impactSideVertices.GetLength(1); tangentIndex++){
                Mesh newMesh = new Mesh();

                if(tangentIndex == 0){
                    if(obliqueIndex+1 < impactSideVertices.GetLength(0)){
                        newMesh.vertices = new Vector3[] {
                            impactSideVertices[obliqueIndex, 0], impactSideVertices[obliqueIndex+1, 0], localisedEpicenter,
                            oppositeSideVertices[obliqueIndex, 0], oppositeSideVertices[obliqueIndex+1, 0], oppositeSideEpicenter
                        };
                    }else{
                        newMesh.vertices = new Vector3[] {
                            impactSideVertices[obliqueIndex, tangentIndex], impactSideVertices[0, tangentIndex], localisedEpicenter,
                            oppositeSideVertices[obliqueIndex, tangentIndex], oppositeSideVertices[0, tangentIndex], oppositeSideEpicenter
                        };
                    }
                    
                    newMesh.triangles = new int[] {0,1,2, 2,5,3, 3,0,2, 2,1,5, 5,1,4, 4,1,0, 0,3,4, 4,3,5};
                }else{
                    if(obliqueIndex+1 < impactSideVertices.GetLength(0)){
                        newMesh.vertices = new Vector3[] {
                            impactSideVertices[obliqueIndex, tangentIndex], impactSideVertices[obliqueIndex+1, tangentIndex], impactSideVertices[obliqueIndex+1, tangentIndex-1], impactSideVertices[obliqueIndex, tangentIndex-1],
                            oppositeSideVertices[obliqueIndex, tangentIndex], oppositeSideVertices[obliqueIndex+1, tangentIndex], oppositeSideVertices[obliqueIndex+1, tangentIndex-1], oppositeSideVertices[obliqueIndex, tangentIndex-1]
                        };
                    }else{
                        newMesh.vertices = new Vector3[] {
                            impactSideVertices[obliqueIndex, tangentIndex], impactSideVertices[0, tangentIndex], impactSideVertices[0, tangentIndex-1], impactSideVertices[obliqueIndex, tangentIndex-1],
                            oppositeSideVertices[obliqueIndex, tangentIndex], oppositeSideVertices[0, tangentIndex], oppositeSideVertices[0, tangentIndex-1], oppositeSideVertices[obliqueIndex, tangentIndex-1]
                        };
                    }
                    
                    newMesh.triangles = new int[] { 
                        0,1,2, 2,3,0,   //top
                        0,3,7, 7,4,0,   //left
                        0,4,1, 1,4,5,   //back
                        5,4,7, 7,6,5,   //bottom
                        5,6,1, 1,6,2,   //right
                        2,6,7, 7,3,2    //front
                    };
                }
                
                GameObject GO = new GameObject("Fragment Triangle " + obliqueIndex);
                GO.transform.position = this.transform.position;
                GO.transform.rotation = this.transform.rotation;
                GO.AddComponent<MeshRenderer>().material = mr_.materials[currentMeshID_];
                GO.AddComponent<MeshFilter>().mesh = newMesh;
                GO.GetComponent<MeshFilter>().mesh.RecalculateNormals();
                MeshCollider collider = GO.AddComponent<MeshCollider>();
                collider.convex = true;
                GO.AddComponent<Rigidbody>();

                fragments.Add(GO);
            }
        }
        calcForceExplosion(colObject);

        foreach (var go in fragments){
            this.explode(worldSpaceEpiCenter_, go.GetComponent<Rigidbody>());
        }
    }

    public IEnumerable<Vector3> Flatten(Vector3[,] arr2D) {
    for (int i = 0; i < arr2D.GetLength(0); i++) {
        for (int j = 0; j < arr2D.GetLength(1); j++) {
            yield return arr2D[i,j];
            }
        }
    }

    private void DeformSurface(Vector3 worldSpaceEpiCenter_, Vector3[,] impactSideVertices_, Vector3[,] oppositeSideVertices_, int currentMeshID_, MeshFilter mf_){
        Vector3[,] impactSideVertices = impactSideVertices_.Clone() as Vector3[,];
        Vector3[,] oppositeSideVertices = oppositeSideVertices_.Clone() as Vector3[,];

        Vector3 localisedEpicenter = this.transform.worldToLocalMatrix.MultiplyPoint3x4(worldSpaceEpiCenter_);
        Vector3 oppositeSideEpicenter = this.getOppositeSideVertex(localisedEpicenter, impactSideVertices[0, this.addedTangentSplit], oppositeSideVertices[0, this.addedTangentSplit]);

        Vector3 impactDirection = Vector3.Normalize(oppositeSideVertices[0,0] - impactSideVertices[0,0]);

        Vector3 getOffset(float distanceFromImpact_){
                if(distanceFromImpact_ < this.deformRadius){
                    float deformationFactor = 1 - (distanceFromImpact_/this.deformRadius) * this.damageFalloff;
                    Vector3 deformation = deformationFactor * localisedEpicenter;
                    return Vector3.Scale(impactDirection, deformation) * this.damageMultiplier;
                }else{
                    return new Vector3(0,0,0);
                }
        }
        
        for (int obliqueIndex = 0; obliqueIndex < impactSideVertices.GetLength(0); obliqueIndex ++){
            for (int tangentIndex = 0; tangentIndex < impactSideVertices.GetLength(1); tangentIndex++){
                float distanceFromImpact = Vector3.Distance(impactSideVertices[obliqueIndex, tangentIndex], localisedEpicenter);

                impactSideVertices[obliqueIndex, tangentIndex] -= getOffset(distanceFromImpact);
                oppositeSideVertices[obliqueIndex, tangentIndex] -= getOffset(distanceFromImpact);
            }
        }

        IEnumerable<int> getFrontTopRightBottomLeft(){
            int o = impactSideVertices.GetLength(0);
            int t = impactSideVertices.GetLength(1);

            //sens normal
            for (int obliqueIndex = 0; obliqueIndex < o; obliqueIndex ++){
                for (int tangentIndex = 0; tangentIndex < t; tangentIndex++){
                    int index = (obliqueIndex*t) + tangentIndex;
                    if(tangentIndex == t-1){
                        //extremité face avant ==> face avant à 4 côtés + face lattérale
                        yield return index;
                        yield return ((index+t) % (t*o));
                        yield return ((index+t) % (t*o)) + (t*o);
                        yield return ((index+t) % (t*o)) + (t*o);
                        yield return ((index) % (t*o)) + (t*o);
                        yield return index;

                        if(tangentIndex == 0 && obliqueIndex % (o/2) == 0) foreach (var item in new int[]{t*(o/2),t*(o/4),0}) yield return (index+item) % (t*o);
                        else foreach (var item in new int[]{0,-1,t-1,t-1,t,0}) yield return (index+item) % (t*o);
                    }else if(tangentIndex == 0){
                        //Intérieur face avant à trois coté, autour de l'épicentre avant
                        yield return (t*o*2);
                        yield return ((index+t) % (t*o));
                        yield return index;
                    }else{
                        //milieu face avant à 4 côtés
                        foreach (var item in new int[]{0,-1,t-1,t-1,t,0}) yield return (index+item) % (t*o);
                    }
                }
            }
        }
        
        IEnumerable<int> getBack(){
            int o = impactSideVertices.GetLength(0);
            int t = impactSideVertices.GetLength(1);

            //sens inverse
            for (int obliqueIndex = 0; obliqueIndex < o; obliqueIndex ++){
                for (int tangentIndex = 0; tangentIndex < t; tangentIndex++){
                    int index = (t*o) + (obliqueIndex*t) + tangentIndex;
                    if(tangentIndex == 0){
                        //Intérieur face avant à trois coté, autour de l'épicentre arrière
                        yield return index;
                        yield return ((index+t) % (t*o)) + (t*o);
                        yield return (t*o*2)+1;
                    }else{
                        //milieu face avant à 4 côtés
                        foreach (var item in new int[]{0,t,t-1,t-1,-1,0}) yield return ((index+item) % (t*o)) + (t*o);
                    }
                }
            }
        }

        Mesh newMesh = new Mesh();

        //localisedEpicenter et oppositeSideEpicenter sont ajouté dans le newMesh.vertices mais pas dans impactSideVertices et oppositeSideVertices pour simplifier la logique des index
        newMesh.vertices = this.Flatten(impactSideVertices)
            .Concat(this.Flatten(oppositeSideVertices))
            .Append(localisedEpicenter - getOffset(0))
            .Append(oppositeSideEpicenter - getOffset(0))
            .ToArray();

        newMesh.triangles = getFrontTopRightBottomLeft().Concat(getBack()).ToArray();
        newMesh.RecalculateNormals();
        mf_.mesh = newMesh;
    }

    /// <summary>
    /// Génere l'ensemble des déformations liées au point d'impacte
    /// </summary>
    private IEnumerator Impact(Vector3 epiCenter_, Collision colObject){
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
        
        /// Renvoie un vecteur qui se trouve n*100 % de la distance entre start et stop
        /// Si isSmoothed est true alors cette distance est attenuée de manière exponentielle inverse, c'est principalement utile pour arrondire le point d'impacte
        Vector3 NOfTheWayBetween(Vector3 start, Vector3 stop, float n, bool isSmoothed){

            /// Permet d'arrondir la disposition des vertex tangents à l'impact.
            /// Quand x est dans [0,1], x^val fait une courbe progressivement décroissante pour val allant de 0 à 1 et qui tend vers 0.
            /// L utilité etant que cela permet d attenuer la distance entre les nouveaux vertex et l'epicentre de manière non linéaraire, ce qui aura pour concéquence d'arrondire le contour de l'impacte
            float smoother(float val){
                return Mathf.Pow(0.5f, val); 
            }

            if(isSmoothed){
                float smoothingFactor = smoother(Vector3.Magnitude((n * stop + (1-n) * start) - start)); //distance epicentre ==> point
                return (n*smoothingFactor) * stop + (1-(n*smoothingFactor)) * start;
            }
            else return n * stop + (1-n) * start;
            
        }

        //LeaveTrail(epiCenter_, 0.1f, this.trailMaterial);

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

            //l'ordre des vecteurs est: dans le sens de rotation et ensuite du point d'impacte vers les extrémités (= rotate puis intérieur vers exterieur)
            Vector3[,] impactSideVertices = new Vector3[totalAmountOfObliqueSplits, this.addedTangentSplit+1]; //+1 pour les vecteurs existants sur chaque segment oblique
            Vector3[,] oppositeSideVertices = new Vector3[totalAmountOfObliqueSplits, this.addedTangentSplit+1];

            for (int i=0; i < impactSideVertices.GetLength(0); i ++){
                if(i % (this.addedObliqueSplit+1) == 0){ //les point à attribuer existent déjà
                    impactSideVertices[i, this.addedTangentSplit] = existingImpactSideVertices[i/(this.addedObliqueSplit+1)];
                    oppositeSideVertices[i, this.addedTangentSplit] = existingOppositeSideVertices[i/(this.addedObliqueSplit+1)];
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

                    impactSideVertices[i, this.addedTangentSplit] = NOfTheWayBetween(existingImpactSideVertices[startIndex], existingImpactSideVertices[stopIndex], nOfTheWay, false);
                    oppositeSideVertices[i, this.addedTangentSplit] = NOfTheWayBetween(existingOppositeSideVertices[startIndex], existingOppositeSideVertices[stopIndex], nOfTheWay, false);
                }

                //pour i allant de 0 à addedTangentSplit+1, rajouter un split ...
                for(int j=1; j<this.addedTangentSplit+1; j++){
                    float n = (this.impactConfinementFactor*j)/(this.addedTangentSplit+1.0f);
                    impactSideVertices[i,j-1] = NOfTheWayBetween(localisedEpicenter, impactSideVertices[i, this.addedTangentSplit], n, true);
                    oppositeSideVertices[i, j-1] =  NOfTheWayBetween(oppositeSideEpicenter, oppositeSideVertices[i, this.addedTangentSplit], n, true);
                }
            }

            if(this.isImpact) this.BreakSurface(epiCenter_, impactSideVertices, oppositeSideVertices, meshID, mr, colObject);
            else this.DeformSurface(epiCenter_, impactSideVertices, oppositeSideVertices, meshID, mf);
        }

        //mr.enabled = false;
        //Time.timeScale = 0.1f;  //pour ralentir la scène
        yield return new WaitForSeconds(0.8f);
        Time.timeScale = 1.0f;
        
    }

    void OnCollisionEnter(Collision col){
        if(counter == 0 ){
            counter++;
            Vector3 impactPoint = col.GetContact(0).point;
            // Debug.Log(col.rigidbody.velocity);
            StartCoroutine(this.Impact(impactPoint,col));
            counter--;
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

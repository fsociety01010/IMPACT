using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Respawns the cube
/// </summary>
public class respawnCube : MonoBehaviour
{
    //The cube's position
    float X;
    float Y;
    float Z;
    //The cube to reposition
    public GameObject cube;
    // Start is called before the first frame update
    void Start()
    {
        X=cube.transform.position.x;
        Y=cube.transform.position.y;
        Z=cube.transform.position.z;
    }
    //Replaces the cube to it's starting position
    public void reposition()
    {
        cube.transform.position = new Vector3(X,Y,Z);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

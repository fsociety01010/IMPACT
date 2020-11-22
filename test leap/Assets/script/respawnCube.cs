using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class respawnCube : MonoBehaviour
{
    float X;
    float Y;
    float Z;
    public GameObject cube;
    // Start is called before the first frame update
    void Start()
    {
        X=cube.transform.position.x;
        Y=cube.transform.position.y;
        Z=cube.transform.position.z;
    }

    public void reposition()
    {
        cube.transform.position = new Vector3(X,Y,Z);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

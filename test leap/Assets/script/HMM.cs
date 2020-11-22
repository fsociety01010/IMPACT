using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;
using UnityEngine.UI;
using System.IO;
using System;
using System.Linq;

public class HMM : MonoBehaviour
{
    // Start is called before the first frame update


    public int k = 3;

    //Le LeapMotion utilisé
    private Controller controller;

    //Vérifie si on doit faire le KNN
    private bool isTracking = true;

    //Le modèle des mains
    public HandModel left;
    public HandModel right;

    //La zone de texte à afficher
    public Text textLeft;
    public Text textRight;

    //Le chemin vers le fichier de donnée
    public string path;

    //Les données du fichier
    private List<Tuple<string, float[]>> data = new List<Tuple<string, float[]>>();
    void Start()
    {
        controller = new Controller();

        //Lit le contenu du fichier
        StreamReader read = new StreamReader(path);
        while (!read.EndOfStream){
            string[] line = read.ReadLine().Split(' ');
            string classe = line[line.Length-1];
            float[] vals = Array.ConvertAll(line.Slice(0, line.Length - 1).ToArray(),float.Parse);
            data.Add(new Tuple<string, float[]>(classe, vals));
        }
    }


    //Active le tracking
    public void enableTracking()
    {
        isTracking = true;
    }

    //Désactive le tracking
    public void disableTracking()
    {
        isTracking = false;
    }

    //calcule la classe
    string getClassOfHand(Tuple<string, float>[] kNearest)
    {
        List<string> label = new List<string>();
        List<int> vals= new List<int>();
        foreach (Tuple<string, float> tuple in kNearest)
        {
            if (label.Contains(tuple.Item1))
            {
                vals[label.IndexOf(tuple.Item1)] += 1;
            }
            else
            {
                label.Add(tuple.Item1);
                vals.Add(1);
            }
        }
        return label[vals.IndexOf(vals.Max())];
    }

    // Update is called once per frame
    void Update()
    {
        //Récupère les deux mains
        Frame frame = controller.Frame();
        List<Hand> hands = frame.Hands;
        if (hands.Count != 0 && isTracking)
        {

            //Pour les deux mains
            foreach (Hand hand in hands)
            {

                //Calcule la distance de a paume avec le bout des doigts
                List<float> distances = new List<float>();
                Vector palm = hand.PalmPosition;
                foreach (Finger finger in hand.Fingers)
                {
                    Vector posFinger = finger.TipPosition;
                    float dist = posFinger.DistanceTo(palm);
                    distances.Add(dist);
                }
                //Calcule du KNN

                //Mauvaise clasif rn
                List<Tuple<string, float>> dists = new List<Tuple<string, float>>();
                foreach(Tuple<string,float[]> tuple in data)
                {
                    float d = 0;
                    for(int i = 0; i < tuple.Item2.Length; i++)
                    {
                        d += Mathf.Pow(tuple.Item2[i] - distances[i],2);
                    }
                    dists.Add(new Tuple<string, float>(tuple.Item1, Mathf.Sqrt(d)));
                }
                dists.Sort((t1,t2)=>t1.Item2.CompareTo(t2.Item2));
                string classe = getClassOfHand(dists.Slice(0, k).ToArray());

                //Change le texte
                if (hand.IsLeft)
                {
                    textLeft.text = "Main Gauche : " +classe;
                }else if (hand.IsRight)
                {
                    textRight.text = "Main Droite : " + classe;
                }

            }
        }
    }
}


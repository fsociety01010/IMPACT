using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;
using UnityEngine.UI;
using System.IO;
using System;
using System.Linq;

public class KNN : MonoBehaviour
{


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

    //Le chemin vers le fichier de donnée de test
    public string pathTest;

    public Light sun;

    List<float> frameWindow=new List<float>();

    public int sizeOfWindow = 20;

    float rangeLeap = 430;

    //Les données du fichier
    private List<Tuple<string, float[]>> data = new List<Tuple<string, float[]>>();

    void Start()
    {
        controller = new Controller();
        readDataFile();
        //Lit le contenu du fichier
    }

    private void readDataFile()
    {
        StreamReader read = new StreamReader(path);
        while (!read.EndOfStream)
        {
            string[] line = read.ReadLine().Split(' ');
            string classe = line[line.Length - 1];
            float[] vals = Array.ConvertAll(line.Slice(0, line.Length - 1).ToArray(), float.Parse);
            data.Add(new Tuple<string, float[]>(classe, vals));
        }
    }

    //Active le tracking
    public void enableTracking()
    {
        isTracking = true;
        readDataFile();
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
    
    public void adjustSunHeight(Hand hand,string classe)
    {
        if (classe == "sun")
        {
            sun.intensity = Math.Max((float)0.4,Math.Min((float)1.5,hand.PalmPosition.y / rangeLeap));
        }
    }

    public void testKNN()
    {
        StreamReader read = new StreamReader(pathTest);
        int nbTestData = 0;
        int nbCorrectGuesses=0;
        while (!read.EndOfStream)
        {
            string[] line = read.ReadLine().Split(' ');
            string classe = line[line.Length - 1];
            float[] vals = Array.ConvertAll(line.Slice(0, line.Length - 1).ToArray(), float.Parse);

            List<Tuple<string, float>> dists = new List<Tuple<string, float>>();
            foreach (Tuple<string, float[]> tuple in data)
            {
                float d = 0;
                for (int i = 0; i < tuple.Item2.Length; i++)
                {
                    d += Mathf.Pow(tuple.Item2[i] - vals[i], 2);
                }
                dists.Add(new Tuple<string, float>(tuple.Item1, Mathf.Sqrt(d)));
            }
            dists.Sort((t1, t2) => t1.Item2.CompareTo(t2.Item2));
            string predictedClasse = getClassOfHand(dists.Slice(0, k).ToArray());
            nbTestData++;
            if (predictedClasse.Equals(classe))
            {
                nbCorrectGuesses++;
            }
        }
        print("accuracy : "+ (nbCorrectGuesses*100)/nbTestData+"%");
    }

    // Update is called once per frame
    void Update()
    {
        //Récupère les deux mains
        Frame frame = controller.Frame();
        List<Hand> hands = frame.Hands;
        if (hands.Count != 0 && isTracking && data.Count != 0)
        {

            //Pour les deux mains
            foreach (Hand hand in hands)
            {

                //Calcule la distance de la paume avec le bout des doigts
                List<float> handData = new List<float>();
                Vector palm = hand.PalmPosition;
                foreach (Finger finger in hand.Fingers)
                {
                    Vector posFinger = finger.TipPosition;
                    float dist = posFinger.DistanceTo(palm);
                    handData.Add(dist);
                }
                handData.Add(hand.Rotation.x);
                handData.Add(hand.Rotation.y);
                handData.Add(hand.Rotation.z);
                handData.Add(hand.Rotation.w);
                //Calcule du KNN

                List<Tuple<string, float>> dists = new List<Tuple<string, float>>();
                foreach(Tuple<string,float[]> tuple in data)
                {
                    float d = 0;
                    for(int i = 0; i < tuple.Item2.Length; i++)
                    {
                        d += Mathf.Pow(tuple.Item2[i] - handData[i],2);
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
                    adjustSunHeight(hand, classe);
                }
            }
        }
    }
}


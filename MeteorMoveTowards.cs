using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteorMoveTowards : MonoBehaviour
{
    public MeteorTarget myTarget;
    public LineRenderer lr;
    public AudioSource myAud;
    // Start is called before the first frame update
    void Start()
    {
        myAud = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.GM.gameState == 2)
        {
            myAud.volume = .1f;
        }
    }

    public IEnumerator MoveToPos(Vector3 pos, float ttImpact)
    {
        Vector3 curPos = transform.position;
        lr.SetPosition(0, pos); 
        
        Color newCol = Color.red;
        newCol.a = 0;
        float t = 0f;
        while(t<1)
        {
            t += Time.deltaTime / ttImpact; //percentage through the lerp.
            //transform.position = Vector3.Lerp(curPos, pos, t); //linear lerp (duh)
            transform.position = Vector3.Lerp(curPos, pos, Exponential(t)); //exponential?
            //transform.position = Vector3.Lerp(curPos, pos, t*t); //ease in


            lr.SetPosition(1, transform.position);
            newCol.a += Time.deltaTime / ttImpact;
            lr.endColor = newCol;
            lr.startColor = newCol;
            yield return null;
        }
    }

    public float Exponential(float p)
    {
        return (p == 0f) ? p : Mathf.Pow(2, 10 * (p - 1));
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteorTarget : MonoBehaviour
{
    public float timeTillDestroyed;
    public GameObject explosionPrefab;
    public Vector3 overrideRand;
    // Start is called before the first frame update
    void Start()
    {
        if(explosionPrefab==null)
        {
            explosionPrefab = GameManager.GM.genericMeteorCollPF;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.transform.CompareTag("Meteor"))
        {
            if (collision.GetComponent<MeteorMoveTowards>().myTarget == this) //some meateors were destroying other targets cause of the randomized angle.
            {
                GameManager.GM.PlayClip(GameManager.GM.meteorExplode);

                //leave a crator and definitely destroy me:
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                Destroy(collision.gameObject);
                Destroy(gameObject);
            }
        }
    }
    //private void OnCollisionEnter(Collision collision)
    //{
    //    if(collision.transform.CompareTag("Meteor"))
    //    {
    //        //leave a crator and definitely destroy me:
    //        Instantiate(explosionPrefab, transform.position, Quaternion.identity);
    //        Destroy(collision.gameObject);
    //        Destroy(gameObject);
    //    }
    //}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteorSpawner : MonoBehaviour
{

    public GameObject meteorPrefab;
    [Tooltip("Distance from the center of this GameObject that prefabs will spawn")]
    public float spawnDist;
    public List<MeteorTarget> destructables;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void MeteorsSpawn()
    {
        destructables = new List<MeteorTarget>();


        foreach (MeteorTarget mt in FindObjectsOfType<MeteorTarget>())
        {
            destructables.Add(mt); //might not even need to keep track of these
            SpawnMeteorAroundCircle(mt);
        }
        SpawnMegaMeteor();
    }
    public void SpawnMeteorAroundCircle(MeteorTarget meteor)
    {
        Vector3 spawnPos = transform.position;
        if (meteor.overrideRand!=Vector3.zero)
        {
            spawnPos = transform.position + (meteor.overrideRand * spawnDist); //spawn it opposite of the mountain, high in the sky
        }
        else
        {
            float angle = Random.Range(0, Mathf.PI * 2);

            spawnPos.x += Mathf.Cos(angle) * spawnDist;
            spawnPos.z += Mathf.Sin(angle) * spawnDist;
        }
        
        //Vector3 targetBCPos = transform.TransformPoint(meteor.GetComponent<BoxCollider>().center);
        //print(targetBCPos);

        MeteorMoveTowards mmt = Instantiate(meteorPrefab, spawnPos, Quaternion.identity).GetComponent<MeteorMoveTowards>();
        Vector3 closestPt = meteor.GetComponent<Collider>().ClosestPointOnBounds(mmt.transform.position);
        mmt.myTarget = meteor;

        mmt.StartCoroutine(mmt.MoveToPos(closestPt, meteor.timeTillDestroyed));
        //mmt.StartCoroutine(mmt.MoveToPos(meteor.transform.position, meteor.timeTillDestroyed));

    }

    public void SpawnMegaMeteor()
    {
        Vector3 spawnPos = transform.position + (transform.right * spawnDist * 2) + (transform.up*500); //spawn it opposite of the mountain, high in the sky
        MeteorMoveTowards mmt = Instantiate(meteorPrefab, spawnPos, Quaternion.identity).GetComponent<MeteorMoveTowards>();
        mmt.transform.localScale *= 10;
        Vector3 closestPt = GameManager.GM.meteorPointOfImpact.GetComponent<Collider>().ClosestPointOnBounds(mmt.transform.position);
        mmt.StartCoroutine(mmt.MoveToPos(closestPt, GameManager.GM.totalRunTime));
        mmt.tag = "MegaMeteor";
    }
}

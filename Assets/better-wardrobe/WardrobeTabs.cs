using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WardrobeTabs : MonoBehaviour
{
    public List<GameObject> OldTabs = new List<GameObject>();
    public GameObject NewTab;
    public float triggerCooldown = 1f; // adjust as needed
    private float lastTriggerTime;

    // Start is called before the first frame update
    void Start()
    {
        lastTriggerTime = -triggerCooldown; // initialize to allow triggering immediately
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnTriggerEnter(){
        if (Time.time > lastTriggerTime + triggerCooldown) {
            foreach (var obj in OldTabs) {
                obj.SetActive(false);
            }
            NewTab.SetActive(true);
            lastTriggerTime = Time.time;
        }
    }
}

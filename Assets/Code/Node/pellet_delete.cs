using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//remove pellet of nodes spawned inside its' collider2D
public class pellet_delete : MonoBehaviour{
    void Start(){
        Destroy(gameObject,2f);
    }


    void OnTriggerEnter2D(Collider2D other){
        if(other.gameObject.CompareTag("node_pellet")){
            Destroy(other.gameObject.GetComponent<SpriteRenderer>());
            other.gameObject.tag="node_normal";
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BoidsSettings", menuName = "DBG/New Boids Settings")]
public class BoidsSettings : ScriptableObject
{
    [Serializable]
    public class BoidSettings
    {
        public GameObject prefab;
        public float maxSpeed;
        public float maxForce;
        [Range(0.0f, 1.0f)] public float seekWeight;
        public float r;
        public float minScale;
        public float maxScale;
        public float spawnProb;
        public float stamina;
        public float collider_r;
        public float collider_h;
        public float walkSrcSpeed;
        public float runSrcSpeed;
        public float damage0;
        public float damage1;
    }

    public BoidSettings[] boids;
}

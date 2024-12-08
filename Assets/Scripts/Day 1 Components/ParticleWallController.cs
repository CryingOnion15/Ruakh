using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ParticleWallControllers : MonoBehaviour
{

    [SerializeField]
    protected GameObject northWall;

    [SerializeField]
    protected GameObject eastWall;

    [SerializeField]
    protected GameObject southWall;

    [SerializeField]
    protected GameObject westWall;

    public void OnMoveWalls(InputValue input) {
        
    }
}

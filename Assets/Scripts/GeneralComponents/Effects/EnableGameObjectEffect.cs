using System.Collections.Generic;
using UnityEngine;

public class EnableGameObjectEffect : Effect
{
    [SerializeField]
    protected List<GameObject> _gameObjects = new List<GameObject>();

    [SerializeField]
    protected bool _enabled = true;

    protected override void playAction() {
        _gameObjects.ForEach(x => x.SetActive(_enabled));
        Complete();
    }
}

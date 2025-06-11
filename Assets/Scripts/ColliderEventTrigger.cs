using System;
using System.Collections.Generic;
using UnityEngine;

public class ColliderEventTrigger : MonoBehaviour
{
    [SerializeField] private List<string> ColladingWith = new List<string>();
    public event Action ColliderTrigered;

    private void OnTriggerEnter(Collider other)
    {
        if (ColladingWith.Contains(other.tag))
        {
            ColliderTrigered.Invoke();
        }
    }
}

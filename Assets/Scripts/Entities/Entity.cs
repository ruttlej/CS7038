﻿using UnityEngine;

public class Entity : MonoBehaviour
{
    protected Transform entity;

    protected AudioManager audioManager;

    public Vector2 Position
    {
        get { return transform.position; }
        set { transform.position = value; }
    }

    // Use this for initialization
    protected virtual void Start()
    {
        entity = transform;

        audioManager = GameObject.FindObjectOfType<AudioManager>();
    }
}

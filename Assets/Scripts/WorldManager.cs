using UnityEngine;

public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance { get; private set; }

    [SerializeField] private bool in2DMode; // assign in Inspector or toggle in code

    public bool In2DMode => in2DMode;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // optional, if persistent across scenes
    }

    // Call this when entering/exiting 2D sections
    public void Set2DMode(bool value)
    {
        in2DMode = value;
    }
}
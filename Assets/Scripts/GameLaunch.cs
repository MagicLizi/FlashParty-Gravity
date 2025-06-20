using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameLaunch : MonoBehaviour
{
    void Awake()
    {
        Time.fixedDeltaTime  = 1/60f;
        Application.targetFrameRate = 60;
        InputManager.Instance.Startup();
    }

    void Destroy()
    {

    }
}

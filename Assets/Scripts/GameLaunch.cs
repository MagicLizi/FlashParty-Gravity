using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameLaunch : MonoBehaviour
{
    void Awake()
    {
        Time.fixedDeltaTime = 1 / 60f;
        Application.targetFrameRate = 60;
        DOVirtual.DelayedCall(0.5f, () =>
        {
            InputManager.Instance.Startup();
        });
    }

    void Destroy()
    {

    }
}

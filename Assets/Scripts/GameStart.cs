using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class GameStart : MonoBehaviour
{
    private System.IDisposable anyKeySubscription;
    // Start is called before the first frame update
    void Start()
    {
        anyKeySubscription = InputSystem.onAnyButtonPress.Call(OnAnyButtonPressed);
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnAnyButtonPressed(InputControl control)
    {
        // 我们不关心是哪个键，只要有键按下了就触发回调
        // 触发后立即取消订阅，避免重复触发
        anyKeySubscription?.Dispose();

        Debug.Log($"Any key pressed: {control.displayName}. Invoking callback.");
        // onAnyKey?.Invoke();
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameMain");
    }
}

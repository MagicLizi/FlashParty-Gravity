using System;
using System.Collections;
using System.Collections.Generic;
using Lizi.FrameWork.Util;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoSingleton<InputManager>
{
    private GameInputActions _input;

    private bool isMove = false;

    InputAction moveAction;

    void Awake()
    {
        _input = new GameInputActions();
        AddInputCallback("Move", OnMoveStart, OnMoving, OnMoveEnd);
        AddInputCallback("Jump", OnJumpStart, OnJumping, OnJumpEnd);
        AddInputCallback("Action", OnActionStart, OnAction, OnActionEnd);
        Enable(true);
    }

    public void Destroy()
    {
        RemoveInputCallback("Move", OnMoveStart, OnMoving, OnMoveEnd);
        RemoveInputCallback("Jump", OnJumpStart, OnJumping, OnJumpEnd);
        Enable(false);
    }

    public void AddInputCallback(string actionName,
    Action<InputAction.CallbackContext> startCallback = null,
    Action<InputAction.CallbackContext> performedCallback = null,
    Action<InputAction.CallbackContext> endCallback = null)
    {
        var action = _input.FindAction(actionName);
        if (actionName == "Move")
        {
            moveAction = action;
        }
        if (action != null)
        {
            if (startCallback != null)
            {
                action.started += startCallback;
            }
            if (performedCallback != null)
            {
                action.performed += performedCallback;
            }
            if (endCallback != null)
            {
                action.canceled += endCallback;
            }
        }
    }

    public void RemoveInputCallback(string actionName,
    Action<InputAction.CallbackContext> startCallback = null,
    Action<InputAction.CallbackContext> performedCallback = null,
    Action<InputAction.CallbackContext> endCallback = null)
    {
        var action = _input.FindAction(actionName);
        if (action != null)
        {
            if (startCallback != null)
            {
                action.started -= startCallback;
            }
            if (performedCallback != null)
            {
                action.performed -= performedCallback;
            }
            if (endCallback != null)
            {
                action.canceled -= endCallback;
            }
        }
    }

    public void Enable(bool enable)
    {
        if (enable)
        {
            _input.Enable();
        }
        else
        {
            _input.Disable();
        }
    }

    void Update()
    {
        if (isMove)
        {
            EventManager.Instance.TriggerEvent(EventType.Move, moveAction.ReadValue<Vector2>());
        }
    }

    void OnMoveStart(InputAction.CallbackContext value)
    {
        // Debug.Log("StartMove: " + value.ReadValue<Vector2>());
        EventManager.Instance.TriggerEvent(EventType.Move, value.ReadValue<Vector2>());
        isMove = true;
    }

    void OnMoving(InputAction.CallbackContext value)
    {
        // Debug.Log("onMove: " + value.ReadValue<Vector2>());
        EventManager.Instance.TriggerEvent(EventType.Move, value.ReadValue<Vector2>());
    }

    void OnMoveEnd(InputAction.CallbackContext value)
    {
        // Debug.Log("endMove: " + value.ReadValue<Vector2>());
        EventManager.Instance.TriggerEvent(EventType.Move, value.ReadValue<Vector2>());
        isMove = false;
    }

    void OnJumpStart(InputAction.CallbackContext value)
    {
        EventManager.Instance.TriggerEvent(EventType.Jump);
    }

    void OnJumping(InputAction.CallbackContext value)
    {
        
    }

    void OnJumpEnd(InputAction.CallbackContext value)
    {
        
    }

    void OnActionStart(InputAction.CallbackContext value)
    {
        Debug.Log("StartAction");
    }

    void OnAction(InputAction.CallbackContext value)
    {
        Debug.Log("onAction");
    }

    void OnActionEnd(InputAction.CallbackContext value)
    {
        Debug.Log("endAction");
    }   
}

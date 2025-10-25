using System;
using System.Collections;
using System.Collections.Generic;
using Lizi.FrameWork.Util;
using UnityEngine;
using UnityEngine.InputSystem;

public enum CardinalDir { None, Up, Down, Left, Right }

public enum MoveType
{
    MoveStart,
    Move,
    MoveEnd
}


public class MoveData
{
    public Vector2 moveDir;
    public MoveType moveType;
}

public class InputManager : MonoSingleton<InputManager>
{
    private GameInputActions _input;

    private bool isMove = false;

    private bool inSnapShot = false;

    private bool startSnapMove = false;

    InputAction moveAction;

    void Awake()
    {
        _input = new GameInputActions();
        AddInputCallback("Move", OnMoveStart, OnMoving, OnMoveEnd);
        AddInputCallback("Jump", OnJumpStart, OnJumping, OnJumpEnd);
        AddInputCallback("Action", OnActionStart, OnAction, OnActionEnd);
        AddInputCallback("Special", OnSpecialStart, OnSpecial, OnSpecialEnd);
        AddInputCallback("Reset", OnReset);
        AddInputCallback("Snapshot", null, OnSnapshot, null);
        AddInputCallback("CancelSnap", null, OnSnapshotEnd, null);
        //鼠标
        AddInputCallback("TriggerMoveSnap", MouseStartSnapMove, null, MouseEndSnapMove);
        AddInputCallback("SnapMove", null, OnSnapMoving, null);
        //手柄
        AddInputCallback("GamePadSnapMove", GamePadSnapMoveBegin, GamePadSnapMoving, GamePadSnapMoveEnd);
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
            MoveData moveData = new MoveData()
            {
                moveDir = moveAction.ReadValue<Vector2>(),
                moveType = MoveType.Move
            };
            EventManager.Instance.TriggerEvent(EventType.Move, moveData);
        }
    }

    void OnMoveStart(InputAction.CallbackContext value)
    {
        // Debug.Log("StartMove: " + value.ReadValue<Vector2>());
        MoveData moveData = new MoveData()
        {
            moveDir = value.ReadValue<Vector2>(),
            moveType = MoveType.MoveStart
        };
        EventManager.Instance.TriggerEvent(EventType.Move, moveData);
        isMove = true;
    }

    void OnMoving(InputAction.CallbackContext value)
    {
        // Debug.Log("onMove: " + value.ReadValue<Vector2>());
        MoveData moveData = new MoveData()
        {
            moveDir = value.ReadValue<Vector2>(),
            moveType = MoveType.Move
        };
        EventManager.Instance.TriggerEvent(EventType.Move, moveData);
    }

    void OnMoveEnd(InputAction.CallbackContext value)
    {
        // Debug.Log("endMove: " + value.ReadValue<Vector2>());
        MoveData moveData = new MoveData()
        {
            moveDir = value.ReadValue<Vector2>(),
            moveType = MoveType.MoveEnd
        };
        EventManager.Instance.TriggerEvent(EventType.Move, moveData);
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
        EventManager.Instance.TriggerEvent(EventType.Action);
    }

    void OnAction(InputAction.CallbackContext value)
    {
        // Debug.Log("onAction");
    }

    void OnActionEnd(InputAction.CallbackContext value)
    {
        // Debug.Log("endAction");
    }

    void OnSpecialStart(InputAction.CallbackContext value)
    {
        EventManager.Instance.TriggerEvent(EventType.Special);
    }

    void OnSpecial(InputAction.CallbackContext value)
    {

    }

    void OnSpecialEnd(InputAction.CallbackContext value)
    {

    }

    void OnReset(InputAction.CallbackContext value)
    {
        EventManager.Instance.TriggerEvent(EventType.Reset);
    }

    void OnSnapshot(InputAction.CallbackContext value)
    {
        inSnapShot = true;
        EventManager.Instance.TriggerEvent(EventType.Snapshot, false);
    }

    void OnSnapshotEnd(InputAction.CallbackContext value)
    {
        if (inSnapShot)
        {
            inSnapShot = false;
            EventManager.Instance.TriggerEvent(EventType.Snapshot, true);
        }
    }

    void OnSnapMoving(InputAction.CallbackContext value)
    {
        bool canMove = startSnapMove || value.control == Gamepad.current?.rightStick;
        if (canMove)
        {
            MoveData moveData = new MoveData()
            {
                moveDir = value.ReadValue<Vector2>(),
                moveType = MoveType.Move
            };
            EventManager.Instance.TriggerEvent(EventType.SnapMove, moveData);
        }
    }

    void MouseStartSnapMove(InputAction.CallbackContext value)
    {
        if (inSnapShot)
        {
            startSnapMove = true;
            EventManager.Instance.TriggerEvent(EventType.SnapMoveBegin);
        }
    }

    void MouseEndSnapMove(InputAction.CallbackContext value)
    {
        if (inSnapShot)
        {
            EventManager.Instance.TriggerEvent(EventType.SnapMoveEnd);
        }
        startSnapMove = false;
    }

    void GamePadSnapMoveBegin(InputAction.CallbackContext value)
    {
        if (inSnapShot)
        {
            startSnapMove = true;
        }
    }

    CardinalDir curGamePadMoveDir = CardinalDir.None;
    void GamePadSnapMoving(InputAction.CallbackContext value)
    {
        if (inSnapShot && startSnapMove && curGamePadMoveDir == CardinalDir.None)
        {
            curGamePadMoveDir = ToCardinal(value.ReadValue<Vector2>(), 0.01f);
            Debug.Log($"手柄移动 截图 {curGamePadMoveDir}");
            EventManager.Instance.TriggerEvent(EventType.GamePadSnapMove, curGamePadMoveDir);
        }
    }
    void GamePadSnapMoveEnd(InputAction.CallbackContext value)
    {
        startSnapMove = false;
        curGamePadMoveDir = CardinalDir.None;
    }

    public CardinalDir ToCardinal(Vector2 v, float deadzone = 0.2f, float bias = 0.05f)
    {
        // 1) 死区
        if (v.sqrMagnitude < deadzone * deadzone) return CardinalDir.None;

        // 2) 选择“占优轴”：|x| vs |y|，加一点偏置，避免刚好在对角线上抖动
        float ax = Mathf.Abs(v.x);
        float ay = Mathf.Abs(v.y);

        if (ax - ay > bias)
            return v.x > 0 ? CardinalDir.Right : CardinalDir.Left;
        if (ay - ax > bias)
            return v.y > 0 ? CardinalDir.Up : CardinalDir.Down;

        // 3) 平局时沿上次方向保持（可选）
        return CardinalDir.None; // 或者根据你的需要返回上次方向
    }
}

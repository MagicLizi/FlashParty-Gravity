using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Lizi.FrameWork.Util;

public enum EventType
{
    Move,
    Jump,
    Action,
}

public class EventManager : SingleTon<EventManager>
{
    // 事件字典，存储所有注册的事件
    private Dictionary<EventType, Action<object>> eventDictionary = new Dictionary<EventType, Action<object>>();

    /// <summary>
    /// 注册事件监听器
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="listener">监听器回调</param>
    public void AddListener(EventType eventName, Action<object> listener)
    {
        if (eventDictionary.ContainsKey(eventName))
        {
            eventDictionary[eventName] += listener;
        }
        else
        {
            eventDictionary[eventName] = listener;
        }
    }

    /// <summary>
    /// 移除事件监听器
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="listener">监听器回调</param>
    public void RemoveListener(EventType eventName, Action<object> listener)
    {
        if (eventDictionary.ContainsKey(eventName))
        {
            eventDictionary[eventName] -= listener;
            
            // 如果没有监听器了，删除这个事件
            if (eventDictionary[eventName] == null)
            {
                eventDictionary.Remove(eventName);
            }
        }
    }

    /// <summary>
    /// 触发事件
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="data">事件数据</param>
    public void TriggerEvent(EventType eventName, object data = null)
    {
        if (eventDictionary.ContainsKey(eventName))
        {
            eventDictionary[eventName]?.Invoke(data);
        }
    }

    /// <summary>
    /// 清除所有事件
    /// </summary>
    public void ClearAllEvents()
    {
        eventDictionary.Clear();
    }

    /// <summary>
    /// 清除指定事件
    /// </summary>
    /// <param name="eventName">事件名称</param>
    public void ClearEvent(EventType eventName)
    {
        if (eventDictionary.ContainsKey(eventName))
        {
            eventDictionary.Remove(eventName);
        }
    }
}

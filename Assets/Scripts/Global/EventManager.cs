using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
/*
 * EventManager
 * 事件管理器
 * 用于实现全局的观察者模式（Observer Pattern）
 * 任意类都能发送想要的委托，并被感兴趣的任意类所接收，解决耦合问题
 */

//单位受到攻击事件
public class UnitHitEvent : EventArgs
{
    public IAttackable agent;   //攻击者
    public IHitable patient;    //受击者
    public DamageScript script; //处理本次攻击的DamageScript
    public ActionData actionData;   //本次攻击的动作数据
    public HitData hitData; //本次攻击的数据
    public void SetArgs(IAttackable agent, IHitable patient, DamageScript script, ActionData adata, HitData hdata)
    {
        this.agent = agent;
        this.patient = patient;
        this.script = script;
        this.actionData = adata;
        this.hitData = hdata;
    }
}
public class EventManager : MonoBehaviour
{
    private static Dictionary<Type, SortedDictionary<int, Delegate>> listeners = new Dictionary<Type, SortedDictionary<int, Delegate>>();
    //Type: 事件类型（EventArgs）
    //int：委托优先级
    //Delegate: 该事件类型和优先级下的所有订阅委托（Action<EventArgs>）
    public static EventManager instance;
    private void Awake()
    {
        instance = this;
    }

    //订阅事件
    public void Subscribe<T>(Action<T> listener, int priority) where T : EventArgs
    {
        Type eventType = typeof(T);
        //该事件下没有委托的话先进行初始化
        if (!listeners.TryGetValue(eventType, out SortedDictionary<int, Delegate> priorityListeners))
        {
            priorityListeners = new SortedDictionary<int, Delegate>();
            listeners[eventType] = priorityListeners;
        }
        //该优先级下有委托的话进行合并
        if (priorityListeners.TryGetValue(priority, out Delegate del))
        {
            priorityListeners[priority] = Delegate.Combine(del, listener);
        }
        else
        {
            priorityListeners[priority] = listener;
        }
    }

    //取消订阅事件
    public void Unsubscribe<T>(Action<T> listener, int priority) where T : EventArgs
    {
        Type eventType = typeof(T);
        //先检测该事件有没有相应优先级下的委托
        if (listeners.TryGetValue(eventType, out SortedDictionary<int, Delegate> priorityListeners) && priorityListeners.TryGetValue(priority, out Delegate del))
        {
            Delegate currentDel = Delegate.Remove(del, listener);
            //没有委托的时候移除
            if (currentDel == null)
            {
                priorityListeners.Remove(priority);
                if (priorityListeners.Count == 0)
                {
                    listeners.Remove(eventType);
                }
            }
            else
            {
                priorityListeners[priority] = currentDel;
            }
        }
    }

    //广播事件
    public void Publish<T>(T args) where T : EventArgs
    {
        Type eventType = typeof(T);
        //没有委托就不广播
        if (listeners.TryGetValue(eventType, out SortedDictionary<int, Delegate> priorityListeners))
        {
            // 按照优先级顺序执行每个委托
            foreach (var pair in priorityListeners.OrderBy(p => p.Key))
            {
                if (pair.Value is Action<T> action)
                {
                    action.Invoke(args);
                }
                else
                {
                    throw new InvalidCastException("EventManager: 委托类型错误");
                }
            }
        }
    }
}
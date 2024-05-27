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
    private static Dictionary<Type, Delegate> listeners = new Dictionary<Type, Delegate>();
    //Type: 事件类型（EventArgs）
    //Delegate: 该事件类型和优先级下的所有订阅委托（Action<EventArgs>）
    public static EventManager instance;
    private void Awake()
    {
        instance = this;
    }

    //订阅事件
    public void Subscribe<T>(Action<T> listener) where T : EventArgs
    {
        Type eventType = typeof(T);
        //该事件下没有委托的话先进行初始化
        if (!listeners.TryGetValue(eventType, out Delegate del))
        {
            listeners[eventType] = listener;
        }
        else
        {
            listeners[eventType] = Delegate.Combine(del, listener);
        }
    }

    //取消订阅事件
    public void Unsubscribe<T>(Action<T> listener) where T : EventArgs
    {
        Type eventType = typeof(T);
        //先检测该事件有没有委托
        if (listeners.TryGetValue(eventType, out Delegate del))
        {
            Delegate currentDel = Delegate.Remove(del, listener);
            //没有委托的时候移除
            if (currentDel == null)
            {
                listeners.Remove(eventType);
            }
            else
            {
                listeners[eventType] = currentDel;
            }
        }
    }

    //广播事件
    public void Publish<T>(T args) where T : EventArgs
    {
        Type eventType = typeof(T);
        //没有委托就不广播
        if (listeners.TryGetValue(eventType, out Delegate del))
        {
            if (del is Action<T> action)
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
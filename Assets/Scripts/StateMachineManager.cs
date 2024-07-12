using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/*
 * StateMachineManager
 * 状态机管理器
 * 负责追踪动画状态机
 * 获取状态机当前状态（Tag或Name），并为状态转换及动画事件提供事件
 * 取代Unity的垃圾StateMachineBehaviour以及AnimationEvent
 */

public class StateMachineManager : MonoBehaviour
{
    public event Action<int, int> StateTransition, TagTransition;   //转换事件，参数为退出状态和进入状态
    public event Action<string[]> AnimatedEvent; //动画事件，参数为事件名称
    private Animator animator;
    private int previousState;  //记录之前的状态
    private int currentState;   //记录现在的状态
    private int previousTag;    //记录之前的Tag
    private int currentTag;     //记录现在的Tag
    private bool isInTrans; //是否处于转换中
    void Start()
    {
        animator = GetComponent<Animator>();
    }
    void Update()
    {
        // 检查是否处于过渡状态
        isInTrans = animator.IsInTransition(0);
        if (isInTrans)
        {
            // 如果处于过渡状态，获取下一个状态的Hash
            currentState = animator.GetNextAnimatorStateInfo(0).shortNameHash;
            currentTag = animator.GetNextAnimatorStateInfo(0).tagHash;
        }
        else
        {
            // 如果不在过渡状态，获取当前状态的Hash
            currentState = animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
            currentTag = animator.GetCurrentAnimatorStateInfo(0).tagHash;
        }

        // 比较当前状态与上一个状态，检查是否发生了变化
        if (currentState != previousState)
        {
            // 如果状态发生了变化，触发状态转换事件
            StateTransition?.Invoke(previousState, currentState);

            // 更新上一个状态的Hash
            previousState = currentState;
        }

        // 比较当前Tag与上一个Tag，检查是否发生了变化
        if (currentTag != previousTag)
        {
            // 如果状Tag发生了变化，触发Tag转换事件
            TagTransition?.Invoke(previousTag, currentTag);

            // 更新上一个Tag的Hash
            previousTag = currentTag;
        }
    }

    //AnimationEvent触发的通用事件，每个事件拥有唯一名称
    //parameters: 动画事件调用的事件和参数，以字符串形式传输，用分号隔开
    public void TriggerEvent(string parameters)
    {

        string[] para = parameters.Split(';');
        AnimatedEvent?.Invoke(para);
    }
    public bool IsState(string name)
    {
        return currentState == Animator.StringToHash(name);
    }
    public bool IsTag(string name)
    {
        return currentTag == Animator.StringToHash(name);
    }
    public bool Equals(string str, int hash)
    {
        return hash == Animator.StringToHash(str);
    }
}

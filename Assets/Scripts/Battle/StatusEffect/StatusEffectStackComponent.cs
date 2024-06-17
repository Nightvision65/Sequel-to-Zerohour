using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * StatusEffectStackComponent
 * 状态效果层数组件
 * 允许状态叠加层数，并给出控制层数的接口
 */
public class StatusEffectStackComponent : StatusEffectComponent
{
    private int maxStack;   //层数上限
    private int nowStack; //当前层数
    void Start()
    {
        nowStack = 1;
    }
    public int GetMaxStack() => maxStack;
    public int GetNowStack() => nowStack;
    public bool isMaxStack() => nowStack == maxStack;
    public bool hasStack() => nowStack == 0;

    //层数+1
    //返回是否达到最大层数
    public bool IncreaseStack()
    {
        nowStack = Mathf.Min(nowStack + 1, maxStack);
        return isMaxStack();
    }

    //层数-1
    //返回是否层数为空
    public bool DecreaseStack()
    {
        nowStack = Mathf.Max(nowStack - 1, 0);
        return hasStack();
    }

    //直接设置层数
    public void SetStack(int stack)
    {
        nowStack = Mathf.Clamp(nowStack, 0, maxStack);
    }
    public override void OnRelease()
    {
        nowStack = 1;
    }
}

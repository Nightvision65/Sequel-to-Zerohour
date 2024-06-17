using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * StatusEffectCore
 * 状态效果核心
 * 状态脚本的基类
 * 定义基础的状态接口（状态开始和结束）
 */
public class StatusEffectCore : MonoBehaviour, IPoolObject
{
    [SerializeField] protected string statusName; //状态效果名称（作为主键）
    public GameObject prefab { set; get; }
    protected List<StatusEffectComponent> components;  //状态组件
    private float globalTimer;    //计时器
    void Update()
    {
        //状态倒计时逻辑
        if(Global.IsTriggered(ref globalTimer))
        {
            StatusEnd();
            ObjectPoolManager.instance.Release(this);
        }
    }

    //状态被施加时调用
    protected virtual void StatusStart() { }

    //状态结束时调用
    protected virtual void StatusEnd() { }

    //状态叠加时调用
    protected virtual void StatusOverlay() { }

    //返回状态剩余时长
    public float GetCurrentDuration() => globalTimer;

    //回到对象池时重置自身状态
    public void OnRelease()
    {
        globalTimer = 0;
        foreach (StatusEffectComponent component in components)
        {
            component.OnRelease();
        }
    }
    //设置状态（通常由激活者调用）
    public void SetStatus(float time)
    {
        globalTimer = time;
        StatusStart();
    }
}

/*
 * StatusEffectComponent
 * 状态效果组件
 * 定义进阶的可组合的状态接口（如层数、更新等）
 */
public abstract class StatusEffectComponent : MonoBehaviour
{
    //回到对象池时调用（初始化状态）
    public abstract void OnRelease();
}
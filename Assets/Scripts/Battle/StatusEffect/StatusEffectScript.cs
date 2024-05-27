using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffectCoreScript : MonoBehaviour, IPoolObject
{
    [SerializeField] protected string statusName; //状态效果名称（作为主键）
    [SerializeField] protected float interval; //触发间隔时间
    public GameObject prefab { set; get; }
    private float globalTimer, updateTimer;    //计时器
    private float level;    //状态等级
    void Update()
    {
        if(interval > 0 && Global.IsTriggered(ref updateTimer))
        {
            updateTimer = interval;
            StatusUpdate();
        }
        if(Global.IsTriggered(ref globalTimer))
        {
            StatusEnd();
            ObjectPoolManager.instance.Release(this);
        }
    }
    protected void StatusStart()
    {

    }
    protected void StatusUpdate()
    {

    }

    protected void StatusEnd()
    {

    }

    //回到对象池时重置自身状态
    public void OnRelease()
    {
        updateTimer = 0;
        globalTimer = 0;
    }
    //设置状态（通常由激活者调用）
    public void SetStatus(float time)
    {
        globalTimer = time;
        StatusStart();
    }
}

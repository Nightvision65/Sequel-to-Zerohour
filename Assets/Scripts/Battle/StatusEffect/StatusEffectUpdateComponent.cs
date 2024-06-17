using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * StatusEffectUpdateComponent
 * 状态效果更新组件
 * 允许状态每间隔一段时间触发效果
 */
public class StatusEffectUpdateComponent : StatusEffectComponent
{
    private float updateTime; //触发间隔时间
    private float updateTimer = 0;    //计时器
    public event Action update; 
    void Update()
    {
        //触发计时器
        if (updateTime > 0)
        {
            updateTimer -= Time.deltaTime;
            if (updateTimer <= 0)
            {
                updateTimer += updateTime;
                update?.Invoke();
            }
        }
    }
    public void SetUpdateTime(float time) => updateTime = time;
    public float GetUpdateTime() => updateTime;
    public override void OnRelease()
    {
        updateTimer = 0;
    }
}

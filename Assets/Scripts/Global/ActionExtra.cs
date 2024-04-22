using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
 * ActionExtra
 * 额外动作附件
 * 附加在动作上的各类特殊效果
 * 全局参数类型表在Global里
 */

public enum ActionExtraType
{
    status, //状态
    critical,   //暴击修正
};//额外动作附件类型
public class ActionExtra
{
    public ActionExtraType type;  //额外动作附件类型
    [SerializeField] private Dictionary<string, object> parameters; //额外动作拥有的参数
    public void SetValue<T>(string key, T value)
    {
        if (typeof(T) == Global.instance.actionExtraParameters[key])
        {
            parameters[key] = value;
        }
        else
        {
            throw new InvalidCastException("ActionExtra：键对应的参数类型错误");
        }
    }

    public bool TryGetValue<T>(string key, out T value)
    {
        if (typeof(T) == Global.instance.actionExtraParameters[key])
        {
            if (parameters.ContainsKey(key))
            {
                value = (T)parameters[key];
                return true;
            }
        }
        else
        {
            throw new InvalidCastException("ActionExtra：键对应的参数类型错误");
        }
        value = default(T);
        return false;
    }
}

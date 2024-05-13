using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/*
 * ModifierManager
 * 修正值管理器
 * 用于存放和管理数据的修正乘数
 * 功能包括增删改查等
 */
public class ModifierManager
{
    //保存数据修正（双重字典，记录修正的来源使其便于更改）
    private Dictionary<string, Dictionary<string, float>>[] modifiers = new Dictionary<string, Dictionary<string, float>>[2];   
    private Dictionary<string, float> finalModifier = new Dictionary<string, float>();   //计算出来的最终修正(乘数)，只有修正被更改时才会更新
    public ModifierManager()
    {
        modifiers[0] = new Dictionary<string, Dictionary<string, float>>();   //加法计算的数据修正
        modifiers[1] = new Dictionary<string, Dictionary<string, float>>();   //独立乘区的数据修正
    }
    public ModifierManager(string[] names) : this()
    {
        foreach (string name in names)
        {
            InitModifier(name);
        }
    }

    //初始化修正值
    //name: 修正值名称
    public void InitModifier(string name)
    {
        modifiers[0].Add(name, new Dictionary<string, float>());
        modifiers[1].Add(name, new Dictionary<string, float>());
        finalModifier[name] = 1;
    }

    //添加或更改修正
    //name: 修正值名称
    //key: 修正值来源
    //value: 修正值数据
    //isIndie: 是否是独立乘区
    public void SetModifier(string name, string key, float value, bool isIndie = false)
    {
        modifiers[isIndie? 1 : 0][name][key] = value;
        UpdateModifier(name);
    }

    //移除修正
    //name: 修正值名称
    //key: 修正值来源
    //isIndie: 是否是独立乘区

    public void RemoveModifier(string name, string key, bool isIndie = false)
    {
        modifiers[isIndie ? 1 : 0][name].Remove(key);
        UpdateModifier(name);
    }

    //查询修正（特定）
    //name: 修正值名称
    //key: 修正值来源
    //isIndie: 是否是独立乘区
    public float GetModifier(string name, string key, bool isIndie = false)
    {
        if (modifiers[isIndie ? 1 : 0][name].ContainsKey(key))
        {
            return modifiers[isIndie ? 1 : 0][name][key];
        }
        else
        {
            return float.MinValue;
            //查询失败时返回最小float值
        }
    }

    //查询修正（最终）
    //name: 修正值名称
    public float GetModifier(string name)
    {
        return finalModifier[name];
    }

    //更新修正值
    private void UpdateModifier(string name)
    {
        float sum = 1;
        foreach (float v in modifiers[0][name].Values)
        {
            sum += v;
        }
        foreach (float v in modifiers[1][name].Values)
        {
            sum *= v;
        }
        //不会修正到负的
        sum = Math.Max(sum, 0);
        finalModifier[name] = sum;
    }
}

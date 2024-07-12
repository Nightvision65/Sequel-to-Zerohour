using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

/*
 * ModifierManager
 * 修正值管理器
 * 用于存放和管理数据的修正乘数
 * 功能包括增删改查等
 */

//修正值类型
public enum ModifierType
{
    Direct,     //直接加值
    MultiCommon,//百分比乘数（同一乘区）
    MultiIndie  //百分比乘数（不同乘区）
}
public interface IHasModifier
{
    public ModifierManager _modifier { get; set; }
}
public class ModifierManager
{
    //保存数据修正（三重字典，记录修正的来源使其便于更改）
    private Dictionary<ModifierType, Dictionary<string, Dictionary<string, float>>> modifiers = new Dictionary<ModifierType, Dictionary<string, Dictionary<string, float>>>();   
    private Dictionary<string, float> finalModifier = new Dictionary<string, float>();   //计算出来的最终修正(乘数)，只有修正被更改时才会更新
    public ModifierManager()
    {
        foreach (ModifierType type in Enum.GetValues(typeof(ModifierType)))
        {
            modifiers[type] = new Dictionary<string, Dictionary<string, float>>();
        }
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
        foreach (ModifierType type in Enum.GetValues(typeof(ModifierType)))
        {
            modifiers[type].Add(name, new Dictionary<string, float>());
        }
        finalModifier[name] = 1;
    }

    //添加或更改修正
    //name: 修正值名称
    //key: 修正值来源
    //value: 修正值数据
    //type: 修正值类型
    public void SetModifier(string name, string key, float value, ModifierType type)
    {
        modifiers[type][name][key] = value;
        UpdateModifier(name);
    }

    //移除修正
    //name: 修正值名称
    //key: 修正值来源
    //type: 修正值类型
    public void RemoveModifier(string name, string key, ModifierType type)
    {
        modifiers[type][name].Remove(key);
        UpdateModifier(name);
    }

    //查询修正（特定）
    //name: 修正值名称
    //key: 修正值来源
    //type: 修正值类型
    public float GetModifier(string name, string key, ModifierType type)
    {
        if (modifiers[type][name].ContainsKey(key))
        {
            return modifiers[type][name][key];
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
        foreach (float v in modifiers[ModifierType.MultiCommon][name].Values)
        {
            sum += v;
        }
        foreach (float v in modifiers[ModifierType.MultiIndie][name].Values)
        {
            sum *= v;
        }
        //不会修正到负的
        sum = Math.Max(sum, 0);
        finalModifier[name] = sum;
    }
}

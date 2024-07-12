using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/*
 * ActionBaseSO
 * 动作基础数据
 */

public enum ActionTag
{
    direct, //直接来源于单位
    basic,
    skill,
    melee,
    projectile,
    dot
};//动作标签
[CreateAssetMenu(fileName = "ActionBase", menuName = "ScriptableObject/ActionBase", order = 1)]
public class ActionBaseSO : SerializedScriptableObject
{
    public List<ActionTag> tags;    //动作标签
    public float damage;    //伤害值
    public float impact;    //削韧值
    public float knockback; //击退值
    public KnockType knocktype; //击退类型
    public float hitFreezeTime; //命中后顿帧时间
    public CameraShakeData hitCameraShake;  //相机震动数据
}

//角色某个动作携带的数据（包括临时的额外数据）
public class ActionData
{
    public ActionBaseSO baseData;   //基础动作数据
    public Dictionary<string, ActionExtra> extraData;  //额外动作附件
    public bool hasTag(ActionTag tag)
    {
        return baseData.tags.Contains(tag);
    }
};
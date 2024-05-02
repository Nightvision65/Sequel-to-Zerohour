using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "ActionBase", menuName = "ScriptableObject/ActionBase", order = 1)]
/*
 * ActionBaseSO
 * 动作基础数据
 */
public class ActionBaseSO : SerializedScriptableObject
{
    public List<ActionTag> tags;    //动作标签
    public float damage;    //伤害值
    public float impact;    //削韧值
    public float knockback; //击退值
    public KnockType knocktype; //击退类型
    public float moveForce; //动作导致角色位移的力
    public float hitFreezeTime; //命中后顿帧时间
    public CameraShakeData hitCameraShake;  //相机震动数据
}
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FlashData", menuName = "ScriptableObject/FlashData", order = 0)]
/*
 * FlashDataSO
 * 精灵闪烁数据
 * flashColor: 闪烁颜色
 * flashSpeed: 闪烁速度
 * flashMax: 初始闪烁强度（1为满，0为不变）
 */
public class FlashDataSO : ScriptableObject
{
    public Color flashColor;
    public float flashSpeed;
    public float flashMax;
}

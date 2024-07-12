using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * FlashDataSO
 * 精灵闪烁数据
 */
[CreateAssetMenu(fileName = "FlashData", menuName = "ScriptableObject/FlashData", order = 0)]
public class FlashDataSO : ScriptableObject
{
    public Color flashColor;    //闪烁颜色
    public float flashSpeed;    //闪烁速度
    public float flashMax;  //初始闪烁强度（1为满，0为不变）
}

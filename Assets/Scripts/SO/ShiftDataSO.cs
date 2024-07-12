using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * ShiftDataSO
 * 位移数据
 */
[CreateAssetMenu(fileName = "ShiftData", menuName = "ScriptableObject/ShiftData", order = 0)]
public class ShiftDataSO : ScriptableObject
{
    public float force; //位移施力
    public float duration;  //位移施力时长
    public float angle; //位移方向（0为正前方）
    public bool followDir;  //持续跟随朝向
}

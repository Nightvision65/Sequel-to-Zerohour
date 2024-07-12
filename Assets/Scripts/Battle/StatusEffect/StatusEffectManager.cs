using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Status
{
    none,
    burning,
    freezing,
};//状态

//状态数据
public struct StatusData
{
    public Status status;   //状态类型
    public float duration;  //状态持续时间
    public float chance;    //状态触发几率
};
public class StatusEffectManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

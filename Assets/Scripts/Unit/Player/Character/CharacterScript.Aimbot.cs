using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
/*
* CharacterScript.Aimbot
* 角色脚本.自动瞄准
* 专门负责自动瞄准功能
* 开启时，让CharacterScript的TargetPosition一直跟着某个敌人
* 关闭时，让CharacterScript的TargetPosition直接接收原始数据
*/
/*
public partial class CharacterScript : SerializedMonoBehaviour, IHitable, IAttackable
{
    public Device abMode;  //自瞄模式，键鼠还是手柄模式
    [SerializeField] protected bool abOn;  //是否开启自瞄
    [SerializeField] protected Transform abTarget; //自瞄目标
    
    //设置目标位置
    protected void SetTargetPosition()
    {
        if (abOn && abTarget)
        {
            targetPosition = abTarget.position;
        }
        else
        {
            if (abMode == Device.keyboard)
            {
                targetPosition = deviceLockInput;
            }
            else
            {
                if (deviceLockInput == Vector2.zero)
                {
                    targetPosition = (Vector2)mTransform.position + moveDirection.normalized;
                }
                else
                {
                    targetPosition = (Vector2)mTransform.position + deviceLockInput.normalized;
                }
            }
        }
    }

    //自动瞄准最近敌人
    protected void Aimbot_SetClosetTarget()
    {
        Transform closestTarget = null;
        float closestDistance = Mathf.Infinity;
        GameObject[] units = GameObject.FindGameObjectsWithTag("BattleUnit");
        foreach (GameObject unit in units)
        {
            TeamScript unitTeam = unit.GetComponentInParent<TeamScript>();
            if (!mTeam.IsSameTeam(unitTeam))
            {
                float distance = Vector2.Distance(mTransform.position, unit.transform.position);
                if (distance < closestDistance)
                {
                    closestTarget = unit.transform;
                    closestDistance = distance;
                }
            }
        }
        if (closestTarget)
        {
            abTarget = closestTarget;
            SetTargetPosition();
        }
    }

    //自动精确瞄准敌人
    protected void Aimbot_SetAccurateTarget()
    {
        if (abMode == Device.keyboard)
        {
            //键鼠状态下，精确瞄准最接近鼠标位置的敌人
            Transform closestTarget = null;
            float closestDistance = Mathf.Infinity;
            GameObject[] units = GameObject.FindGameObjectsWithTag("BattleUnit");
            foreach (GameObject unit in units)
            {
                TeamScript unitTeam = unit.GetComponentInParent<TeamScript>();
                if (!mTeam.IsSameTeam(unitTeam))
                {
                    float distance = Vector2.Distance(deviceLockInput, unit.transform.position);
                    if (distance < closestDistance)
                    {
                        closestTarget = unit.transform;
                        closestDistance = distance;
                    }
                }
            }
            if (closestTarget)
            {
                abTarget = closestTarget;
                SetTargetPosition();
            }
        }
        else
        {
            //手柄状态下，精确瞄准最接近右摇杆方向的敌人
            if (deviceLockInput != Vector2.zero)
            {
                Transform closestTarget = null;
                float closestAngle = 360f;
                GameObject[] units = GameObject.FindGameObjectsWithTag("BattleUnit");
                foreach (GameObject unit in units)
                {
                    TeamScript unitTeam = unit.GetComponentInParent<TeamScript>();
                    if (!mTeam.IsSameTeam(unitTeam))
                    {
                        float angle = Vector2.Angle(deviceLockInput, unit.transform.position - mTransform.position);
                        if (angle < closestAngle)
                        {
                            closestTarget = unit.transform;
                            closestAngle = angle;
                        }
                    }
                }
                if (closestTarget)
                {
                    abTarget = closestTarget;
                    SetTargetPosition();
                }
            }
            else
            {
                //未使用精确瞄准时，自动瞄准最近敌人
                Aimbot_SetClosetTarget();
            }
        }
    }
}
*/

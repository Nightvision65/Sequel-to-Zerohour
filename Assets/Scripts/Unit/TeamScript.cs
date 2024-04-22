using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
 * TeamScript
 * 队伍脚本
 * 挂载在所有战斗单位(BattleUnit)上，负责控制战斗单位的所属队伍
 * 0: 无队伍，即与所有其他单位为敌
 * 1: 玩家队伍
 * 2: 玉势力敌人队伍
 * 3: 生物势力敌人队伍
 */

public class TeamScript : MonoBehaviour
{
    public int team;    //所属队伍
    public List<int> aggro;  //会主动攻击aggro中的队伍
    //判断对方是否与自己处于相同队伍
    public bool IsSameTeam(TeamScript target)
    {
        return target.team == team && team != 0 || target == this;
    }

    //判断对方是否与自己为敌
    public bool IsEnemy(TeamScript target)
    {
        return !IsSameTeam(target) && aggro.Contains(target.team);
    }

    //设置对指定队伍的仇恨
    public void SetAggro(int team, bool hate)
    {
        if (hate)
        {
            if (!aggro.Contains(team)){
                aggro.Add(team);
            }
        }
        else
        {
            if (aggro.Contains(team))
            {
                aggro.Remove(team);
            }
        }
    }
}

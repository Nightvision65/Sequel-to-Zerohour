using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
 * TestEnemyScript
 * 测试用敌人脚本
 * 弱生物测试
 */

public class TestEnemyScript : EnemyScript
{
    
    void FixedUpdate()
    {
        if (enableAI)
        {
            TestEnemyAI();
        }
    }
    //弱生物AI（临时）
    void TestEnemyAI()
    {
        AI_Chase(AI_Find_Target_Closest(100));
    }
    public void StaggerEnd()
    {

    }
}

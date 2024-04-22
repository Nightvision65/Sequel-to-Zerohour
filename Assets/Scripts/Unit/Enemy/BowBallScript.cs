using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
/*
 * SwordBallScript
 * 弓玉脚本
 * 敌人脚本的子类
 * 负责弓玉敌人的相关实现
 */

public class BowBallScript : EnemyBallScript
{
    [SerializeField] private float detectRange;   //察觉距离
    [SerializeField] private float attackRange;   //攻击距离
    [SerializeField] private float closeRange;   //过近距离
    [SerializeField] private float attackCDMin, attackCDMax;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform arrowTransform;
    [SerializeField] private float arrowForce;   //过近距离
    private float attackCDTimer;
    private bool standoffDir;
    
    new void Start()
    {
        base.Start();
        attackCDTimer = Random.Range(attackCDMin, attackCDMax);
    }
    new void Update()
    {
        base.Update();
        if (attackCDTimer > 0) attackCDTimer -= Time.deltaTime;
    }
    void FixedUpdate()
    {
        if (enableAI)
        {
            BowBallAI();
        }
        else
        {
            faceState = FaceState.lockedDir;
        }
    }
    protected override void OnAnimStateChange(int exitState, int enterState)
    {
        //攻击和破韧结束后，重置攻击计时器
        if (mSMM.Equals("AttackEnd", exitState) || mSMM.Equals("Stagger", exitState))
        {
            attackCDTimer = Random.Range(attackCDMin, attackCDMax);
        }
        //进入破韧时触发
        if (mSMM.Equals("Stagger", enterState) || mSMM.Equals("Death", enterState))
        {
            ball.CharacterFace(-knockDir, true);
            faceState = FaceState.lockedDir;
            standoffDir = Random.value > 0.5f;
            target = null;  //丢失目标
        }
    }

    void BowBallAI()
    {
        //直接使用动画状态作为状态机
        if (mSMM.IsState("Idle"))
        {
            if (!target)
            {
                //寻找目标
                target = AI_Find_Target_Closest(detectRange);
                faceState = FaceState.moveDir;
            }
            else
            {
                //追逐目标
                if (AI_Get_Target_Distance(target) >= attackRange)
                {
                    faceState = FaceState.moveDir;
                    AI_Chase(target);
                }
                else
                {
                    faceState = FaceState.targetDir;
                    if (attackCDTimer <= 0)
                    {
                        //攻击目标
                        attackCDTimer = Random.Range(attackCDMin, attackCDMax);
                        standoffDir = Random.value > 0.5f;
                        AI_Attack();
                    }
                    else
                    {
                        //与目标对峙/太近了离远点
                        if (AI_Get_Target_Distance(target) >= closeRange)
                        {
                            AI_Standoff(target, 0.5f, Global.ToTernary(standoffDir));
                        }
                        else
                        {
                            AI_Standoff(target, 0.5f, Ternary.zero);
                        }
                    }
                }
            }
        }
    }
    void ArrowShot()
    {
        GameObject arrow = ObjectPoolManager.instance.Get(arrowPrefab);
        ProjectileScript arrowScript = arrow.GetComponent<ProjectileScript>();
        arrowScript.SetTransform(arrowTransform);
        arrowScript.SetDScriptData(this, "Arrow");
        arrowScript.LaunchForward(arrowForce);
    }
}

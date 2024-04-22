using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Rendering.PostProcessing;
using UnityEditor.PackageManager;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.InputSystem.LowLevel;
using static UnityEngine.Rendering.DebugUI;
using UnityEngine.Rendering;
/*
 * CharacterScript
 * 角色脚本
 * 角色的父类，负责所有角色都能做的事情
 * 负责保存角色的基本数据，以及提供数据接口
 * 能够对用户输入信息进行处理
 */

public class CharacterScript : SerializedMonoBehaviour, IHitable, IAttackable
{
    public BallScript ball;
    [SerializeField] protected Rigidbody2D activeRbody; //用于记录主动位移产生动量的Rigidbody，以此将主动和被动位移产生的动量区分开
    TrailRenderer mTrail;
    protected FaceState faceState = FaceState.moveDir;    //朝向状态
    public Vector2 moveDirection; //角色移动方向
    [SerializeField] protected Vector2 targetPosition;  //锁定的目标位置
    public Vector2 deviceLockInput;  //输入设备的指针所锁定的Vector2数据，由PlayerController不断更新
    public Dictionary<Attribute, int> chAttribute;    //保存角色的属性
    public Dictionary<string, ActionData> chActionData;    //保存关于角色的动作数据(比如技能动作值、削韧等)
    public Dictionary<string, float> chData;//保存关于角色的顶层数据(每次基础数据被修改时更新)
    public Dictionary<Modifier, Dictionary<string, float>> chModifiersAdd;   //保存加法运算的角色数据修正（双重字典，记录修正的来源使其便于更改）
    public Dictionary<Modifier, Dictionary<string, float>> chModifiersMul;   //保存独立乘区的角色数据修正（双重字典，记录修正的来源使其便于更改）
    protected Dictionary<Modifier,float> finalModifier = new Dictionary<Modifier, float>();   //计算出来的最终修正(乘数)
    protected float nowHealth;    //现在生命值
    protected bool isMoving;    //角色正在移动
    [SerializeField] protected float dodgeSpeed;    //闪避速度(乘数)
    [SerializeField] protected float dodgeCD; //闪避冷却时间
    protected float dodgeTimer;
    protected bool isDodging;   //角色正在闪避
    protected Coroutine isHitFreezing;  //角色正在顿帧（存放顿帧的协程）
    protected float savedAnimSpeed; //临时存放的动画速度，用来顿帧
    protected Transform mTransform;
    protected Rigidbody2D mRbody;
    protected Collider2D mCollider;
    protected Animator mAnim;
    protected FlashEffectScript mFlashEffect;
    protected StateMachineManager mSMM;
    protected TeamScript mTeam;
    public float tempAttackSpeed;
    protected void Start()
    {
        mRbody = GetComponent<Rigidbody2D>();
        mCollider = GetComponent<Collider2D>();
        mAnim = GetComponent<Animator>();
        mTrail = GetComponent<TrailRenderer>();
        mTransform = transform;
        ball = GetComponent<BallScript>();
        mFlashEffect = GetComponent<FlashEffectScript>();
        mSMM = GetComponent<StateMachineManager>();
        mSMM.StateTransition += OnAnimStateChange;
        mSMM.TagTransition += OnAnimTagChange;
        mTeam = GetComponent<TeamScript>();
        /*
        chWeaponSub = chWeapon.GetChild(0);
        weaponOffset = chWeaponSub.localPosition.x;
        List<SpriteRenderer> eyeSprites = chBody.GetComponentsInChildren<SpriteRenderer>().ToList();
        foreach (SpriteRenderer eye in eyeSprites)
        {
            chEyes.Add(eye.transform);
        }
        */
        foreach (Modifier m in Enum.GetValues(typeof(Modifier)))
        {
            chModifiersAdd.Add(m, new Dictionary<string, float>());
            chModifiersMul.Add(m, new Dictionary<string, float>());
            UpdateModifier(m);
        }
    }

    protected void Update()
    {
        switch (faceState) {
            case FaceState.moveDir:
                //Debug.Log(activeRbody.velocity.magnitude);
                ball.CharacterFace(moveDirection, false);
                break;
            case FaceState.targetDir:
                ball.CharacterFace(targetPosition - (Vector2)mTransform.position, false);
                break;
        }
        if (dodgeTimer > 0) dodgeTimer -= Time.deltaTime;
        mAnim.SetFloat("AttackSpeed", tempAttackSpeed);
    }

    protected void FixedUpdate()
    {
        //Debug.Log(mRbody.velocity.magnitude);
        if (isMoving) Move(moveDirection);
    }

    #region [函数组]动画器事件
    protected virtual void OnAnimStateChange(int exitState, int enterState) { }
    protected virtual void OnAnimTagChange(int exitTag, int enterTag) { }
    #endregion

    //重置状态，用于被强制打断动作后正常结束目前状态
    protected virtual void ResetStatus() { }

    //数值改变后，重设数据
    protected void SetData()
    {

    }
    //通过输入设置目标位置
    public void SetTargetFromInput(Device mode)
    {
        if (mode == Device.keyboard)
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

    //添加修正
    //type: 修正值类型
    //key: 修正值来源
    //value: 修正值数据
    //isIndie: 是否是独立乘区
    public void SetModifier(Modifier type, string key, float value, bool isIndie = false)
    {
        if (isIndie)
        {
            chModifiersMul[type][key] = value;
        }
        else
        {
            chModifiersAdd[type][key] = value;
        }
        UpdateModifier(type);
    }

    //移除修正
    //type: 修正值类型
    //key: 修正值来源
    //isIndie: 是否是独立乘区

    public void RemoveModifier(Modifier type, string key, bool isIndie = false)
    {
        if (isIndie)
        {
            chModifiersMul[type].Remove(key);
        }
        else
        {
            chModifiersAdd[type].Remove(key);
        }
        UpdateModifier(type);
    }

    //获取修正
    //type: 修正值类型
    //key: 修正值来源
    //isIndie: 是否是独立乘区
    public float GetModifier(Modifier type, string key, bool isIndie = false)
    {
        if (isIndie)
        {
            if (chModifiersMul[type].ContainsKey(key))
            {
                return chModifiersMul[type][key];
            }
            else
            {
                return float.MinValue;
            }
        }
        else
        {
            if (chModifiersAdd[type].ContainsKey(key))
            {
                return chModifiersAdd[type][key];
            }
            else
            {
                return float.MinValue;
            }
        }
    }

    //更新修正值
    public void UpdateModifier(Modifier type)
    {
        float sum = 1;
        foreach (float v in chModifiersAdd[type].Values)
        {
            sum += v;
        }
        foreach (float v in chModifiersMul[type].Values)
        {
            sum *= v;
        }
        sum = Math.Max(sum, 0);
        finalModifier[type] = sum;
    }
    
    //角色移动(移动速度 * 移动修正)
    public virtual void Move(Vector2 dir)
    {
        mRbody.AddForce(dir * chData["moveSpeed"] * finalModifier[Modifier.move]);
        activeRbody.AddForce(dir * chData["moveSpeed"] * finalModifier[Modifier.move]);
    }

    //移动开始
    public void MoveStart(Vector2 dir)
    {
        isMoving = true;
        moveDirection = dir;
    }

    //移动结束
    public void MoveEnd()
    {
        isMoving = false;
    }

    //角色闪避(修正闪避率+100%)
    public void DodgeStart()
    {
        ResetStatus();
        faceState = FaceState.lockedDir;
        SetModifier(Modifier.move, "dodge", 0f, true);
        SetModifier(Modifier.dodge, "dodge", 0f, true);
        dodgeTimer = dodgeCD;
        if (isMoving)
            ball.CharacterFace(moveDirection, true);
        isDodging = true;
        mTrail.enabled = true;
        mRbody.AddForce(ball.faceDirection * chData["moveSpeed"] * dodgeSpeed);
        activeRbody.AddForce(ball.faceDirection * chData["moveSpeed"] * dodgeSpeed);
        SetCollisionIgnore(true);
    }

    //闪避结束
    public void DodgeEnd()
    {
        mAnim.SetBool("Dodge", false);
        faceState = FaceState.moveDir;
        RemoveModifier(Modifier.move, "dodge", true);
        RemoveModifier(Modifier.dodge, "dodge", true);
        isDodging = false;
        mTrail.enabled = false;
        SetCollisionIgnore(false);
    }

    //关闭/启用角色与其他战斗单位的物理交互
    public void SetCollisionIgnore(bool ignore)
    {
        GameObject[] units = GameObject.FindGameObjectsWithTag("BattleUnit");
        foreach (GameObject unit in units)
        {
            string key = Global.GetUniqueKey(gameObject.GetInstanceID(), unit.GetInstanceID());
            if (ignore)
            {
                //忽略物理时，直接往栈里push
                if(!Global.physicsStack.TryAdd(key, 1))
                {
                    Global.physicsStack[key]++;
                }
            }
            else
            {
                //启用物理时，根据栈内元素执行操作
                int stack;
                if (Global.physicsStack.TryGetValue(key, out stack))
                {
                    if (stack > 1)
                    {
                        //栈大于1，说明关系的另一边也在忽略物理，把自己的栈出了后直接结束
                        Global.physicsStack[key]--;
                        return;
                    }
                    else
                    {
                        //栈小于等于1，直接关了栈释放内存，正常运行忽略物理
                        Global.physicsStack.Remove(key);
                    }
                }
            }
            Collider2D[] colliders = unit.GetComponents<Collider2D>();
            foreach (Collider2D collider in colliders)
            {
                if (!collider.isTrigger)
                    Physics2D.IgnoreCollision(mCollider, collider, ignore);
                    
            }
        }
    }

    //写入伤害判定类型（传递给伤害脚本）
    public void AttackSet(string action)
    {
        chActionData[action].damageScript.SetAction(action);
    }

    //角色受伤
    public void GetHit(HitData hit, IAttackable attacker = null)
    {
        OnHitEnter(hit);
        float damage = hit.damage;
        string hitMessage;
        float hitchance = (1 - chData["dodge"]) * finalModifier[Modifier.dodge];
        if (hitchance > UnityEngine.Random.value)
        {
            mRbody.AddForce(hit.knockback);
            float finalDamage = Mathf.Ceil(damage * (1 - chData["defense"]) * finalModifier[Modifier.defense]);
            if (finalDamage <= 0)
            {
                finalDamage = 0;
                hitMessage = "blocked";
                CameraManager.instance.CameraShake(Global.instance.cameraShakeData["blocked"], Global.P_CS_GetHit);
            }
            else
            {
                Debug.Log("playerhit");
                mFlashEffect.SetFlash("playerHit");
                nowHealth -= finalDamage;
                float damageRatio = finalDamage / chData["maxHealth"];
                if (damageRatio > 0.2f) CameraManager.instance.CameraShake(Global.instance.cameraShakeData["severeHit"], Global.P_CS_GetHit);
                else if (damageRatio > 0.1f) CameraManager.instance.CameraShake(Global.instance.cameraShakeData["moderateHit"], Global.P_CS_GetHit);
                else CameraManager.instance.CameraShake(Global.instance.cameraShakeData["slightHit"], Global.P_CS_GetHit);
                hitMessage = finalDamage.ToString();
            }
            if (nowHealth <= 0)
            {
                    //死亡
            }

        }
        else
        {
            hitMessage = "miss";
        }
        //Debug.Log("角色伤害信息：" + hitMessage);
        OnHitExit(hit);
    }

    //命中顿帧
    public void HitFreeze(float time)
    {
        if (isHitFreezing != null)
        {
            StopCoroutine(isHitFreezing);
        }
        else
        {
            savedAnimSpeed = mAnim.speed;
            mAnim.speed = 0.1f;
        }
        isHitFreezing = StartCoroutine(HitFreezeDisable(time));
    }

    //命中顿帧结束
    private IEnumerator HitFreezeDisable(float time)
    {
        yield return new WaitForSeconds(time);
        if (mAnim.speed == 0.1f)
        {
            mAnim.speed = savedAnimSpeed;
        }
        isHitFreezing = null;
    }

    //响应用户动作（动画器参数应和输入管理器中按键命名一致）
    //actionName: 输入动作名称
    //inputState: 按键状态
    public void SetAction(string actionName, KeyState inputState)
    {
        //Debug.Log(actionName + ": " + inputState);
        if (inputState == KeyState.pressed)
        {
            switch (actionName)
            {
                case "Dodge":
                    if (dodgeTimer <= 0)
                        mAnim.SetBool(actionName, true);
                    break;
                default:
                    mAnim.SetBool(actionName, true);
                    break;
            }
        }
        if (inputState < KeyState.pressed)
        {
            mAnim.SetBool(actionName, false);
        }
        SetActionSpecial(actionName, inputState);
    }

    #region [函数组]子类用增加功能类函数
    //设置特殊的用户动作输入情况（如长按响应动作）
    public virtual void SetActionSpecial(string actionName, KeyState inputState){}
    //被命中时，命中脚本开始运行前调用
    public virtual void OnHitEnter(HitData hit) { }
    //被命中时，命中脚本完成运行后调用
    public virtual void OnHitExit(HitData hit) { }
    #endregion
}
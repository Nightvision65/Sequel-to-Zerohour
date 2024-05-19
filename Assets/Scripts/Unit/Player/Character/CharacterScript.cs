using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
/*
 * CharacterScript
 * 角色脚本
 * 角色的父类，负责所有角色都能做的事情
 * 负责保存角色的基本数据，以及提供数据接口
 * 能够对用户输入信息进行处理
 */

public class CharacterScript : SerializedMonoBehaviour, IHitable, IAttackable
{
    TrailRenderer _trailRenderer;
    protected FaceState faceState = FaceState.moveDir;    //朝向状态
    public Vector2 moveDirection; //角色移动方向
    [SerializeField] protected Vector2 targetPosition;  //锁定的目标位置
    public Vector2 deviceLockInput;  //输入设备的指针所锁定的Vector2数据，由PlayerController不断更新
    public Dictionary<Attribute, int> chAttribute;    //保存角色的属性
    public Dictionary<string, ActionData> chActionData;    //保存关于角色的动作数据(比如技能动作值、削韧等
    public Dictionary<string, ShiftDataSO> chShiftData;    //保存关于角色的基础位移数据
    public Dictionary<string, float> chData;//保存关于角色的顶层数据(每次基础数据被修改时更新)
    public List<DamageScript> dScripts; //保存直接相关的DamageScript
    protected float nowHealth;    //现在生命值
    protected bool isMoving;    //角色正在移动
    [SerializeField] protected float dodgeSpeed;    //闪避速度(乘数)
    [SerializeField] protected float dodgeCD; //闪避冷却时间
    protected float dodgeTimer;
    protected bool isDodging;   //角色正在闪避
    protected Coroutine isHitFreezing;  //角色正在顿帧（存放顿帧的协程）
    protected float savedAnimSpeed; //临时存放的动画速度，用来顿帧
    protected Transform _transform;
    public BallScript _ball;
    protected Rigidbody2D _rigidbody;
    protected Collider2D _collider;
    protected Animator _animator;
    protected FlashEffectScript _flashEffect;
    protected StateMachineManager _stateMachine;
    protected ModifierManager _modifier;
    protected TeamScript _team;
    public float tempAttackSpeed;
    protected void Start()
    {
        _transform = transform;
        _ball = GetComponent<BallScript>();
        _rigidbody = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _animator = GetComponent<Animator>();
        _trailRenderer = GetComponent<TrailRenderer>();
        _flashEffect = GetComponent<FlashEffectScript>();
        _stateMachine = GetComponent<StateMachineManager>();
        _stateMachine.StateTransition += OnAnimStateChange;
        _stateMachine.TagTransition += OnAnimTagChange;
        string[] modifiers = new string[] { "move", "defense", "dodge" ,"animSpeed"};
        _modifier = new ModifierManager(modifiers);
        _team = GetComponent<TeamScript>();
        /*
        chWeaponSub = chWeapon.GetChild(0);
        weaponOffset = chWeaponSub.localPosition.x;
        List<SpriteRenderer> eyeSprites = chBody.GetComponentsInChildren<SpriteRenderer>().ToList();
        foreach (SpriteRenderer eye in eyeSprites)
        {
            chEyes.Add(eye.transform);
        }
        */
    }

    protected void Update()
    {
        switch (faceState) {
            case FaceState.moveDir:
                //Debug.Log(activeRbody.velocity.magnitude);
                _ball.CharacterFace(moveDirection, false);
                break;
            case FaceState.targetDir:
                _ball.CharacterFace(targetPosition - (Vector2)_transform.position, false);
                break;
        }
        if (dodgeTimer > 0) dodgeTimer -= Time.deltaTime;
        _animator.SetFloat("AttackSpeed", tempAttackSpeed);
    }

    protected void FixedUpdate()
    {
        //Debug.Log(_rigidbody.velocity.magnitude);
        if (isMoving) Move(moveDirection);
    }

    #region [函数组]动画器事件
    protected virtual void OnAnimStateChange(int exitState, int enterState) { }
    protected virtual void OnAnimTagChange(int exitTag, int enterTag) { }
    #endregion

    //重置状态，用于被强制打断动作后正常结束目前状态
    protected virtual void ResetStatus() { }

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
                targetPosition = (Vector2)_transform.position + moveDirection.normalized;
            }
            else
            {
                targetPosition = (Vector2)_transform.position + deviceLockInput.normalized;
            }
        }
    }

    
    //角色移动(移动速度 * 移动修正)
    public virtual void Move(Vector2 dir)
    {
        _rigidbody.AddForce(dir * chData["moveSpeed"] * _modifier.GetModifier("move"));
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
        _modifier.SetModifier("move", "dodge", 0f, true);
        _modifier.SetModifier("dodge", "dodge", 0f, true);
        dodgeTimer = dodgeCD;
        if (isMoving)
            _ball.CharacterFace(moveDirection, true);
        isDodging = true;
        _trailRenderer.enabled = true;
        _rigidbody.AddForce(GetFaceDir() * chData["moveSpeed"] * dodgeSpeed);
        Global.instance.SetCollisionIgnore(_transform, true);
    }

    //闪避结束
    public void DodgeEnd()
    {
        _animator.SetBool("Dodge", false);
        faceState = FaceState.moveDir;
        _modifier.RemoveModifier("move", "dodge", true);
        _modifier.RemoveModifier("dodge", "dodge", true);
        isDodging = false;
        _trailRenderer.enabled = false;
        Global.instance.SetCollisionIgnore(_transform, false);
    }

    //写入伤害判定类型（传递给伤害脚本）
    public void SetAttack(string action, int index)
    {
        dScripts[index].SetAction(chActionData[action]);
    }

    //开启位移动作
    public Coroutine SetShift(string key)
    {
        ShiftDataSO data = chShiftData[key];
        return StartCoroutine(Shift(data.force, data.duration, data.angle, data.followDir));
    }

    //位移协程
    public IEnumerator Shift(float force, float duration, float angle, bool followDir)
    {
        Vector2 dir = Quaternion.Euler(0, 0, angle) * GetFaceDir();
        int time = Mathf.Max(1, Mathf.RoundToInt(duration / Time.fixedDeltaTime));
        while (time > 0)
        {
            time--;
            if (followDir)
            {
                dir = Quaternion.Euler(0, 0, angle) * GetFaceDir();
            }
            _rigidbody.AddForce(force * dir);
            yield return new WaitForFixedUpdate();
        }
    }

    //获取朝向
    public Vector2 GetFaceDir()
    {
        return _ball.GetFaceDir();
    }

    //角色受伤
    public void GetHit(ref UnitHitEvent hit)
    {
        OnHitEnter(hit);
        float damage = hit.hitData.damage;
        float finalDamage = damage;
        string hitMessage;
        float hitchance = (1 - chData["dodge"]) * _modifier.GetModifier("dodge");
        if (hitchance > UnityEngine.Random.value)
        {
            finalDamage = Mathf.Ceil(damage * (1 - chData["defense"]) * _modifier.GetModifier("defense"));
            if (finalDamage <= 0)
            {
                finalDamage = 0;
                hitMessage = "blocked";
                hit.hitData.knockback /= 2;
                CameraManager.instance.CameraShake(Global.instance.cameraShakeData["blocked"], Global.P_CS_GetHit);
            }
            else
            {
                //Debug.Log("playerhit");
                _flashEffect.SetFlash("playerHit");
                nowHealth -= finalDamage;
                float damageRatio = finalDamage / chData["maxHealth"];
                if (damageRatio > 0.2f) CameraManager.instance.CameraShake(Global.instance.cameraShakeData["severeHit"], Global.P_CS_GetHit);
                else if (damageRatio > 0.1f) CameraManager.instance.CameraShake(Global.instance.cameraShakeData["moderateHit"], Global.P_CS_GetHit);
                else CameraManager.instance.CameraShake(Global.instance.cameraShakeData["slightHit"], Global.P_CS_GetHit);
                hitMessage = finalDamage.ToString();
            }
            _rigidbody.AddForce(hit.hitData.knockback);
            if (nowHealth <= 0)
            {
                    //死亡
            }
        }
        else
        {
            finalDamage = -1;
            hitMessage = "miss";
        }
        //Debug.Log("角色伤害信息：" + hitMessage);
        OnHitExit(hit);
        //调整HitData的伤害为最终实际伤害
        hit.hitData.damage = finalDamage;
    }

    public void LandHit(UnitHitEvent hit)
    {
        if (hit.script.GetHitCount() == 1)
        {
            Debug.Log("effect");
            //命中自己顿帧
            if (hit.actionData.baseData.hitFreezeTime > 0 && hit.actionData.hasTag(ActionTag.melee))
            {
                HitFreeze(hit.actionData.baseData.hitFreezeTime);
            }
            //命中相机震动
            if (hit.actionData.baseData.hitCameraShake.intensity > 0)
            {
                CameraManager.instance.CameraShake(hit.actionData.baseData.hitCameraShake, Global.P_CS_DealHit);
            }
        }
    }

    //命中顿帧
    public void HitFreeze(float time)
    {
        if (time > 0)
        {
            if (isHitFreezing != null)
            {
                StopCoroutine(isHitFreezing);
            }
            else
            {
                savedAnimSpeed = _animator.speed;
                _animator.speed = 0.1f;
            }
            isHitFreezing = StartCoroutine(HitFreezeDisable(time));
        }
    }

    //命中顿帧结束
    private IEnumerator HitFreezeDisable(float time)
    {
        yield return new WaitForSeconds(time);
        if (_animator.speed == 0.1f)
        {
            _animator.speed = savedAnimSpeed;
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
                        _animator.SetBool(actionName, true);
                    break;
                default:
                    _animator.SetBool(actionName, true);
                    break;
            }
        }
        if (inputState < KeyState.pressed)
        {
            _animator.SetBool(actionName, false);
        }
        SetActionSpecial(actionName, inputState);
    }

    #region [函数组]子类用增加功能类函数
    //设置特殊的用户动作输入情况（如长按响应动作）
    public virtual void SetActionSpecial(string actionName, KeyState inputState){}
    //被命中时，命中脚本开始运行前调用
    public virtual void OnHitEnter(UnitHitEvent hitArgs) { }
    //被命中时，命中脚本完成运行后调用
    public virtual void OnHitExit(UnitHitEvent hitArgs) { }
    #endregion
}
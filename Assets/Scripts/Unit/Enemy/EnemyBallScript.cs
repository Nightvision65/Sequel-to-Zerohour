using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
/*
 * EnemyBallScript
 * 敌人玉脚本
 * 敌人脚本的子类
 * 负责玉型敌人的相关实现
 */

public class EnemyBallScript : EnemyScript
{
    protected BallScript _ball;
    protected FaceState faceState = FaceState.moveDir;    //朝向状态
    protected new void Start()
    {
        base.Start();
        _ball = GetComponent<BallScript>();
    }
    protected new void Update()
    {
        base.Update();
        faceDirection = _ball.faceDirection;
        switch (faceState)
        {
            case FaceState.moveDir:
                _ball.CharacterFace(moveDirection, false);
                break;
            case FaceState.targetDir:
                if (target)
                    _ball.CharacterFace((Vector2)target.position - (Vector2)_transform.position, false);
                break;
        }
    }
}

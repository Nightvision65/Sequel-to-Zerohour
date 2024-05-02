using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
/*
 * BallScript
 * 玉脚本
 * 玉的父类，负责所有玉形单位能做的事情
 */
public class BallScript : SerializedMonoBehaviour
{
    [SerializeField] private Transform chBody;
    public List<WeaponSpriteData> chWeapon;
    public Vector2 faceDirection; //朝向
    [SerializeField] private float discipline;    //纪律性，影响转向时实际转向的速度
    [SerializeField] private bool weaponCorrect;  //是否开启旋转时的武器位置修正（就是绕着椭圆还是圆）
    [SerializeField] private Ternary weaponFixedSort; //武器强制排序（强制在角色下或角色上）
    private List<Transform> chEyes = new List<Transform>();
    private Vector2 lerpDirection = Vector2.right;   //过渡中的实际朝向

    // Start is called before the first frame update
    private void Start()
    {
        List<SpriteRenderer> eyeSprites = chBody.GetComponentsInChildren<SpriteRenderer>().ToList();
        foreach (SpriteRenderer eye in eyeSprites)
        {
            chEyes.Add(eye.transform);
        }
    }
    private void Update()
    {
        BallSpriteProcess(false);
    }

    //角色朝向指定位置(now表示是否立刻转向指定位置)
    //dir: 朝向的方向
    //now: 是否立刻朝向（为False的话武器会根据discipline慢慢转过去）
    public void CharacterFace(Vector2 dir, bool now)
    {
        faceDirection = dir.normalized;
        if (now) BallSpriteProcess(true);
    }

    //处理精灵（图层排序、位置等）
    public void BallSpriteProcess(bool now)
    {
        if (Vector2.Angle(lerpDirection, faceDirection) > 0.1f)
        {
            float nowDir = Mathf.Atan2(lerpDirection.y, lerpDirection.x) * Mathf.Rad2Deg;
            if (now)
            {
                lerpDirection = faceDirection;
            }
            else
            {
                //接近180°转向时，做一个微调使Slerp运行正常
                if(Vector3.Angle(lerpDirection, faceDirection) > 179f)
                {
                    lerpDirection += new Vector2(0.01f, 0.01f);
                }
                lerpDirection = (Vector2)Vector3.Slerp((Vector3)lerpDirection, (Vector3)faceDirection, Time.deltaTime * discipline).normalized;
            }
            float targetDir = Mathf.Atan2(lerpDirection.y, lerpDirection.x) * Mathf.Rad2Deg;
            float faceRotate = nowDir - targetDir;
            //武器和身体翻转
            if (targetDir >= -90 && targetDir < 90)
            {
                foreach (WeaponSpriteData weapon in chWeapon)
                {
                    weapon.main.localScale = new Vector3(1, 1, 1);
                }
                if (chBody.localScale.z == -1)
                    faceRotate += 180;
                chBody.localScale = new Vector3(1, 1, 1);
            }
            else
            {
                foreach (WeaponSpriteData weapon in chWeapon)
                {
                    weapon.main.localScale = new Vector3(1, -1, 1);
                }
                if (chBody.localScale.z == 1)
                    faceRotate += 180;
                chBody.localScale = new Vector3(1, 1, -1);
            }
            //处理眼睛旋转
            chBody.RotateAround(chBody.position, chBody.up, faceRotate);
            //处理武器旋转
            foreach (WeaponSpriteData weapon in chWeapon)
            {
                float weaponAngle = targetDir + weapon.offsetDir;
                if (weaponAngle > 180) weaponAngle -= 360;
                if (weaponAngle < -180) weaponAngle += 360;

                //武器朝向指定位置（逐渐靠近)
                Quaternion weaponDirection = Quaternion.Euler(new Vector3(0, 0, weaponAngle));
                weapon.main.localRotation = weaponDirection;
                float xCorrect;
                if (weaponCorrect)
                {
                    float sint = Mathf.Sin(Mathf.Deg2Rad * (weaponAngle));
                    xCorrect = Mathf.Sqrt(1 + 3 * sint * sint);
                }
                else
                {
                    xCorrect = 1;
                }
                Vector3 weaponPosition = new Vector3(weapon.offsetDis / xCorrect, 0, 0);
                weapon.sub.localPosition = weaponPosition;
                //武器排序图层
                if (weaponAngle > 0)
                {
                    weapon.sort.sortingOrder = 0;
                }
                else
                {
                    weapon.sort.sortingOrder = 1;
                }
                //武器图层强制排序
                switch (weaponFixedSort)
                {
                    case Ternary.positive:
                        weapon.sort.sortingOrder = 2;
                        break;
                    case Ternary.negative:
                        weapon.sort.sortingOrder = -2;
                        break;
                }
            }
        }
        //眼睛可视判定
        foreach (Transform eye in chEyes)
        {
            //Debug.Log(eye.name+eye.position.z);
            if (eye.position.z > -0.125f)
            {
                eye.gameObject.SetActive(false);
            }
            else
            {
                eye.gameObject.SetActive(true);
            }
        }
    }

    public void SetWeaponSort(Ternary ter)
    {
        weaponFixedSort = ter;
    }
}

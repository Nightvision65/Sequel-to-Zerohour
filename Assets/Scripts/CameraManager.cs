using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
 * CameraManager
 * 相机管理器
 * 处理相机相关的一切功能（如相机震动）
 */

public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;
    private int shakePriority = 999;
    private float shakeTime, shakeTimer, shakeIntensity;
    private CinemachineVirtualCamera mCamera;
    private CinemachineBasicMultiChannelPerlin mCameraNoise;
    // Start is called before the first frame update
    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        mCamera = GetComponent<CinemachineVirtualCamera>();
        mCameraNoise = mCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Global.IsTriggered(ref shakeTimer))
        {
            mCameraNoise.m_AmplitudeGain = 0;
            shakePriority = 999;
        }
        /*
    else
    {
        if (shakeTimer > 0)
        {
            mCameraNoise.m_AmplitudeGain = Mathf.Lerp(shakeIntensity, 0f, 1 - shakeTimer / shakeTime);
        }
    }
        */
    }

    //相机震动(优先级越小越高)
    public void CameraShake(CameraShakeData data, int priority)
    {
        if (priority <= shakePriority)
        {
            shakeIntensity = data.intensity;
            mCameraNoise.m_AmplitudeGain = shakeIntensity;
            shakeTime = data.duration;
            shakeTimer = shakeTime;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/*
 * FlashEffectScript
 * 闪烁效果脚本
 * 精灵需要闪烁时，调用该脚本
 * 之后可能扩容成任意Shader特效，还要设置优先级
 */
public class FlashEffectScript : MonoBehaviour
{
    private List<SpriteRenderer> mSRenderers;
    private List<Material> mMaterials;
    private float flashAlpha;
    private bool isFlashing;
    private Coroutine flashCoroutine;
    private FlashDataSO flashData;

    // Start is called before the first frame update
    void Start()
    {
        mSRenderers = GetComponentsInChildren<SpriteRenderer>().ToList();
        mMaterials = new List<Material>();
        foreach (SpriteRenderer renderer in mSRenderers)
        {
            mMaterials.Add(renderer.material);
        }
    }
    void Update()
    {


    }

    private void SetMaterial()
    {
        foreach (Material material in mMaterials)
        {
            material.SetColor("_FlashColor", flashData.flashColor);
            material.SetFloat("_FlashAmount", flashAlpha);
        }
    }
    //直接设置精灵闪烁值
    public void SetFlashStatic(string type)
    {
        flashData = Global.instance.flashEffectData[type];
        flashAlpha = flashData.flashMax;
        SetMaterial();
    }

    //设置精灵闪烁
    public void SetFlash(string type)
    {
        flashData = Global.instance.flashEffectData[type];
        flashAlpha = flashData.flashMax;
        if (isFlashing) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashEffect(type));
    }
    public IEnumerator FlashEffect(string type)
    {
        isFlashing = true;
        //Debug.Log("flash: " + flashAlpha);
        while (flashAlpha > 0.01f)
        {
            yield return null;
            flashAlpha = Mathf.Lerp(flashAlpha, 0, flashData.flashSpeed * Time.deltaTime);
            SetMaterial();
        }
        isFlashing = false;
        flashAlpha = 0f;
        SetMaterial();
    }
}

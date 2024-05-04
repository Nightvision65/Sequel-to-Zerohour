using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    public TMP_Text scoreText, scoreText2, hpText;
    public int score;
    public Transform endText;
    private void Awake()
    {
        instance = this;
    }
    private void Update()
    {
        if(!PlayerController.instance.isActiveAndEnabled && !endText.gameObject.activeSelf)
        {
            scoreText2.text = scoreText.text;
            endText.gameObject.SetActive(true);
        }
    }
    public void UpdateScore(int s)
    {
        score += s;
        scoreText.text = "Score: " + score.ToString();
    }
    public void UpdateHP(float hp)
    {
        hpText.text = "HP: " + hp.ToString();
    }
    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}

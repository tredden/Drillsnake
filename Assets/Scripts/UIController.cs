using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIController : MonoBehaviour {

    [SerializeField]
    TMPro.TextMeshProUGUI bankGoldText;
    [SerializeField]
    TMPro.TextMeshProUGUI unbankedGoldText;
    [SerializeField]
    TMPro.TextMeshProUGUI lostGoldText;
    [SerializeField]
    TMPro.TextMeshProUGUI depthText;
    [SerializeField]
    float depthMult = 0.1f;
    [SerializeField]
    float goldMult = 1f;

    int currentBankedGold;
    int currentUnbankedGold;
    int currentLostGold;
    float targetDepositTime;
    float currentDepositTime;

    // Start is called before the first frame update
    void Start()
    {
        lostGoldText.alpha = 0f;
    }

    // Update is called once per frame
    void Update()
    {
       if (currentDepositTime <= targetDepositTime + .05) {
            float frac = Mathf.Clamp01(currentDepositTime / targetDepositTime);
            int diff = (int) (Mathf.RoundToInt((Mathf.Lerp(0f, currentUnbankedGold, frac) * goldMult)) / goldMult);
            SetGold(currentBankedGold + diff, currentUnbankedGold - diff, currentLostGold);
            currentDepositTime += Time.deltaTime;
            if (currentLostGold > 0) {
                lostGoldText.alpha = (1f - frac);
            } else {
                lostGoldText.alpha = 0f;
            }
       }
    }

    public void SetGold(int banked, int current, int lost)
    {
        bankGoldText.text = "$" + (int)(banked*goldMult);
        unbankedGoldText.text = "+$" + (int)(current*goldMult);
        if (lost > 0) {
            lostGoldText.text = "-$" + (int)(lost * goldMult);
            lostGoldText.alpha = 1f;
        } else {
            lostGoldText.alpha = 0f;
        }
    }

    public void TriggerGoldDeposit(int banked, int current, int lost, float time)
    {
        currentBankedGold = banked;
        currentUnbankedGold = current;
        currentLostGold = lost;
        lostGoldText.text = "-$" + (int)(lost * goldMult);
        targetDepositTime = time;
        currentDepositTime = 0f;
    }

    public void SetDepth(float depth)
    {
        depthText.text = "DEPTH: " + (int)(depth * depthMult) + "M";
    }
}

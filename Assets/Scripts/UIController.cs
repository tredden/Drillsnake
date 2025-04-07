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
    UnityEngine.UI.RawImage fuelGuage;
    [SerializeField]
    float depthMult = 0.1f;
    [SerializeField]
    float goldMult = 1f;

    int currentBankedGold;
    int currentUnbankedGold;
    int currentLostGold;
    float targetDepositTime;
    float currentDepositTime;

    float currentFuelLevel;
    float currentMaxFuel;
    float targetFuelTime;
    float currentFuelTime;

    Vector2 originalFuelBarScale;
    // Start is called before the first frame update
    void Start()
    {
        lostGoldText.alpha = 0f;
        originalFuelBarScale = fuelGuage.rectTransform.sizeDelta;
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
       if (currentFuelTime <= targetFuelTime + .05) {
            float frac = Mathf.Clamp01(currentFuelTime / targetFuelTime);
            SetFuel(currentMaxFuel, Mathf.Lerp(frac, currentFuelLevel, currentMaxFuel));
            currentFuelTime += Time.deltaTime;
        }
    }

    Color buyGreen = new Color(0.4156863f, 0.7450981f, 0.1882353f, 1f);
    Color buyRed = new Color(0.7450981f, 0.2853141f, 0.1882353f, 1f);
    public void SetFuel(float maxFuel, float currentFuel)
    {
        float t = currentFuel / maxFuel;
        Color c = Color.Lerp(buyRed, buyGreen, t * 1.4f - .2f);
        fuelGuage.rectTransform.sizeDelta = new Vector2(originalFuelBarScale.x, originalFuelBarScale.y * t);
        fuelGuage.color = c;
    }
    public void TriggerFuelReset(float maxFuel, float currentFuel, float time)
    {
        currentMaxFuel = maxFuel;
        currentFuelLevel = currentFuel;
        targetFuelTime = time;
        currentFuelTime = 0f;
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

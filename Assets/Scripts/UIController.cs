using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIController : MonoBehaviour {

    [SerializeField]
    TMPro.TextMeshProUGUI bankGoldText;
    [SerializeField]
    TMPro.TextMeshProUGUI currentGoldText;
    [SerializeField]
    TMPro.TextMeshProUGUI depthText;
    [SerializeField]
    float depthMult = 0.1f;
    [SerializeField]
    float goldMult = 0.01f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetGold(int banked, int current)
    {
        bankGoldText.text = "$" + (int)(banked*goldMult);
        currentGoldText.text = "+$" + (int)(current*goldMult);
    }

    public void SetDepth(float depth)
    {
        depthText.text = "DEPTH: " + (int)(depth * depthMult) + "M";
    }
}

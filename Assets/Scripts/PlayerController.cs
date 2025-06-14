using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    int gold = 0;

    private SnakeController snakeController;
    [SerializeField]
    UIController uiController;
    [SerializeField]
    float goldDeathMult = .5f;
    [SerializeField]
    float deathTime = .3f;
    bool dying;
    bool shopping = false;

    bool enableSpeedControl = false;
    float maxSpeedControl = 0f;
    float targetSpeedChange = 1f;
    public float targetSpeedChangeTime = 1f;

    bool hasShownSalvageText = false;

    ControlInputs controlInputs = new ControlInputs();

    [SerializeField]
    private float depositPeriod = 1f;
    private float lastDepositTime = 0;
    bool returnElligible = false;

    void OnSnakeGoldGained(int newGoldAmount)
    {
        uiController.SetGold((int)gold, newGoldAmount, 0);
    }

    void OnSnakeDepthChanged(float newDepth)
    {
        float depth = MapGenerator.GetInstance().GetTargetSpawnPos().y - newDepth;
        uiController.SetDepth(depth);

        if (depth > 1f) {
            returnElligible = true;
        }

        if (returnElligible && depth <= 0f)
        {
            if (Time.time - lastDepositTime > depositPeriod)
            {
                lastDepositTime = Time.time;
                //if (snakeController.gold >= 100f) {
                //    Debug.Log("Depositing. snake: " + (int)snakeController.gold + " gold: " + (int)gold);
                //    snakeController.SetGold(snakeController.gold - 100f);
                //    gold += 100;
                //    uiController.SetGold((int)gold, (int)snakeController.gold, 0);
                //}
                GameController.GetInstance().SetDescText("There's gold down there!!!\nWhat are you waiting for???");
                ReturnToBase();
            }
        }
    }
    void OnSnakeSegmentExploded(int totalSegments, int segmentsLeft, float goldPending)
    {
        int snakeGoldAfterDeath = (int) (snakeController.gold * this.goldDeathMult);
        int goldLossPending = (int) goldPending - snakeGoldAfterDeath;
        int accedMissing = (int)(((totalSegments - segmentsLeft) / (float)totalSegments) * goldLossPending);
        uiController.SetGold(gold, (int)goldPending - accedMissing, accedMissing);
    }

    void OnSnakeDeath()
    {
        dying = true;
        if (!hasShownSalvageText) {
            GameController.GetInstance().SetDescText("Make sure you bring your gold back to the surface if you want to avoid paying the salvage cost!!!");
            hasShownSalvageText = true;
        }
        TriggerGoldReturn();
    }

    void OnUpgradePurchased(int cost, UpgradeType type, float newValue)
    {
        gold -= cost;
        SetStat(type, newValue);
        uiController.SetGold((int)gold, 0, 0);
    }

    void SetStat(UpgradeType type, float value)
    {
        // TODO: change stats to match upgrade types
        switch (type) {
            case UpgradeType.ALCHEMY:
                snakeController.alchemy = value;
                break;
            case UpgradeType.DRILL_STRENGTH:
                snakeController.drillStats.drillHardness = value;
                break;
            case UpgradeType.FUEL_AMOUNT:
                snakeController.maxFuel = value;
                snakeController.currentFuel = value;
                break;
            case UpgradeType.HEAT_CAPACITY:
                snakeController.drillStats.maxDrillHeat = value;
                break;
            case UpgradeType.MAX_SPEED:
                this.maxSpeedControl = value;
                break;
            case UpgradeType.MOVE_SPEED:
                snakeController.maxSpeed = value;
                break;
            case UpgradeType.SALVAGE_AMOUNT:
                goldDeathMult = value;
                break;
            case UpgradeType.SPEED_CONTROL:
                enableSpeedControl = value > 0;
                break;
            case UpgradeType.TURN_SPEED:
                snakeController.turnSpeed = value; 
                break;
        }
    }

    void OpenShop()
    {
        GameController.GetInstance().OpenShop((int)(gold));
    }

    void OnShopOpen()
    {
        shopping = true;
        snakeController.state = SnakeState.Shopping;
    }

    void ExitShop()
    {
        // GameController.GetInstance().ExitShop();
        shopping = false;
        snakeController.state = SnakeState.Alive;
    }

    // Start is called before the first frame update
    void Start()
    {
		snakeController = GetComponent<SnakeController>();
        if (snakeController == null)
        {
            Debug.LogError("SnakeController component not found on the GameObject.");
        }
        // Setup Snake Event Hooks
        snakeController.onGoldGained += this.OnSnakeGoldGained;
        snakeController.onDepthChanged += this.OnSnakeDepthChanged;
        snakeController.onSegmentExploded += this.OnSnakeSegmentExploded;
        snakeController.onDeath += this.OnSnakeDeath;
        snakeController.transform.position = MapGenerator.GetInstance().GetTargetSpawnPos();
        snakeController.Reset();

        // Start the snake after one second.
        StartCoroutine(StartSnake(1f));

        // Setup Upgrade Stats and Hooks
        GameController gc = GameController.GetInstance();
        gc.onUpgradePurchased += this.OnUpgradePurchased;
        gc.onShopEnter += this.OnShopOpen;
        gc.onShopExit += this.ExitShop;
        foreach (UpgradeType type in Enum.GetValues(typeof(UpgradeType))) {
            this.SetStat(type, gc.GetValueForUpgradeType(type));
        }
    }

    private IEnumerator StartSnake(float delay = 1f)
    {
        yield return new WaitForSeconds(delay);
        snakeController.state = SnakeState.Alive;
    }

    // Update is called once per frame
    void Update()
    {
        if (shopping) {
            controlInputs.turn = 0;
            controlInputs.targetSpeed = 0;
            snakeController.SetControlInputs(controlInputs);
        }

        controlInputs.turn = Input.GetAxis("Horizontal");
        float targetAcc = Input.GetAxis("Vertical") * 1f / this.targetSpeedChangeTime * Time.deltaTime;
        this.targetSpeedChange = Mathf.Clamp(targetSpeedChange + targetAcc, -.5f, .5f + this.maxSpeedControl);
        controlInputs.targetSpeed = 1 + (this.enableSpeedControl ? this.targetSpeedChange : 0f);
        
        snakeController.SetControlInputs(controlInputs);

        if (snakeController.state == SnakeState.Alive && !shopping) {
            uiController.SetFuel(snakeController.maxFuel, snakeController.currentFuel);
        }

        if (snakeController.state == SnakeState.Dead && (Time.time - snakeController.deathTime > deathTime)) {
            ReturnToBase();
        }

    }

    void DepositGold(int amt, int lost, float time)
    {
        uiController.TriggerGoldDeposit((int)gold, amt, lost, time);
        gold += (int)amt;
    }

    void TriggerGoldReturn()
    {
        float goldFactor = snakeController.state == SnakeState.Alive ? 1f : goldDeathMult;
        int goldGain = Mathf.RoundToInt(snakeController.gold * goldFactor);
        int goldLost = (int) snakeController.gold - goldGain;
        DepositGold(goldGain, goldLost, deathTime);
        snakeController.SetGold(snakeController.gold % 1f);
    }

    void TriggerFuelRefill()
    {
        // TODO: constant fill rate instead?
        uiController.TriggerFuelReset(snakeController.maxFuel, snakeController.currentFuel, deathTime);
    }

    void ReturnToBase() {
        if (!dying) {
            TriggerGoldReturn();
        }
        this.targetSpeedChange = 0f;
        lastDepositTime = Time.time;
        // let us keep partially accumulated gold
        this.OnSnakeGoldGained(0);
        TriggerFuelRefill();
        snakeController.state = SnakeState.Alive;
        snakeController.speed = 0f;
        snakeController.currentHeat = 0f;
        returnElligible = false;
        if (dying) {
            snakeController.transform.position = MapGenerator.GetInstance().GetTargetSpawnPos();
        }
        snakeController.transform.rotation = Quaternion.identity;
        this.OnSnakeDepthChanged(snakeController.transform.position.y);
        dying = false;
        snakeController.Reset();
        OpenShop();
    }
}

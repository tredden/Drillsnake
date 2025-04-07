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

    ControlInputs controlInputs = new ControlInputs();


    void OnSnakeGoldGained(int newGoldAmount)
    {
        uiController.SetGold((int)gold, newGoldAmount, 0);
    }

    void OnSnakeDepthChanged(float newDepth)
    {
        float depth = MapGenerator.GetInstance().GetTargetSpawnPos().y - newDepth;
        uiController.SetDepth(depth);
    }
    void OnSnakeSegmentExploded(int totalSegments, int segmentsLeft, float goldPending)
    {
        int snakeGoldAfterDeath = (int) (snakeController.gold * this.goldDeathMult);
        float goldLossPending = goldPending - snakeGoldAfterDeath;
        int accedMissing = (int)(((totalSegments - segmentsLeft) / (float)totalSegments) * goldLossPending);
        uiController.SetGold(gold, (int)goldPending - accedMissing, accedMissing);
    }

    void OnSnakeDeath()
    {
        dying = true;
        TriggerGoldReturn();
    }

    void OnUpgradePurchased(int cost, UpgradeType type, float newValue)
    {
        gold -= cost;
        SetStat(type, newValue);
    }

    void SetStat(UpgradeType type, float value)
    {
        // TODO: change stats to match upgrade types
        switch (type) {
            case UpgradeType.ALCHEMY:
                // TODO:
                break;
            case UpgradeType.DRILL_STRENGTH:
                snakeController.drillStats.drillHardness = value;
                break;
            case UpgradeType.FUEL_AMOUNT:
                // TODO:
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
        snakeController.Reset();
        snakeController.transform.position = MapGenerator.GetInstance().GetTargetSpawnPos();

        // Setup Upgrade Stats and Hooks
        GameController gc = GameController.GetInstance();
        gc.onUpgradePurchased += this.OnUpgradePurchased;
        gc.onShopEnter += this.OnShopOpen;
        gc.onShopExit += this.ExitShop;
        foreach (UpgradeType type in Enum.GetValues(typeof(UpgradeType))) {
            this.SetStat(type, gc.GetValueForUpgradeType(type));
        }
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
        controlInputs.targetSpeed = 1 + (this.enableSpeedControl ? (Input.GetAxis("Vertical") * this.maxSpeedControl) : 0f);
        
        snakeController.SetControlInputs(controlInputs);

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
    }

    void ReturnToBase() {
        if (!dying) {
            TriggerGoldReturn();
        }
        // let us keep partially accumulated gold
        snakeController.gold = snakeController.gold % 1f;
        this.OnSnakeGoldGained(0);
        snakeController.state = SnakeState.Alive;
        snakeController.speed = 0f;
        snakeController.currentHeat = 0f;
        snakeController.transform.position = MapGenerator.GetInstance().GetTargetSpawnPos();
        this.OnSnakeDepthChanged(0f);
        dying = false;
        snakeController.Reset();
        snakeController.transform.position = MapGenerator.GetInstance().GetTargetSpawnPos();
        OpenShop();
    }
}

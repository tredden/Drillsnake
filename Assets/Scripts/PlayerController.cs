using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    int gold = 0;

	private SnakeController snakeController;
    [SerializeField]
    UIController uiController;

    void OnSnakeGoldGained(int newGoldAmount)
    {
        uiController.SetGold(gold, newGoldAmount);
    }

    void OnSnakeDepthChanged(float newDepth)
    {
        float depth = MapGenerator.GetInstance().GetTargetSpawnPos().y - newDepth;
        uiController.SetDepth(depth);
    }
    void OnSnakeDeath()
    {

    }

    // Start is called before the first frame update
    void Start()
    {
		snakeController = GetComponent<SnakeController>();
        if (snakeController == null)
        {
            Debug.LogError("SnakeController component not found on the GameObject.");
        }
        snakeController.onGoldGained += this.OnSnakeGoldGained;
        snakeController.onDepthChanged += this.OnSnakeDepthChanged;
        snakeController.onDeath += this.OnSnakeDeath;
    }

    // Update is called once per frame
    void Update()
    {
        ControlInputs controlInputs = new ControlInputs();

        controlInputs.turn = Input.GetAxis("Horizontal");
        controlInputs.targetSpeed = 1 + Input.GetAxis("Vertical") * .8f;
        
        snakeController.SetControlInputs(controlInputs);

        if (snakeController.state == SnakeState.Dead && (Time.time - snakeController.deathTime > 0.3f))
        {
            snakeController.Reset();
            snakeController.transform.position = MapGenerator.GetInstance().GetTargetSpawnPos();
        }
    }

    void ReturnToBase() {
        float goldFactor = snakeController.state == SnakeState.Alive ? 1f : 0.7f;
		gold += Mathf.RoundToInt(snakeController.gold * goldFactor);
        snakeController.gold = 0;
        this.OnSnakeGoldGained(0);
        snakeController.state = SnakeState.Alive;
        snakeController.speed = 0f;
        snakeController.currentHeat = 0f;
        snakeController.transform.position = MapGenerator.GetInstance().GetTargetSpawnPos();
        this.OnSnakeDepthChanged(0f);
    }
}

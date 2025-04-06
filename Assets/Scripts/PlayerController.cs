using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    int gold = 0;

	private SnakeController snakeController;

    // Start is called before the first frame update
    void Start()
    {
		snakeController = GetComponent<SnakeController>();
        if (snakeController == null)
        {
            Debug.LogError("SnakeController component not found on the GameObject.");
        }
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
        snakeController.state = SnakeState.Alive;
        snakeController.speed = 0f;
        snakeController.transform.position = MapGenerator.GetInstance().GetTargetSpawnPos();
    }
}

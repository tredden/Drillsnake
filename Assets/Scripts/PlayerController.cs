using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    
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
        controlInputs.targetSpeed = Input.GetAxis("Vertical");
        //controlInputs.targetSpeed = 1;
        
        snakeController.SetControlInputs(controlInputs);
    }
}

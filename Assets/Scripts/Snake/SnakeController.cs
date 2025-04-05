using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public struct ControlInputs
{
    public float turn; // -1 to 1. -1 is left, 1 is right.
    public float targetSpeed; // 0 to 1. 0 is stop, 1 is full speed.
}


public class SnakeController : MonoBehaviour
{
    [SerializeField]
    private int snakeLength = 5;

    [SerializeField]
    private float segmentDistance = 0.5f;

    [SerializeField]
    private float maxSpeed = 50f;
    [SerializeField]
    private float acceleration = 10f;

    [SerializeField]
    private float turnSpeed = 200f;
    
    // [SerializeField]
    // private GameObject snakeSegmentPrefab;

    private List<GameObject> snakeSegments = new List<GameObject>();
    private Queue<PositionData> positionHistory = new Queue<PositionData>();

    private ControlInputs controlInputs;
    private float speed = 0f;

    // Start is called before the first frame update
    void Start()
    {
        // for (int i = 0; i < snakeLength; i++)
        // {
        //     GameObject segment = Instantiate(snakeSegmentPrefab, transform.position, Quaternion.identity);
        //     snakeSegments.Add(segment);
        // }
    }

    // Update is called once per frame
    void Update()
    {
        // Apply control inputs
        float turn = controlInputs.turn * turnSpeed * Time.deltaTime;
        transform.Rotate(0, 0, -turn);
        speed = Mathf.MoveTowards(speed, controlInputs.targetSpeed * maxSpeed, acceleration * Time.deltaTime);
        
        Vector3 position = transform.position;
        position.x += Mathf.Sin(transform.eulerAngles.z * Mathf.Deg2Rad) * speed * Time.deltaTime;
        position.y += -Mathf.Cos(transform.eulerAngles.z * Mathf.Deg2Rad) * speed * Time.deltaTime;
        transform.position = position;



        // Update position history
        // PositionData positionData = new PositionData();
        // positionData.transform = transform;
        // positionData.distanceDelta = speed * Time.deltaTime;
        // positionHistory.Enqueue(positionData);

        // 
        
    }

    public void SetControlInputs(ControlInputs inputs)
    {
        controlInputs = inputs;
        // TODO: Record this in the history if it's different.
    }
}

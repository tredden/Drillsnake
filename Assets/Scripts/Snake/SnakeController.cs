using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public struct ControlInputs
{
    public float turn; // -1 to 1. -1 is left, 1 is right.
    public float targetSpeed; // 0 to 1. 0 is stop, 1 is full speed.
}


public class SnakeController : MonoBehaviour
{
    [SerializeField]
    private int segments = 5;

    [SerializeField]
    private float segmentDistance = 10f;

    [SerializeField]
    private float maxSpeed = 50f;
    [SerializeField]
    private float acceleration = 10f;

    [SerializeField]
    private float turnSpeed = 200f;
    
    [SerializeField]
    private GameObject snakeSegmentPrefab;

    private List<GameObject> snakeSegments = new List<GameObject>();

    private ControlInputs controlInputs;
    private float speed = 0f;

    // The distance traveled since the snake started
    private float distanceSinceStart = 0f;


    // Start is called before the first frame update
    void Start()
    {
        MakeSegments(segments);
    }

    // Update is called once per frame
    void Update()
    {
        // Apply control inputs
        float turn = controlInputs.turn * turnSpeed * Time.deltaTime;
        transform.Rotate(0, 0, -turn);
        speed = Mathf.MoveTowards(speed, controlInputs.targetSpeed * maxSpeed, acceleration * Time.deltaTime);
        distanceSinceStart += speed * Time.deltaTime;

        Vector3 position = transform.position;
        position.x += Mathf.Sin(transform.eulerAngles.z * Mathf.Deg2Rad) * speed * Time.deltaTime;
        position.y += -Mathf.Cos(transform.eulerAngles.z * Mathf.Deg2Rad) * speed * Time.deltaTime;
        transform.position = position;

        SnakeSegment segment = snakeSegments[0].GetComponent<SnakeSegment>();

        segment.positionQueue.Enqueue(new PositionData
        {
            position = new Vector2(position.x, position.y),
            rotation = transform.eulerAngles.z,
            distanceSinceStart = distanceSinceStart,
        });
        segment.OnParentMove(distanceSinceStart);
    }

    void MakeSegments(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject segment = Instantiate(snakeSegmentPrefab, transform.position, Quaternion.identity);
//            segment.transform.SetParent(transform);
//            segment.transform.localPosition = new Vector3(0, -segmentDistance * (i + 1), 0);
            snakeSegments.Add(segment);

            SnakeSegment currentSegment = segment.GetComponent<SnakeSegment>();
            if (i > 0)
            {
                SnakeSegment previousSegment = snakeSegments[i - 1].GetComponent<SnakeSegment>();
                previousSegment.nextSegment = currentSegment;
            }
        }
        Debug.Log("Snake segments created: " + snakeSegments.Count);
    }

    public void SetControlInputs(ControlInputs inputs)
    {
        controlInputs = inputs;
        // TODO: Record this in the history if it's different.
    }
}

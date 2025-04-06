using System.Collections.Generic;
using UnityEngine;

public struct ControlInputs
{
    public float turn; // -1 to 1. -1 is left, 1 is right.
    public float targetSpeed; // 0 to 1. 0 is stop, 1 is full speed.
}

[System.Serializable]
public struct DrillStats
{
    public float drillRadius;
    public float drillHardness;
}


public class SnakeController : MonoBehaviour
{
    [SerializeField]
    public DrillStats drillStats;
    [SerializeField]
    private int segments = 5;
    [SerializeField]
    private float maxSpeed = 128f;
    [SerializeField]
    private float acceleration = 48f;

    [SerializeField]
    private float turnSpeed = 180f;
    
    [SerializeField]
    private GameObject headPrefab;
    [SerializeField]
    private GameObject bodyPrefab;
    private List<GameObject> snakeSegments = new List<GameObject>();

    private ControlInputs controlInputs;
    private float speed = 0f;
    private bool dead = false;
    private float deathTime = 0f;

    // The distance traveled since the snake started
    private float distanceSinceStart = 0f;

    // Start is called before the first frame update
    void Start()
    {
		SetLength(segments);
    }

    // Update is called once per frame
    void Update()
    {
        if (dead) {
            speed = 0;
            if (Time.time - deathTime > 2) {
                Reset();
            } else {
                return;
            }
        }
        // Apply control inputs
        float turn = controlInputs.turn * turnSpeed * Time.deltaTime;
        transform.Rotate(0, 0, -turn);
        speed = Mathf.MoveTowards(speed, controlInputs.targetSpeed * maxSpeed, acceleration * Time.deltaTime);
        distanceSinceStart += speed * Time.deltaTime;

        Vector3 position = transform.position;
        position.x += Mathf.Sin(transform.eulerAngles.z * Mathf.Deg2Rad) * speed * Time.deltaTime;
        position.y += -Mathf.Cos(transform.eulerAngles.z * Mathf.Deg2Rad) * speed * Time.deltaTime;
        transform.position = position;

        if (snakeSegments.Count == 0) return;

        SnakeSegment segment = snakeSegments[0].GetComponent<SnakeSegment>();
        segment.positionQueue.Enqueue(new PositionData
        {
            position = new Vector2(position.x, position.y),
            rotation = transform.eulerAngles.z,
            distanceSinceStart = distanceSinceStart,
        });
        segment.OnParentMove(distanceSinceStart);
    }

    private void AppendSegments(int add)
    {
        int count = snakeSegments.Count;
        for (int i = count; i < count + add; i++)
        {
            Transform parentTransform = i > 0 ? snakeSegments[i - 1].transform : transform;
            GameObject prefab = i == 0 ? headPrefab : bodyPrefab;
            GameObject segment = Instantiate(prefab, parentTransform.position, parentTransform.rotation);
            snakeSegments.Add(segment);

            GameObject currentSegment = segment;
            if (i > 0)
            {
                GameObject previousSegment = snakeSegments[i - 1];
                SnakeSegment previousSegmentComponent = previousSegment.GetComponent<SnakeSegment>();
                if (previousSegmentComponent == null)
                {
                    Debug.LogError("Previous segment is null");
                    return;
                }
                previousSegmentComponent.SetNext(currentSegment);
            }
        }
    }

    public void SetLength(int length)
    {
        if (length < 0)
        {
            Debug.LogError("Length cannot be negative");
            return;
        }

        length++; // +1 for the head
        if (length < snakeSegments.Count)
        {
			SnakeSegment newLastSegment = snakeSegments[length].GetComponent<SnakeSegment>();
            newLastSegment.SetNext(null);
            for (int i = length; i < snakeSegments.Count; i++)
            {
                Destroy(snakeSegments[i]);
            }
            snakeSegments.RemoveRange(length, snakeSegments.Count - length);
        }
        else if (length > snakeSegments.Count)
        {
            AppendSegments(length - snakeSegments.Count);
        }
    }

    public void SetControlInputs(ControlInputs inputs)
    {
        controlInputs = inputs;
    }

    public void Explode() {
		// Explodes the snake from the head down through its tail over time.
		if (!dead) {
            deathTime = Time.time;
        }

		dead = true;
        Debug.Log("Snake exploded!");
    }

    public void Reset() {
        // Reset the snake to its initial state
        dead = false;
        distanceSinceStart = 0f;
        speed = 0f;

        foreach (GameObject segment in snakeSegments)
        {
            Destroy(segment);
        }
        snakeSegments.Clear();
        SetLength(segments);
    }
}

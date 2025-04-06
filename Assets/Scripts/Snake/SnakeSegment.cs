#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnakeSegment : MonoBehaviour
{
    [SerializeField]
    float frontSpacing = 10f;
    [SerializeField]
    private float backSpacing = 10f;
    
    float computedSpacing = 0f;

    public Queue<PositionData> positionQueue = new Queue<PositionData>();
    private GameObject? next;

    private PositionData? beforeTargetDistance;
    private PositionData? afterTargetDistance;

    public SnakeController? owner;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetNext(GameObject? next)
    {
        this.next = next;
        if (next == null) return;

        SnakeSegment nextSegment = next.GetComponent<SnakeSegment>();
        if (nextSegment == null)
        {
            Debug.LogError("Next segment is not a SnakeSegment");
            return;
        }
        nextSegment.computedSpacing = nextSegment.frontSpacing + backSpacing;
    }

    public void OnParentMove(float parentDistanceSinceStart)
    {
		float targetDistance = parentDistanceSinceStart - computedSpacing;
        SnakeSegment? nextSegment = next != null ? next.GetComponent<SnakeSegment>() : null;

        while (positionQueue.Count > 0 && positionQueue.Peek().distanceSinceStart < targetDistance)
        {
            beforeTargetDistance = afterTargetDistance;
            afterTargetDistance = positionQueue.Dequeue();
            nextSegment?.positionQueue.Enqueue(afterTargetDistance.Value);
        }

        if (afterTargetDistance.HasValue) {
            if (beforeTargetDistance.HasValue) {
				// Interpolate between the two positions based on the distance
				float t = (targetDistance - beforeTargetDistance.Value.distanceSinceStart)
                        / (afterTargetDistance.Value.distanceSinceStart - beforeTargetDistance.Value.distanceSinceStart);
                transform.position = Vector2.Lerp(beforeTargetDistance.Value.position, afterTargetDistance.Value.position, t);
                transform.rotation = Quaternion.Euler(0, 0, Mathf.LerpAngle(beforeTargetDistance.Value.rotation, afterTargetDistance.Value.rotation, t));
            } else {
			    transform.position = afterTargetDistance.Value.position;
                transform.rotation = Quaternion.Euler(0, 0, afterTargetDistance.Value.rotation);
            }
            nextSegment?.OnParentMove(targetDistance);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        owner?.HandleCollision(collision);
    }
}

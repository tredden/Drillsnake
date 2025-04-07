#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ShakeParams
{
    public float amplitude;
    public float frequency;
    public float distancePhaseOffset;
}

public class SnakeSegment : MonoBehaviour
{
    [SerializeField]
    float frontSpacing = 10f;
    [SerializeField]
    private float backSpacing = 10f;
    
    float computedSpacing = 0f;
    float distanceFromHead = 0f;

    public Queue<PositionData> positionQueue = new Queue<PositionData>();
    private GameObject? next;

    private PositionData? beforeTargetDistance;
    private PositionData? afterTargetDistance;

    public SnakeController? owner;

    [SerializeField]
    public ShakeParams shakeParams;

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
        nextSegment.distanceFromHead = distanceFromHead + nextSegment.computedSpacing;
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

            if (owner?.shaking > 0.0f)
            {
				// Apply shake effect perpendicular to the snake's direction
				Vector2 shakeDirection = new Vector2(
					Mathf.Cos(transform.eulerAngles.z * Mathf.Deg2Rad),
					Mathf.Sin(transform.eulerAngles.z * Mathf.Deg2Rad)
				);
				Vector2 shakeOffset = shakeDirection * Mathf.Sin(Time.time * shakeParams.frequency * (Mathf.PI * 2)
				  + distanceFromHead * shakeParams.distancePhaseOffset * (Mathf.PI * 2)) * shakeParams.amplitude * owner.shaking;
				transform.position += (Vector3)shakeOffset;
            }

            nextSegment?.OnParentMove(targetDistance);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        owner?.HandleCollision(collision);
    }
}

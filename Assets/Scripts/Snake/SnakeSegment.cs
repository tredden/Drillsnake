#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnakeSegment : MonoBehaviour
{
    [SerializeField]
    private float segmentLength = 20f;
    
    public Queue<PositionData> positionQueue = new Queue<PositionData>();
    public SnakeSegment? nextSegment;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnParentMove(float parentDistanceSinceStart)
    {
		float moveUpTo = parentDistanceSinceStart - segmentLength;
        PositionData? nextPositionData = null;

        while (positionQueue.Count > 0 && positionQueue.Peek().distanceSinceStart < moveUpTo)
        {
            nextPositionData = positionQueue.Dequeue();
            if (nextSegment != null)
            {
                nextSegment.positionQueue.Enqueue(nextPositionData.Value);
            }
        }

        if (nextPositionData.HasValue) {
			transform.position = nextPositionData.Value.position;
            transform.rotation = Quaternion.Euler(0, 0, nextPositionData.Value.rotation);
            if (nextSegment != null)
            {
				nextSegment.OnParentMove(nextPositionData.Value.distanceSinceStart);
            }
        }
    }
}

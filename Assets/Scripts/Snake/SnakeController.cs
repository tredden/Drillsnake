using System.Collections;
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
    public int drillRadius;
    public Vector2Int drillOffset;
    public float drillHardness;
    public float maxDrillHeat;
    public float heatRechargeDelay;
}


[System.Serializable]
public enum SnakeState
{
    Alive,
    Exploding,
    Dead,
    Shopping,
}

public delegate void OnGoldGained(int newGoldAmount);
public delegate void OnDepthChanged(float newDepth);
public delegate void OnSegmentExploded(int totalSegments, int segmentsLeft, float goldPending);
public delegate void OnDeath();

public class SnakeController : MonoBehaviour
{
    [SerializeField]
    public DrillStats drillStats;
    [SerializeField]
    private int segments = 5;
    [SerializeField]
    public float maxSpeed = 128f;
    
    [SerializeField]
    private float acceleration = 48f;

    [SerializeField]
    public float turnSpeed = 180f;
    
    [SerializeField]
    private GameObject headPrefab;
    [SerializeField]
    private GameObject bodyPrefab;
    [SerializeField]
    private GameObject explosionPrefab;
    private List<GameObject> snakeSegments = new List<GameObject>();
    Collider2D drillCollider;
    private ControlInputs controlInputs;
    public float speed = 0f;
    public SnakeState state = SnakeState.Alive;
    public float deathTime = 0f;

    // The distance traveled since the snake started
    private float distanceSinceStart = 0f;
    [SerializeField]
    public float currentHeat = 0;
    [SerializeField]
    float deltaHeat;
    float timeSinceHeatDamage = 0f;

    public float gold = 0;
    public float goldMult = 0.1f;

    public OnGoldGained onGoldGained;
    public OnDepthChanged onDepthChanged;
    public OnSegmentExploded onSegmentExploded;
    public OnDeath onDeath;

    public float shaking = 1f; // Segments read this to shake

    // Start is called before the first frame update
    void Start()
    {
		SetLength(segments);
        Reset();
    }

    // Update is called once per frame
    void Update()
    {
        if (state == SnakeState.Dead || state == SnakeState.Exploding || state == SnakeState.Shopping) {
            speed = 0;
            if(gameObject.GetComponent<PlayerController>()!=null){
                snakeSegments[0].GetComponentInChildren<Animator>().SetBool("isDrilling",false);
            }
            return;
        }
        if(gameObject.GetComponent<PlayerController>()!=null){
            snakeSegments[0].GetComponentInChildren<Animator>().SetBool("isDrilling",true);
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
        this.onDepthChanged.Invoke(position.y);

        if (snakeSegments.Count == 0) return;

        SnakeSegment segment = snakeSegments[0].GetComponent<SnakeSegment>();
        segment.positionQueue.Enqueue(new PositionData
        {
            position = new Vector2(position.x, position.y),
            rotation = transform.eulerAngles.z,
            distanceSinceStart = distanceSinceStart,
        });
        segment.OnParentMove(distanceSinceStart);

        // Carve the map
        Vector3 drillCoord = transform.TransformPoint(
            new Vector3(drillStats.drillOffset.x, drillStats.drillOffset.y, 0f));
        MapGenerator map = MapGenerator.GetInstance();
        if (map != null) {
            CarveResults results = map.CarveMap(
                Mathf.RoundToInt(drillCoord.x), Mathf.RoundToInt(drillCoord.y),
                 drillStats.drillRadius, drillCollider, drillStats.drillHardness);

            // Elongate from gold
            float addedGold = results.totalGold * goldMult;
            if (addedGold > 0) {
                gold += addedGold;
                this.SetLength(Mathf.Max(segments, (int)(gold / 100f) + 3));
                // broadcast change for player controller or UI
                onGoldGained.Invoke((int)gold);
            }

            deltaHeat = (results.averageThickness - drillStats.drillHardness) + (results.maxThickness - drillStats.drillHardness) * 4f;
            if (deltaHeat > 0f) {
                timeSinceHeatDamage = 0f;
            }
            if (timeSinceHeatDamage < drillStats.heatRechargeDelay) {
                deltaHeat = Mathf.Max(0f, deltaHeat);
            }
            currentHeat = Mathf.Clamp(currentHeat + deltaHeat, 0f, drillStats.maxDrillHeat);
            timeSinceHeatDamage += Time.deltaTime;

            if (currentHeat >= drillStats.maxDrillHeat) {
                StartExplode();
            }
        }
    }

    private void AppendSegments(int add)
    {
        int count = snakeSegments.Count;
        for (int i = count; i < count + add; i++)
        {
            Transform parentTransform = i > 0 ? snakeSegments[i - 1].transform : transform;
            GameObject prefab = i == 0 ? headPrefab : bodyPrefab;
            GameObject segment = Instantiate(prefab, parentTransform.position, parentTransform.rotation);
            segment.GetComponent<SnakeSegment>().owner = this;
            snakeSegments.Add(segment);
            if (i == 0) {
                drillCollider = segment.transform.GetChild(0).GetComponent<Collider2D>();
            }

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

    public void HandleCollision(Collider2D collision)
    {
        Debug.Log("Collision detected with: " + collision.gameObject.name);
		collision.gameObject.TryGetComponent(out SnakeSegment segment);
        if (segment == null) return;
        // Check if the snake collided with itself
        if (segment.owner == this)
        {
            // Explode the snake
            Debug.Log("Snake collided with itself!");
            StartExplode();
        }
    }

    public void StartExplode()
    {
        StartCoroutine(Explode());
    }

    public IEnumerator Explode() {
		// Explodes the snake from the head down through its tail over time.
		if (state == SnakeState.Dead) yield break;

        state = SnakeState.Exploding;
        float delay;
        int totalSegments = snakeSegments.Count;
        for (int i = 0; i < totalSegments; i++)
        {
            delay = 1f/(i+1);
            Debug.Log(delay);
            yield return new WaitForSeconds(delay);
            Instantiate(explosionPrefab,snakeSegments[i].transform.position,Quaternion.identity);
            snakeSegments[i].SetActive(false);
            onSegmentExploded.Invoke(totalSegments, totalSegments - i - 1, gold);
        }
        // TODO: Explode
        deathTime = Time.time;
        state = SnakeState.Dead;
        Debug.Log("Snake exploded!");

        onDeath.Invoke();
    }

    public void Reset() {
        // Reset the snake to its initial state
        state = SnakeState.Alive;
        distanceSinceStart = 0f;
        speed = 0f;
        currentHeat = 0f;
        timeSinceHeatDamage = drillStats.heatRechargeDelay + 1f;

        foreach (GameObject segment in snakeSegments)
        {
            Destroy(segment);
        }
        snakeSegments.Clear();
        SetLength(segments);
    }
}

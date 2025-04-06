using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    int drillRadius = 8;
    [SerializeField]
    Vector2Int drillOffset;

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
        controlInputs.targetSpeed = 1 + Input.GetAxis("Vertical");
        
        snakeController.SetControlInputs(controlInputs);

        // TODO: Move this into the SnakeController?
        Vector3 drillCoord = transform.TransformPoint(new Vector3(drillOffset.x, drillOffset.y, 0f));
        MapGenerator map = MapGenerator.GetInstance();
        if (map != null) {
            CarveResults results = map.CarveMap(
                Mathf.RoundToInt(drillCoord.x), Mathf.RoundToInt(drillCoord.y), drillRadius);

            // Elongate from gold
            gold += Mathf.RoundToInt(results.totalGold);
            snakeController.SetLength(gold / 100 + 3);

            // Explode when hitting rocks
            DrillStats drillStats = snakeController.drillStats;
            if (results.maxThickness > drillStats.drillHardness) {
				snakeController.Explode();
            }
        }
    }
}

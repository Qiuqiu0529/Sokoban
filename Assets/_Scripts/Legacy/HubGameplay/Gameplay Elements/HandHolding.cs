using UnityEngine;

public class HandHolding : MonoBehaviour
{

    [SerializeField] GameObject PlayerToFollow;
    private float CurrentDistance;
    private bool holdingHands = false;

    [SerializeField] float DistanceToKeep = 1.0f;
    [SerializeField] float MovementSpeed = 5.0f;

    [SerializeField] float HandHoldingRange = 2.0f;


    // Update is called once per frame
    void Update()
    {
        CurrentDistance = Vector3.Distance(transform.position, PlayerToFollow.transform.position);


        if (Input.GetKeyDown(KeyCode.H) && CurrentDistance <= HandHoldingRange)
        {
            holdingHands = !holdingHands;
            Debug.Log(holdingHands ? "Holding hands :)" : "Hand holding stopped :("); // Shoutout to chatgpt for teaching me about ternary operators! 
        }

        if (holdingHands && CurrentDistance > DistanceToKeep)
        {
            transform.position = Vector3.MoveTowards(transform.position, PlayerToFollow.transform.position, MovementSpeed * Time.deltaTime);
        }
    }

    // source: https://www.youtube.com/watch?v=sUM4B1Tfkes&ab_channel=BudGames dap me up BudGames!!
}

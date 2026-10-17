using System.Collections.Generic;
using Sirenix.Utilities;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    public static List<GameState> savedStates = new List<GameState>();

    void Start()
    {
        Debug.Log("GameManager started");
        SaveGameState();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z)) UndoMove();
		if (Input.GetKeyDown(KeyCode.R)) ResetLevel();
	}

    public static void SaveGameState()
    {
        Debug.Log("Saving GameState");
        savedStates.Add(GameState.GetCurrentState());
        // foreach(var x in savedStates[savedStates.Count-1].pushableTrails[savedStates[savedStates.Count-1].pushableTrails.Count-1])
        // {
        //             Debug.Log(x.position);
        // }

    }

    public static void UndoMove()
    {
        Debug.Log("UndoMove called. Saved states: " + savedStates.Count);

        if (savedStates.Count <= 1)
        {
            Debug.Log("No moves to undo");
        }
        else
        {
            savedStates[savedStates.Count - 1].LoadGameState();
            savedStates.RemoveAt(savedStates.Count - 1);
        }
    }

    public static void ResetLevel()
    {
		if (savedStates.Count <= 1)
		{
			Debug.Log("No moves to undo");
		}
		else
		{
			savedStates[0].LoadGameState();
			savedStates.RemoveRange(1,savedStates.Count - 1);
		}
	}
}
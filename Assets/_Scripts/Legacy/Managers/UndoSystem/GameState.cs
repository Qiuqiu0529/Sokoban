using System;
using System.Collections.Generic;
using Language.Lua;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class GameState
{
    public Vector3 playerPos;
    public List<Vector3> spiritPositions;
    public List<Vector3> pushablePositions;
    public List<List<Transform>> pushableTrails; 
    public List<Vector3> pumpkinPositions;
    public List<List<Transform>> pumpkinTrails;
    public List<Vector3> scareCrowPositions;
    public List<List<Transform>> scareCrowTrails;
    public List<Vector3> barrelPositions;
    public List<List<Transform>> barrelTrails;
    public List<bool> barrelStandings;
    public List<Vector3> logPositions;
    public List<List<Transform>> logTrails;
    public List<bool> logStandings;
    public List<Material> groundMats;

    public bool isPossessing;
    public Vector3 possessedObjectPos;

    public static GameState GetCurrentState()
    {
        GameState gameStateToSave = new GameState();
        gameStateToSave.spiritPositions = new List<Vector3>();
        gameStateToSave.pushablePositions = new List<Vector3>();
        gameStateToSave.pushableTrails = new List<List<Transform>>();        
        gameStateToSave.pumpkinPositions = new List<Vector3>();
        gameStateToSave.pumpkinTrails = new List<List<Transform>>();
        gameStateToSave.scareCrowPositions = new List<Vector3>();
        gameStateToSave.scareCrowTrails = new List<List<Transform>>();
        gameStateToSave.barrelPositions = new List<Vector3>();
        gameStateToSave.barrelTrails = new List<List<Transform>>();
        gameStateToSave.barrelStandings = new List<bool>();
        gameStateToSave.logPositions = new List<Vector3>();
        gameStateToSave.logTrails = new List<List<Transform>>();
        gameStateToSave.logStandings = new List<bool>();

        // FindObjectOfType is deprecated, please change this c:

        PlayerGridController player = GameObject.FindObjectOfType<PlayerGridController>();
        gameStateToSave.isPossessing = player.isPossessing;

        gameStateToSave.groundMats = new List<Material>();
        foreach(var groundMat in GameObject.FindGameObjectsWithTag("Ground"))
        {
            gameStateToSave.groundMats.Add(groundMat.transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial);
        }

        if (player.isPossessing)
        {
            gameStateToSave.possessedObjectPos = player.controllingObject.position;
        }
        else
        {
            gameStateToSave.playerPos = player.transform.position;
        }

        SavedElement[] elementsToSaveOnScene = GameObject.FindObjectsOfType<SavedElement>();
        foreach (SavedElement element in elementsToSaveOnScene)
        {
            if (element.type == SavedElement.Type.NPCSpirit)
            {
                gameStateToSave.spiritPositions.Add(element.transform.position);
            }
            else if (element.type == SavedElement.Type.Possessable)
            {
                if(element.GetComponent<PumpkinMoveOld>() != null)
                {
                    GetCurrentPumpkinState(gameStateToSave, element);
                }
                else if(element.GetComponent<BarrelMoveOld>() != null)
                {
                    GetCurrentBarrelState(gameStateToSave, element);
                }
                else if(element.GetComponent<LogMove>() != null)
                {
                    GetCurrentLogState(gameStateToSave, element);
                }
                else if(element.GetComponent<ScareCrow>() != null)
                {
                    GetCurrentScareScrowState(gameStateToSave, element);
                }
            }
            else if (element.type == SavedElement.Type.Pushable)
            {
                GetCurrentPushableState(gameStateToSave, element);
            }
        }

        return gameStateToSave;
    }

    static void GetCurrentPumpkinState(GameState gameStateToSave, SavedElement element)
    {
        gameStateToSave.pumpkinPositions.Add(element.transform.position);
        List<Transform> tmp = new List<Transform>();
        tmp.AddRange(element.GetComponent<PumpkinMoveOld>().trailGrounds);
        gameStateToSave.pumpkinTrails.Add(tmp);
    }
    static void GetCurrentScareScrowState(GameState gameStateToSave, SavedElement element)
    {
        gameStateToSave.scareCrowPositions.Add(element.transform.position);
        List<Transform> tmp = new List<Transform>();
        tmp.AddRange(element.GetComponent<ScareCrow>().trailGrounds);
        gameStateToSave.scareCrowTrails.Add(tmp);
    }

    static void GetCurrentBarrelState(GameState gameStateToSave, SavedElement element)
    {
        gameStateToSave.barrelPositions.Add(element.transform.position);
        List<Transform> tmp = new List<Transform>();
        tmp.AddRange(element.GetComponent<BarrelMoveOld>().trailGrounds);
        gameStateToSave.barrelTrails.Add(tmp);
        gameStateToSave.barrelStandings.Add(element.GetComponent<BarrelMoveOld>().isStanding);
    }
    static void GetCurrentLogState(GameState gameStateToSave, SavedElement element)
    {
        gameStateToSave.logPositions.Add(element.transform.position);
        List<Transform> tmp = new List<Transform>();
        tmp.AddRange(element.GetComponent<LogMove>().trailGrounds);
        gameStateToSave.logTrails.Add(tmp);
        gameStateToSave.logStandings.Add(element.GetComponent<LogMove>().isStanding);
    }
 
    static void GetCurrentPushableState(GameState gameStateToSave, SavedElement element)
    {
        gameStateToSave.pushablePositions.Add(element.transform.position);
        List<Transform> tmp = new List<Transform>();
        tmp.AddRange(element.GetComponent<Pushable>().trailGrounds);
        gameStateToSave.pushableTrails.Add(tmp);
    }

    public void LoadGameState()
    {
        Debug.Log("Loading GameState");

        PlayerGridController player = GameObject.FindObjectOfType<PlayerGridController>();

        if (isPossessing)
        {
             // Re-possess object
            player.controllingObject.position = possessedObjectPos;
            if(!player.isPossessing) player.Possess(GameObject.FindGameObjectWithTag("Possessable").transform);
        }
        else
        { // Reset to normal state
            if(player.isPossessing) player.DePossess();
			player.transform.position = playerPos;
		}

		SavedElement[] elementsToLoadOnScene = GameObject.FindObjectsOfType<SavedElement>();
        List<Vector3> remainingSpiritPositions = new List<Vector3>(spiritPositions);
        List<Vector3> remainingPumpkinPositions = new List<Vector3>(pumpkinPositions);
        List<List<Transform>> remainingPumpkinTrails = new List<List<Transform>>(pumpkinTrails);
        List<Vector3> remainingScareCrowPositions = new List<Vector3>(scareCrowPositions);
        List<List<Transform>> remainingScareCrowTrails = new List<List<Transform>>(scareCrowTrails);
        List<Vector3> remainingBarrelPositions = new List<Vector3>(barrelPositions);
        List<bool> remainingBarrelStandings = new List<bool>(barrelStandings);
        List<List<Transform>> remainingBarrelTrails = new List<List<Transform>>(barrelTrails);
        List<Vector3> remainingLogPositions = new List<Vector3>(logPositions);
        List<bool> remainingLogStandings = new List<bool>(logStandings);
        List<List<Transform>> remainingLogTrails = new List<List<Transform>>(logTrails);
        List<Vector3> remainingPushablePositions = new List<Vector3>(pushablePositions);
        List<List<Transform>> remainingPushableTrails = new List<List<Transform>>(pushableTrails);

        foreach (SavedElement elementToLoad in elementsToLoadOnScene)
        {
            if (elementToLoad.type == SavedElement.Type.NPCSpirit)
            {
                elementToLoad.transform.position = remainingSpiritPositions[0];
                remainingSpiritPositions.RemoveAt(0);
            }
            else if(elementToLoad.type == SavedElement.Type.Possessable)
            {
                if(elementToLoad.GetComponent<PumpkinMoveOld>())
                {
                    LoadPumpkinState(elementToLoad, remainingPumpkinPositions, remainingPumpkinTrails);
                }
                else if(elementToLoad.GetComponent<BarrelMoveOld>())
                {
                    LoadBarrelState(elementToLoad, remainingBarrelPositions, remainingBarrelTrails, remainingBarrelStandings);
                }
                else if(elementToLoad.GetComponent<LogMove>())
                {
                    LoadLogState(elementToLoad, remainingLogPositions, remainingLogTrails, remainingLogStandings);
                }
                else if(elementToLoad.GetComponent<ScareCrow>())
                {
                    LoadScareCrowState(elementToLoad, remainingScareCrowPositions, remainingScareCrowTrails);
                }

            }
            else if(elementToLoad.type == SavedElement.Type.Pushable)
            {
                elementToLoad.transform.position = remainingPushablePositions[0];
                remainingPushablePositions.RemoveAt(0);

                Pushable tmp = elementToLoad.GetComponent<Pushable>();
                tmp.ResetTrailGrounds(remainingPushableTrails[0]);
                remainingPushableTrails.RemoveAt(0);
            }
        }

        GameObject[] groundMatsToLoad = GameObject.FindGameObjectsWithTag("Ground");
        for(int i = 0; i < groundMatsToLoad.Length; i++)
        {
            groundMatsToLoad[i].transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = groundMats[i];
        }
    }

    static void LoadPumpkinState(SavedElement elementToLoad, List<Vector3> remainingPumpkinPositions, List<List<Transform>> remainingPumpkinTrails)
    {
        elementToLoad.transform.position = remainingPumpkinPositions[0];
        remainingPumpkinPositions.RemoveAt(0);

        PumpkinMoveOld tmpPumpkinMove = elementToLoad.GetComponent<PumpkinMoveOld>();
        tmpPumpkinMove.StopRolling();
        tmpPumpkinMove.ResetTrailGrounds(remainingPumpkinTrails[0]);
        remainingPumpkinTrails.RemoveAt(0);
    }
    static void LoadScareCrowState(SavedElement elementToLoad, List<Vector3> remainingScareCrowPositions, List<List<Transform>> remainingScareCrowTrails)
    {
        elementToLoad.transform.position = remainingScareCrowPositions[0];
        remainingScareCrowPositions.RemoveAt(0);

        ScareCrow tmpScareCrowMove = elementToLoad.GetComponent<ScareCrow>();
        tmpScareCrowMove.StopRolling();
        tmpScareCrowMove.ResetTrailGrounds(remainingScareCrowTrails[0]);
        remainingScareCrowTrails.RemoveAt(0);
    }

    static void LoadBarrelState(SavedElement elementToLoad, List<Vector3> remainingBarrelPositions, 
                                List<List<Transform>> remainingBarrelTrails, List<bool> remainingBarrelStandings)
    {
        elementToLoad.transform.position = remainingBarrelPositions[0];
        remainingBarrelPositions.RemoveAt(0);

        BarrelMoveOld tmpBarrelMove = elementToLoad.GetComponent<BarrelMoveOld>();
        tmpBarrelMove.StopRolling();
        tmpBarrelMove.ResetTrailGrounds(remainingBarrelTrails[0]);
        remainingBarrelTrails.RemoveAt(0);
        tmpBarrelMove.isStanding = remainingBarrelStandings[0];
        remainingBarrelStandings.RemoveAt(0);
        tmpBarrelMove.Reset();
    }

    static void LoadLogState(SavedElement elementToLoad, List<Vector3> remainingLogPositions, 
                                List<List<Transform>> remainingLogTrails, List<bool> remainingLogStandings)
    {
        elementToLoad.transform.position = remainingLogPositions[0];
        remainingLogPositions.RemoveAt(0);

        LogMove tmpLogMove = elementToLoad.GetComponent<LogMove>();
        tmpLogMove.StopRolling();
        tmpLogMove.ResetTrailGrounds(remainingLogTrails[0]);
        remainingLogTrails.RemoveAt(0);
        tmpLogMove.isStanding = remainingLogStandings[0];
        remainingLogStandings.RemoveAt(0);
        tmpLogMove.Reset();
    }
}

using UnityEngine;

public class SavedElement : MonoBehaviour
{
    // Attach this to the prefabs of objects u want saved in scenes
    public enum Type { NPCSpirit, Player, Possessable, Pushable };   // Box is just any interactable element
    public Type type;                   // Assign me in the editor
}

// you still need to add the following line to objects u want saved though
//         GameManager.SaveGameState();

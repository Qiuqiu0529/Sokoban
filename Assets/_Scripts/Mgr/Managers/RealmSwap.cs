using UnityEngine;

public class RealmSwap : MonoBehaviour
{
    [SerializeField] Transform SpiritRealmTeleporterPos;
    [SerializeField] Transform HumanRealmTeleporterPos;
    private GameObject Player;
    private CharacterController controller;
    [SerializeField] float TeleportThreshold = 1.0f;
    [SerializeField] float TeleportCooldown = 3.0f;
    private float LastTeleportTime = 0.0f;

    void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player");
        controller = Player.GetComponent<CharacterController>();

        if (Player == null)
        {
            Debug.LogError("Player not found, check fior 'Player' tag");
        }
        else
        {
            Debug.Log("Player found.");
        }
    }

    void Update()
    {
        if (Time.time < LastTeleportTime + TeleportCooldown)
        {
            return;
        }

        if (Vector3.Distance(Player.transform.position, SpiritRealmTeleporterPos.position) < TeleportThreshold)
        {
            Debug.Log("Player will teleport TO HUMAN realm in " + TeleportCooldown);
            controller.enabled = false;
            Player.transform.position = HumanRealmTeleporterPos.position;
            controller.enabled = true;
            LastTeleportTime = Time.time;
        }

        else if (Vector3.Distance(Player.transform.position, HumanRealmTeleporterPos.position) < TeleportThreshold)
        {
            Debug.Log("Player will teleport TO SPIRIT realm in " + TeleportCooldown);
            controller.enabled = false;
            Player.transform.position = SpiritRealmTeleporterPos.position;
            controller.enabled = true;
            LastTeleportTime = Time.time;
        }
    }

}

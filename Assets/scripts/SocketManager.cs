using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using Newtonsoft.Json.Linq;

public class SocketManager : MonoBehaviour
{
    WebSocket socket;
    public GameObject playerPrefab;
    public Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();
    public PlayerData playerData;

    // Start is called before the first frame update
    void Start()
    {
        socket = new WebSocket("ws://localhost:8080");
        socket.Connect();

        // WebSocket onMessage function
        socket.OnMessage += (sender, e) =>
        {
            if (e.IsText)
            {
                JObject jsonObj = JObject.Parse(e.Data);

                if (jsonObj["type"] != null && jsonObj["type"].ToString() == "playerData")
                {
                    JObject playersObj = (JObject)jsonObj["players"];
                    foreach (var player in playersObj)
                    {
                        string playerId = player.Key;
                        JObject playerInfo = (JObject)player.Value;

                        if (playerId == playerData.id)
                        {
                            // Skip updating local player
                            continue;
                        }

                        // If player doesn't exist in local game, create it
                        if (!players.ContainsKey(playerId))
                        {
                            GameObject newPlayer = Instantiate(playerPrefab);
                            players[playerId] = newPlayer;
                        }

                        // Update player position
                        Vector3 newPos = new Vector3(
                            playerInfo["xPos"].ToObject<float>(),
                            playerInfo["yPos"].ToObject<float>(),
                            playerInfo["zPos"].ToObject<float>()
                        );
                        players[playerId].transform.position = newPos;
                    }

                    return;
                }

                // Handling new player ID from server
                if (jsonObj["id"] != null)
                {
                    playerData.id = jsonObj["id"].ToString();
                    Debug.Log("Player ID is " + playerData.id);
                    return;
                }
            }
        };

        socket.OnClose += (sender, e) =>
        {
            Debug.Log(e.Code);
            Debug.Log(e.Reason);
            Debug.Log("Connection Closed!");
        };
    }

    void Update()
    {
        if (socket == null) return;

        if (playerPrefab != null && playerData.id != "")
        {
            // Update local player's position
            playerData.xPos = playerPrefab.transform.position.x;
            playerData.yPos = playerPrefab.transform.position.y;
            playerData.zPos = playerPrefab.transform.position.z;

            System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            double timestamp = (System.DateTime.UtcNow - epochStart).TotalSeconds;
            playerData.timestamp = timestamp;

            string playerDataJSON = JsonUtility.ToJson(playerData);
            socket.Send(playerDataJSON);
        }
    }

    private void OnDestroy()
    {
        socket.Close();
    }
}

// [Serializable]
// public struct PlayerData
// {
//     public string id;
//     public float xPos;
//     public float yPos;
//     public float zPos;
//     public double timestamp;
// }

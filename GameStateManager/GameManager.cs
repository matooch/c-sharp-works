using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

public class GameManager : MonoBehaviour
{
    // GameState Logic. Uses GameMode to Handle this
    public List<GameObject> gameStateObjects = new List<GameObject>();
    private List<IGameState> gameStates = new List<IGameState>(); // With a serialized list, we can add the conponents directly in editor, as long as their order matches the Enum (GameMode)
    private IGameState _currentState;

    private void Start() 
    {
        // Initalize the gameStateObjects as the Interface object
        for(int i = 0; i < gameStateObjects.Count; i++)
        {
            gameStates.Add(gameStateObjects[i].GetComponent<IGameState>());
            gameStateObjects[i].SetActive(false);
        }
    }

    // This is basically the only function we would need to interact and change the states. If we Implement the logic on the appropreate or newly added manager,
    // then we can have all their respective code run as they need it.
    // (we can also make a version of this that takes in an int and then just cast that to the enum -- This would be relevant for buttons in the UI as they cannont
    // take enums as arguments in editor unfortuntely)
    [Button("Change Game State")]
    public void ChangeGameState(int value)
    {
        ChangeState((GameMode)value);
    }

    public void ChangeState(GameMode newState)
    {
        if (gameStates[(int)newState] != null)
        {
            // start fake Load animation
            if (newState != GameMode.TitleScreen)
            {
                SceneChangeFader.instance.SetFader(true);

                // wait and quit fake Load animation
                StartCoroutine(WaitForContentToFullyLoad());
            }
            // Host sync game state as room property
            if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.IsMasterClient)
            {
                Hashtable hash = new Hashtable { { "GameModeState", (int)newState } };
                PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
            }

            _currentState?.UnloadContent();
            _currentState = gameStates[(int)newState];
            gameMode = newState;
            // load a loading screen image,
            // Have host raise events for Photon syncing, Seeds, room generation, 
            _currentState.LoadContent();
        }
    }
}
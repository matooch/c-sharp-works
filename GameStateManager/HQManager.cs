using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class HQManager : MonoBehaviour, IGameState
{
    public void LoadContent()
    {
        gameObject.SetActive(true);
        
        if (GameManager.isOnline && !PhotonNetwork.IsConnected)
        {
            StartCoroutine(StartNetworkConnection());
            return;
        }
        PlayerManager.instance.SpawnCharacter();
        PlayerManager.instance.PostitionHumanPlayers();
        
        if (PhotonNetwork.IsConnectedAndReady)
        {
            GameManager.instance.isNetworkLoadedReady = true;
        }
    }

    public void UnloadContent()
    {
        gameObject.SetActive(false);
        //PlayerManager.instance.EnableHumanPlayers(false);

        if(GameManager.instance.spaceMarine != null)
        {
            GameManager.instance.spaceMarine.overlay.hordeModeMenu.isClicked = false;
        }
    }

    private IEnumerator StartNetworkConnection()
    {
        WaitForSeconds waiter = new WaitForSeconds(0.1f);
        
        Debug.Log($"StartNetworkConnection");
        ConnectionToPhotonAndAuthentication.Instance.connectedToNetwork = true;
        ConnectionToPhotonAndAuthentication.OnStartPhotonAndPlayfabConnection?.Invoke();
        ConnectionToPhotonAndAuthentication.Instance.isInitialized = true;

        Debug.Log($"connecting to photon");
        /*while (!PhotonNetwork.IsConnected && !PhotonNetwork.InLobby)
        {
            yield return waiter;
        }*/
        
        yield return new WaitUntil( () => PhotonNetwork.IsConnected && PhotonNetwork.InLobby);
        ConnectionToPhotonAndAuthentication.Instance.CreateRoomAndConnectToIt();

        Debug.Log($"connecting to room");
        yield return new WaitUntil( () => PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom);

        /*while (!PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom)
        {
            yield return waiter;
        }*/
        
        Debug.Log($"connected");
        
        PlayerManager.instance.SpawnCharacter();
        PlayerManager.instance.PostitionHumanPlayers();
        
        
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.IsMasterClient)
        {
            ExitGames.Client.Photon.Hashtable hash = new ExitGames.Client.Photon.Hashtable { { "GameModeState", (int)GameManager.GameMode.HQ } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
        }
        
        if (PhotonNetwork.IsConnectedAndReady)
        {
            GameManager.instance.isNetworkLoadedReady = true;
        }
        yield return null;
    }

    // Here is an example of a function we could run on say the 'LoadHordeMode (GB)' interaction in a Load GB menu
    // From here the GameManager would unload the active state (HQ) and then load the next state that is called (Horde)
    // HQ would unload its content (HQ Prefab, change any Photon data to not be joinable) then Load the HordeModeManager content (Room Generator, and setup all that stuff as if the scene is loading
    public void LoadHordeMode() => GameManager.instance.ChangeState(GameManager.GameMode.Horde);
    public void ExitToTitle() => GameManager.instance.ChangeState(GameManager.GameMode.TitleScreen);
}
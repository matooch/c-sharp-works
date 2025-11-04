using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;


public class Portal : MonoBehaviour
{
    public InputChecker inputChecker;
    private Transform spawnPoint;
    private SpaceMarine spaceMarine;
    private bool isActive;
    public void Init(Room injectionRoom)
    {
        // Portal requires where to send the player(s) when its activated
        // we will need a spawn point from a room
        // Maybe thr room Generator should go in reverse since portals can only send up forward in a room (if back then thats a problem too?)

        if(injectionRoom != null)
        {
            spawnPoint = injectionRoom.playerSpawnPoint;
        }

        gameObject.SetActive(false);
    }

    public void Enable(KeyCard requiredKeyCard)
    {
        
        if(!PhotonNetwork.IsConnectedAndReady || PhotonNetwork.IsConnectedAndReady && GameManager.instance.mainPlayer.photonView.Controller.IsMasterClient)
        {
            bool active = GameManager.instance.mainPlayer.playerData.characterData.keycards[(int)requiredKeyCard];
            // Send RPC to all Clients to also spawn their portal
            
            gameObject.SetActive(active);
            isActive = active;
            if(PhotonNetwork.IsConnectedAndReady) RaiseEventEnablePortal(active);
            
        }
    }

    public void EnableEvent(object[] data)
    {
        gameObject.SetActive((bool)data[0]);
        isActive = (bool)data[0];
    }

    private void TriggerPortal()
    {
        if(isActive)
        {
            HordeModeManager.current.roomGenerator.SetPosition(-spawnPoint.position);
            PlayerManager.instance.PostitionHumanPlayers();
            HordeModeManager.current.ClearTrackedLoot();
            HordeModeManager.current.roomGenerator.OnAllRoomsGenerated();

            if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.IsMasterClient)
            {
                RaiseEventTriggerPortal();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player")
        {
            inputChecker.TriggerAnimation(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.tag == "Player")
        {
            inputChecker.TriggerAnimation(false);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if(other.tag == "Player")
        {
            if(spaceMarine == null)
            {
                spaceMarine = other.GetComponent<SpaceMarine>();
            }
            else
            {
                if(spaceMarine.player.GetButtonDown("Interact"))
                {
                    if(!PhotonNetwork.IsConnectedAndReady || (PhotonNetwork.IsConnectedAndReady && GameManager.instance.mainPlayer.photonView.Controller.IsMasterClient))
                    {
                        TriggerPortal();
                    }
                }
            }
            
        }
    }

    public void RaiseEventEnablePortal(bool active)
    {
        object[] package = new object[] {active};

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.EnablePortal,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others, CachingOption = EventCaching.DoNotCache},
            new ExitGames.Client.Photon.SendOptions { Reliability = true } //CH - used to be Others on ReceiverGroup, changing to test something.
        );
    }

    public void RaiseEventTriggerPortal()
    {
        //object[] package = new object[0];

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.TriggerPortal,
            /*package*/null,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others, CachingOption = EventCaching.DoNotCache},
            new ExitGames.Client.Photon.SendOptions { Reliability = true } //CH - used to be Others on ReceiverGroup, changing to test something.
        );
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code >= 200) return;
        
        if (!PhotonNetwork.LocalPlayer.IsLocal) return;

        EventCodes e = (EventCodes) photonEvent.Code;
        object[] o = (object[]) photonEvent.CustomData;

        if (e != EventCodes.EnablePortal)
        {
            //return;
            //Debug.Log($"OnEvent HordeModeSpawner {(EventCodes)photonEvent.Code} {photonEvent.Sender}");
        }

        switch (e)
        {
            case EventCodes.EnablePortal:
                EnableEvent(o);
            break;
            case EventCodes.TriggerPortal:
                TriggerPortal();
            break;
        }
    }
}
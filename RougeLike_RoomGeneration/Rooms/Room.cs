using System;
using System.Collections;
using System.Collections.Generic;
using Com.LuisPedroFonseca.ProCamera2D;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider))]
public class Room : MonoBehaviour, IOnEventCallback
{   
    [Header("Room Defaults")]
    public RoomType roomType = RoomType.EnemyWave;
    public float difficultyFactor;
    public int roomID;
    public bool roomInitialized, roomActive, roomCleared;
    public int genomeBucksOnCompletion; // Probably could set this as a function to centralize the logic
    public Transform playerSpawnPoint;
    public GameObject[] barriers;
    public UnityEvent roomEndEvent; 
    [System.Serializable]
    public class Exit
    {
        public Transform point;
        public Direction direction;
        public enum Direction {None = -1, Up, Down, Left, Right};
    }
    public Exit[] exits;
    public PhotonView[] networkObjectsToInitialize;
    private List<GameObject> playersInsideHordeRoom = new List<GameObject>();
    [HideInInspector] public Room injectionRoom = null;

    public virtual void Init(int _roomID)
    {
        roomID = _roomID;
	      
	    roomInitialized = true;

        if (PhotonNetwork.LocalPlayer.IsLocal && PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            if (networkObjectsToInitialize != null)
            {
                int count = 0;
                foreach (var item in networkObjectsToInitialize)
                {
                    //PhotonNetwork.AllocateRoomViewID(item);
                    Debug.Log($"AllocateRoomViewID {item.gameObject.name} {PhotonNetwork.AllocateRoomViewID(item)}");
                    RaiseEventInitRoomNetworkObjects(item.ViewID, roomID, count);
                    count++;
                }
            }
        }
    }

    public void InjectionSubRoom(Room _injectionRoom)
    {
        injectionRoom = _injectionRoom;
    }
    
    public virtual void StartRoom()
    {
        if(roomActive || roomCleared) return;

		roomActive = true;
		    
        EnableBarriers(true);

        HordeModeManager.current.hordeModeCanvasManager.SetRoomTitle((roomID/* * HordeModeManager.current.currentLoop*/).ToString());
        HordeModeManager.current.hordeModeCanvasManager.SetWaveInfo((HMRoomTitle)roomType, "");
        HordeModeManager.current.hordeModeCanvasManager.SetEnemyInfo("", false);

        Debug.Log("StartRoom Called " + gameObject.name);

        if(PhotonNetwork.IsConnectedAndReady && PhotonNetwork.LocalPlayer.IsLocal && PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            RaiseEventStartRoom(roomID);
        }

        // Will there be a need to have a external Event to call here?
        // Like have nuanced starting conditions that need to be serialized?
        // Not sure yet, would be easy to add
    }
    
    public virtual void EndRoom()
    {
        if(roomCleared) return;

        roomActive = false;
        roomCleared = true;
        
        roomEndEvent?.Invoke();

        // Add some UI to show an increase of GB
        GameManager.instance.mainPlayer.playerData.characterData.AddGenomeBucks(GetGenomeBucks());

        if(PhotonNetwork.IsConnectedAndReady && PhotonNetwork.LocalPlayer.IsLocal && PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            RaiseEventEndRoom(roomID);
        }
    }
    
    public void EnableBarriers(bool active)
    {
        // Enable and disable the barriers
        StartCoroutine(SetBarriers(active));
    }
    
    private IEnumerator SetBarriers(bool active)
    {
        yield return new WaitForSeconds(.1f);
        foreach (GameObject _bar in barriers)
        {
            _bar.SetActive(active);
        }
    }

    public int GetGenomeBucks()
    {   
        switch(roomType)
        {
            default:
            genomeBucksOnCompletion = 0;
            break;

            case RoomType.EnemyWave:
            case RoomType.SubWave:
            genomeBucksOnCompletion = 1;
            break;

            case RoomType.EliteChamber:
            case RoomType.SubElite:
            genomeBucksOnCompletion = 5;
            break;

            case RoomType.BossChamber:
            case RoomType.SubBoss:
            genomeBucksOnCompletion = 10;
            break;
        }
        return genomeBucksOnCompletion;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if(roomActive || roomCleared) return;

        if (other.tag == "Player")
        {
            if (!PhotonNetwork.IsConnectedAndReady)
            {
                StartRoom();
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if(roomActive || roomCleared) return;

        // TEST THIS HERE
        if(other.tag == "Player")
        {
            if(PhotonNetwork.IsConnectedAndReady)
            {
                if (!playersInsideHordeRoom.Contains(other.gameObject))
                {
                    playersInsideHordeRoom.Add(other.gameObject);
                }

                // Check to see if this is being called also by the client. For some reason, the client was able to trigger a start room, when tht 
                // should happen by the host and only the host
                // but some debugs on teh StartRoom, and maybe put the PhotonID on it so I knwo who did it
                if (GameManager.instance.mainPlayer.photonView.Controller.IsLocal && GameManager.instance.mainPlayer.photonView.Controller.IsMasterClient)
                {
                    if (playersInsideHordeRoom.Count == PhotonNetwork.CurrentRoom.PlayerCount)
                    {
                        StartRoom();
                    }

                }
            }
        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            if (other.tag == "Player" && (!roomActive || roomCleared))
            {
                if (playersInsideHordeRoom.Contains(other.gameObject))
                {
                    playersInsideHordeRoom.Remove(other.gameObject);
                }
            }
        }
    }

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    private void RaiseEventStartRoom(int _roomID)
    {
        object[] package = {_roomID};
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.StartRoom,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others, CachingOption = EventCaching.DoNotCache},
            new ExitGames.Client.Photon.SendOptions { Reliability = true }
        );
    }

    private void RaiseEventEndRoom(int _roomID)
    {
        object[] package = {_roomID};
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.EndRoom,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others, CachingOption = EventCaching.DoNotCache},
            new ExitGames.Client.Photon.SendOptions { Reliability = true }
        );
    }

    private void RaiseEventInitRoomNetworkObjects(int objectViewID, int roomID, int arrayIndex)
    {
        object[] package = new object[] { objectViewID, roomID, arrayIndex };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.InitializeRoomNetworkObjects,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All, CachingOption = EventCaching.AddToRoomCache},
            new ExitGames.Client.Photon.SendOptions { Reliability = true }
        );
    }

    private IEnumerator InitRoomNetworkObjectsEvent(object[] data)
    {
        WaitForSeconds waiter = new WaitForSeconds(0.1f);

        if ((int)data[1] == roomID)
        {
            networkObjectsToInitialize[(int)data[2]].ViewID = (int)data[0];
            
            Debug.Log($"Assigned ViewID: {networkObjectsToInitialize[(int)data[2]].ViewID} to {networkObjectsToInitialize[(int)data[2]].gameObject.name}");
        }
        
        yield return null;
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code >= 200) return;
        
        if (!PhotonNetwork.LocalPlayer.IsLocal) return;

        EventCodes e = (EventCodes) photonEvent.Code;
        object[] o = (object[]) photonEvent.CustomData;


        switch(e)
        {
            case EventCodes.InitializeRoomNetworkObjects:
                Debug.Log($"OnEvent HordeMode {(EventCodes)photonEvent.Code} {photonEvent.Sender}");
                StartCoroutine(InitRoomNetworkObjectsEvent(o));
            break;
            case EventCodes.StartRoom:
                if((int)o[0] != roomID) return;
                StartRoom();
            break;
            case EventCodes.EndRoom:
                if((int)o[0] != roomID) return;
                EndRoom();
            break;

            case EventCodes.SlowMotionFinalKill:
                GameManager.instance.SlowMotionKill();
                break;

            case EventCodes.SetBossForHordeMode:
                if((int)o[1] != roomID) return;
                ((BossRoom)this).SetBossForHordeModeEvent(o);
                break;
        }

    }
}

public enum RoomType 
{
    Starting, EnemyWave, EliteChamber, BossChamber, Shop, Story, SubStart, SubWave, SubElite, SubBoss, SubShop, SubStory
}



using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Sirenix.OdinInspector;
using Com.LuisPedroFonseca.ProCamera2D.TopDownShooter;
using System.Linq;

public class RoomGenerator : MonoBehaviourPunCallbacks, IOnEventCallback
{
//    public GameObject[] roomPrefabs; // Array of room prefabs
    [SerializeField]
    public RoomLayout testLayout;
    public bool useTestLayout = false;
    public GameObject startingRoomPrefab; // Starting room prefab
    public Transform startingRoomPosition; // Starting room position in the scene
//    public GameObject[] endRooms;
    public int numberOfRooms = 11; // Number of rooms to generate
    public int roomCount = 0;
    public int maxAttemptsPerRoom = 3; // Maximum retries for a room

    private int roomCounter = 0;  // Tracks room creation order
    public List<RoomDebuffSO> possibleRoomDebuffs; // assign these in the inspector

    private List<Room.Exit> availableExits = new List<Room.Exit>(); // List of unused exits
    public List<Room> rooms = new List<Room>();

    [System.Serializable]
    public class LoopRoomGroup
    {
        public GameObject[] roomPrefabs;
    }

    public List<LoopRoomGroup> loopRoomGroups = new List<LoopRoomGroup>();

    [System.Serializable]
    public class LoopEndRoomGroup
    {
        public GameObject[] endRoomPrefabs;
    }

    public List<LoopEndRoomGroup> loopEndRoomGroups = new List<LoopEndRoomGroup>();

    [SerializeField, ShowInInspector]
    private Dictionary<Room, int> recentRooms = new Dictionary<Room, int>();
    private List<GameObject> recentRoomHistory = new List<GameObject>();
    [SerializeField] private int roomRepeatBuffer = 2;
    public bool enableShopRoom = false;

    private BiomeSO biomeSO;

    #region Network

    public bool isStartingRoomInitiated = false;
    private List<Room.Exit> networkAvailableExits = new List<Room.Exit>();
    #endregion
    

    public int GetNextRoomIndex()
    {
        return roomCounter++;
    }

    public void ResetRoomCounter()
    {
        roomCounter = 0;

        int roomTotal = rooms.Count;

        if(roomTotal > 0)
        {            
            for(int i = roomTotal - 1; i >= 0; i--)
            {
                rooms[i].gameObject.SetActive(false);
                rooms[i].gameObject.transform.SetParent(null);
                
                // to clean photon views that can be locked and make errors on objects destroying
                if(PhotonNetwork.IsConnectedAndReady && rooms[i].networkObjectsToInitialize != null && rooms[i].networkObjectsToInitialize.Length > 0)
                    foreach (var item in rooms[i].networkObjectsToInitialize)
                    {
                        PhotonNetwork.LocalCleanPhotonView(item);
                    }
                
                Destroy(rooms[i].gameObject);
                //Debug.Log($"try to RemoveAt {rooms[i].name} at {i}");
                rooms.RemoveAt(i);
            }

        }

        rooms.Clear();
    }

    public void Init()
    {
        ResetRoomCounter();
        GenerateRooms();
    }

    public void GenerateRooms()
    {
        // Each biome / story has a scriptable object of all room types
            // EnemyWave, EliteWave, StartingRooms, Story, Boss, Shop, etc
            // Store BiomeType
            // Story Boss? Maybe that can be on the room itself

        // Take in the BiomeSO and use a List of RoomTypes for a pattern to build

        // Loop throug the 'pattern' to determine the length of how many rooms

        // Take the room type in the index of the pattern list, and grab from the proper room list prefab in BiomeSO
            // Then grab a 'random' room from that list of room types
            // Initalize the room and setup all its needed parameters

        // Get the BiomeSO based on which loop we are in
        // Current catch for we we dont have enough, but this will just stay at 0 if the current loop is higher
        // Will need to consider how we want to handle 'endless' mode
        // Probably a mod value for the length
        int loopIndex = (HordeModeManager.current.currentLoop - 1) % HordeModeManager.current.biomeSOs.Count;
        biomeSO = HordeModeManager.current.biomeSOs[loopIndex];

        int _totalRoomsToSpawn = biomeSO.defaultRoomLayout.TotalRooms();

        // Now we parse through the defaultRoomLayout for the room (Unless we want to have another we load in for specials)
        // For more special layouts, then we will need to alter the logic as we will not always have a portal then
        
        for(int i = 0; i < _totalRoomsToSpawn; i++)
        {
            // First thing we need to do is spawn all the rooms and store them to a list
            List<RoomType> roomPattern = GetRoomPattern(biomeSO, i);
            // We want to have the proper index of rooms, so we should make sure the subRoom list postion is considered if we are past all the main room pattern
            int index = i < biomeSO.defaultRoomLayout.roomPattern.Count ? i : i - biomeSO.defaultRoomLayout.roomPattern.Count;
            // Use I when interacting with the room list, use Index when interatcing with the RoomPattern list

            // for Reference in if statements
            // Now we spawn each room based on the room type in the roomPattern list

            // Could use rooms.Count to determine if we should start here, then when index == 0, we could setup a parent offset
            if(index == 0)
            {
                // Spawn the starting rooms
                rooms.Add(InstantiateRoom(biomeSO, roomPattern, index).GetComponent<Room>());

                if(i != 0)
                {
                    // this is the secondary rooms
                    // purely temp until we have this in a setup to teleport
                    rooms[i].transform.position += new Vector3(10000, 0, 0);
                }
                continue;
            }

            // Assumign we have a room that exists (as we should since a starting room was just recreated at the start)
            // We will take the last room in the rooms list, and use its exit

            Room.Exit previousRoomExit = rooms[i - 1].exits[0];
            Room.Exit matchingRoomEntrance = null;

            Room _tempRoom;

            // Now lets find a room that matches the roomPattern and has the correct exit
            // We loop through the prefabs (Not instantiating them first) and checking the exits/entrances to compare to what we need from the previous room
            // If that is a matching exit/entrance, then we exit the do while, and proceed with spawning the room and adding it to the list

            do{
                _tempRoom = biomeSO.roomGroups[(int)roomPattern[index]].roomPrefabs[Random.Range(0, biomeSO.roomGroups[(int)roomPattern[index]].roomPrefabs.Count)].GetComponent<Room>();
                matchingRoomEntrance = GetMatchingEntrance(_tempRoom, previousRoomExit.direction);

            }while(matchingRoomEntrance == null && !GetRecentRoom(_tempRoom));

            // Assuming this loop exits
            GameObject roomGO = InstantiateRoom(_tempRoom);
            Room room = roomGO.GetComponent<Room>();

            AlignRoom(previousRoomExit, matchingRoomEntrance, roomGO);
            rooms.Add(room);

            // Put the room ID for the room
            rooms[i].roomID = i;

            // Decrease each Recent Room value by one since they werent used
            foreach(Room value in recentRooms.Keys.ToList())
            {
                recentRooms[value] = recentRooms[value] - 1;

                if(recentRooms[value] <= 0)
                {
                    recentRooms[value] = 0;
                }
            }

            // The used on get set to the buffer
            if(recentRooms.ContainsKey(room))
            {
                recentRooms[room] = roomRepeatBuffer;
            }
            else
            {
                recentRooms.Add(room, roomRepeatBuffer);
            }
            
            // Instead of sending each room as the master spawns it, we maybe should consider letting the master create all the rooms, then when that is done,
            // it can loop through all the rooms data, and send that as instructions for the clients to replicate

            // What we need for that (after this for loop)
            // Loop through each room that we have,
                // We need to send LoopIndex for biome
                // We need to send Index of room 

        }

        // Then we need to loop through them again to initalize them
        // That way we can pass in any other-room's data like spawn points for portals.

        // Using the subRoomInjectionPoint on the BiomeSO, we can determine when to 'inject' the subRoomPattern data into what room's portal.
        InitalizeRooms();

        // I think at this point, the Host should send a call to let all the clients know they can Initalize their spawned rooms when ready


    }

    private bool GetRecentRoom(Room _room)
    {
        if(recentRooms.ContainsKey(_room))
        {
            return recentRooms[_room] > 0;
        }

        return false;
        
    }

    private void InitalizeRooms()
    {
        for(int i = 0; i < rooms.Count; i++)
        {
            // Now we intialize each room
            // we will need to check the subRoomInjectionPoint to make sure we are or aren't there to determin when to inject the subRoom
            //List<RoomType> roomPattern = GetRoomPattern(biomeSO, i);

            // not finished, check for subRoomInjection
            if(i == biomeSO.defaultRoomLayout.subRoomInjectionPoint)
            {
                if(biomeSO.defaultRoomLayout.subRoomPattern.Count > 0)
                {   
                    // Add the room that is the starting room for the sub room layout in the room we want to inject the data into
                    // That way we can use that room how we would like.
                    // More likely than not, we will have the Elite room be that point always, and have a Portal there.
                    // We need to then Initalize the Portal with the Injected Rooms spawn point, so we know where to move everything to
                    // when the player activates the portal
                    rooms[i].InjectionSubRoom(rooms[biomeSO.defaultRoomLayout.roomPattern.Count]);
                }
            }

            rooms[i].Init(i);
        }

        OnAllRoomsGenerated();
    }

    private GameObject InstantiateRoom(BiomeSO biomeSO, List<RoomType> roomPattern, int index)
    {
        return Instantiate(biomeSO.roomGroups[(int)roomPattern[index]].roomPrefabs[Random.Range(0, biomeSO.roomGroups[(int)roomPattern[index]].roomPrefabs.Count)], transform.position, Quaternion.identity, transform);
    }

    private GameObject InstantiateRoom(Room room)
    {
        return Instantiate(room.gameObject, transform.position, Quaternion.identity, transform);
    }

    private List<RoomType> GetRoomPattern(BiomeSO biomeSO, int i)
    {
        return i < biomeSO.defaultRoomLayout.roomPattern.Count ? biomeSO.defaultRoomLayout.roomPattern : biomeSO.defaultRoomLayout.subRoomPattern;
    }

    Room.Exit GetMatchingEntrance(Room room, Room.Exit.Direction exitDirection)
    {
        // Determine the required entrance direction based on the current exit direction
        Room.Exit.Direction requiredDirection = Room.Exit.Direction.None;

        switch (exitDirection)
        {
            case Room.Exit.Direction.Left:
                requiredDirection = Room.Exit.Direction.Right;
                break;
            case Room.Exit.Direction.Right:
                requiredDirection = Room.Exit.Direction.Left;
                break;
            case Room.Exit.Direction.Up:
                requiredDirection = Room.Exit.Direction.Down;
                break;
            case Room.Exit.Direction.Down:
                requiredDirection = Room.Exit.Direction.Up;
                break;
            default:
                Debug.LogError($"Invalid exit direction: {exitDirection}");
                break;
        }

        // Find the matching entrance in the room
        foreach (Room.Exit exit in room.exits)
        {
            if (exit.direction == requiredDirection)
            {
                return exit;
            }
        }
        return null;
    }

    void AlignRoom(Room.Exit currentExit, Room.Exit matchingEntrance, GameObject newRoom)
    {
        // Calculate the offset between the current exit and the matching entrance
        Vector3 offset = currentExit.point.position - matchingEntrance.point.position;

        // Move the new room to align the entrance with the current exit
        newRoom.transform.position += offset;

        Debug.Log($"Aligned room: {newRoom.name} at position {newRoom.transform.position}");
        
    }

    public void SetPosition(Vector3 _pos)
    {
        transform.position = _pos;
    }

    private void RaiseEventSpawnEndRoom(int roomPrefabsIndex, string currentExitDirection, int roomIndex, int debuffIndex, int roomCounter)
    {   
        object[] data = new object[] { roomPrefabsIndex, currentExitDirection, roomIndex, debuffIndex, roomCounter };
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.SpawnRoom,
            data,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others, CachingOption = EventCaching.AddToRoomCache},
            new ExitGames.Client.Photon.SendOptions { Reliability = true }
        );
    }

    private void RaiseEventSpawnRoom(object[] data)
    {

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.SpawnRoom,
            data,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others, CachingOption = EventCaching.AddToRoomCache},
            new ExitGames.Client.Photon.SendOptions { Reliability = true }
        );
    }

    private void RaiseEventSpawnRoom(int index, int loopIndex)
    {
        object[] data = new object[] { index, loopIndex };
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.SpawnRoom,
            data,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others, CachingOption = EventCaching.AddToRoomCache},
            new ExitGames.Client.Photon.SendOptions { Reliability = true }
        );
    }

    private void RaiseEventApplySeed(int seed)
    {
        object[] data = new object[] { seed };
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.SetRoomSeed,
            data,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others, CachingOption = EventCaching.AddToRoomCache},
            new ExitGames.Client.Photon.SendOptions { Reliability = true }
        );
    }

    private void RaiseEventInializeRooms()
    {
        object[] data = new object[0];
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.InitializeRooms,
            data,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others, CachingOption = EventCaching.AddToRoomCache},
            new ExitGames.Client.Photon.SendOptions { Reliability = true }
        );
    }

    /*private void RaiseEventStartNextLoop(int currentLoop, bool _isStartingRoomInitiated, bool _hasScannedPathfinding)
    {
        object[] data = new object[] { currentLoop, _isStartingRoomInitiated, _hasScannedPathfinding };
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.StartNextLoop,
            data,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others, CachingOption = EventCaching.AddToRoomCache},
            new ExitGames.Client.Photon.SendOptions { Reliability = true }
        );
    }*/

    private void SpawnEachRoom(int i, int loopIndex)
    {
        Debug.LogError("SpawnRoom Event Called");
        //int i = (int)roomPrefabsIndexData[0];
        //int loopIndex = (int)roomPrefabsIndexData[1];

        BiomeSO biomeSO = HordeModeManager.current.biomeSOs[loopIndex];
        int index = i < biomeSO.defaultRoomLayout.roomPattern.Count ? i : i - biomeSO.defaultRoomLayout.roomPattern.Count;

        List<RoomType> roomPattern = GetRoomPattern(biomeSO, i);

        GameObject roomGO = InstantiateRoom(biomeSO, roomPattern, index);
        Room room = roomGO.GetComponent<Room>();

        Room.Exit previousRoomExit = rooms.Count > 0 ? rooms[index - 1].exits[0] : null;

        if(rooms.Count > 0)
        {
            Room.Exit matchingRoomEntrance = GetMatchingEntrance(room, previousRoomExit.direction);
            AlignRoom(previousRoomExit, matchingRoomEntrance, roomGO);
        }
        rooms.Add(room);
    }


    private bool hasScannedPathfinding = false;

    public void OnAllRoomsGenerated()
    {
/*        if (hasScannedPathfinding) return;
        hasScannedPathfinding = true;*/

        Debug.Log("All rooms placed. Preparing to scan A* graph...");
        StartCoroutine(DelayedScan());
    }

    private IEnumerator DelayedScan()
    {
        // Wait a couple of frames or a short time to ensure all physics/collider transforms are finalized
        yield return null;        // wait 1 frame
        yield return null;        // wait another frame
        yield return new WaitForSeconds(0.1f); //small real-time buffer if needed

        Debug.LogWarning(">>> A* Scan Running Now");
        AstarPath.active.Scan();
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code >= 200) return;
        
        if (!PhotonNetwork.LocalPlayer.IsLocal) return;

        EventCodes e = (EventCodes) photonEvent.Code;
        object[] o = (object[]) photonEvent.CustomData;

        if(e >= EventCodes.SpawnRoom && e <= EventCodes.SpawnEndRoom)
            //Debug.Log($"OnEvent RoomGenerator {photonEvent.Code} {photonEvent.Sender}");

        //Debug.Log($"[Client] OnEvent Received: {(EventCodes)photonEvent.Code}");


        switch (e)
        {
            case EventCodes.SpawnRoom:
                //SpawnRoomEvent(o);
                break;
            case EventCodes.SetRoomSeed:
                //ApplySeedEvent(o);
                break;
            case EventCodes.InitializeRooms:
                InitalizeRooms();
                break;  
            /*case EventCodes.StartNextLoop:
                StartNextLoopEvent(o);
                break;*/
        }
    }

    public RoomDebuffSO GetRandomDebuff(float baseChance = 0.4f)
    {
        int loop = HordeModeManager.current != null ? HordeModeManager.current.currentLoop : 0;
        float scaledChance = Mathf.Clamp01(baseChance + loop * 0.1f);

        if (Random.value > scaledChance || possibleRoomDebuffs.Count == 0)
            return null;

        return possibleRoomDebuffs[Random.Range(0, possibleRoomDebuffs.Count)];
    }


    public void StartNextLoop()
    {
        // Increment loop count

        // Reset room generator state
        //ResetRoomCounter();
        SetPosition(Vector3.zero);
        roomCount = 0;
        availableExits.Clear();
        isStartingRoomInitiated = false;
        hasScannedPathfinding = false;

        // Reset player position and velocity
        var player = GameManager.instance.mainPlayer;
        player.transform.position = Vector3.zero;
        player.GetComponent<Rigidbody>().velocity = Vector3.zero;

//        StartCoroutine(WaitForPlayerAndResetPosition());
        
        if (PhotonNetwork.IsConnectedAndReady && !GameManager.instance.mainPlayer.photonView.Controller.IsMasterClient)
        {
            //return;
        }
        // Generate a fresh dungeon
        //ApplySeed(HordeModeManager.current.seed);
        //GenerateRooms();
    }

    private IEnumerator WaitForPlayerAndResetPosition()
    {
        while (GameManager.instance.mainPlayer == null)
            yield return null;

        var player = GameManager.instance.mainPlayer;
        player.transform.position = Vector3.zero;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null) rb.velocity = Vector3.zero;
    }


}
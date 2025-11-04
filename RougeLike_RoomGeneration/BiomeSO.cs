using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "HordeMode/Biome")]
public class BiomeSO : ScriptableObject
{
    public string biomeName;
    public BiomeType biomeType;

    [SerializeField]
    public RoomLayout defaultRoomLayout;

    // Should be able to include roomType in each list so we can search through them to grab the right one
    public List<RoomGroup> roomGroups = new List<RoomGroup>
    {
        new RoomGroup {roomType = RoomType.Starting},
        new RoomGroup {roomType = RoomType.EnemyWave},
        new RoomGroup {roomType = RoomType.EliteChamber},
        new RoomGroup {roomType = RoomType.BossChamber},
        new RoomGroup {roomType = RoomType.Shop},
        new RoomGroup {roomType = RoomType.Story}
    };
}

[System.Serializable]
public class RoomLayout
{
    public List<RoomType> roomPattern = new List<RoomType>
    {
        RoomType.Starting,
        RoomType.EnemyWave,
        RoomType.EnemyWave,
        RoomType.EnemyWave,
        RoomType.EnemyWave,
        RoomType.EliteChamber,
        RoomType.EnemyWave,
        RoomType.EnemyWave,
        RoomType.EnemyWave,
        RoomType.EnemyWave,
        RoomType.Shop,
        RoomType.BossChamber
    };

    public int subRoomInjectionPoint = 5; // Could make a list if we want nuanced room patterns
    // But currently we only have one subRoom to select from

    public List<RoomType> subRoomPattern = new List<RoomType>
    {
        RoomType.EnemyWave,
        RoomType.EnemyWave,
        RoomType.EnemyWave,
        RoomType.EnemyWave,
        RoomType.Shop,
        RoomType.BossChamber
    };

    public int TotalRooms()
    {
        return roomPattern.Count + subRoomPattern.Count;
    }
}

[System.Serializable]
public class RoomGroup
{
    public RoomType roomType;
    public List<GameObject> roomPrefabs = new List<GameObject>();
}

public enum BiomeType 
{
    // This order needs to be the same as the order of the list in LevelDataManager
    // That way we can get the right object by knowing what scene we are in and viceversa
    LASewer,
    Giza,
    Peru,
    Antarctica,
    HollowEarth
    // Add others
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class StoryRoom: Room
{
    [Header("Story Room Defaults")]
    public GameObject npcPrefab;
    
    // Add any other needed story elements for this room type
    
    public override void Init(int _roomID)
    {
        base.Init(_roomID);
        
        // Spawn any NPC that would be relevant to the story
        // Spawn any story room elements in the room
    }
    
    public override void StartRoom()
    {
        base.StartRoom();
        
        // Enable any npc in the room
        // Enable any Story room elements in the room
    }
    
    public override void EndRoom()
    {
        base.EndRoom();
        
        EnableBarriers(false);
    }
			
}
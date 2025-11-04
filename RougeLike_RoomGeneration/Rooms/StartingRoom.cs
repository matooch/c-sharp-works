using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class StartingRoom : StoryRoom
{   
    [Header("Starting Room Defaults")]
    public HMStartingRoom hMStartingRoom;

    public ShopTrigger buffShop;
    public override void Init(int _roomID)
    {
        base.Init(_roomID);
        
        HordeModeManager.current.hMStartingRoom = hMStartingRoom;
        HordeModeManager.current.hMStartingRoom.Init();

        buffShop.Init(() => EnableBarriers(false));
        // Initalize StartingRoomObjects and progression checks
        // Determine what bonus objects eh player recieves at the start of this run if any
        // Currently we have the player able to unlock more genetic/animal powers and better weapons, and gun bucks
        // As we build out the bigger progression, what things would be available here?
    }
    
    public override void StartRoom()
    {
        base.StartRoom();
        
        buffShop.Enable();
        GameManager.instance.GenomeTrackFadeOut();
        // Enable any SRO items that need to be enabled if any
    }
}
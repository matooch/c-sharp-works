using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class EliteRoom : WaveRoom
{
    [Header("Elite Room Defaults")]
    public KeyCard requiredKeyCard;
    public Portal portal; // use composition to have portal logic on the class
    
    // Virtual voids that are not overrride, inherit the parents void
    // ie StartRoom and EndRoom from WaveRoom
    
    public override void Init(int _roomID)
    {
        base.Init(_roomID);
        
        // Setup EliteRoom specific conditions
        // Might need to have the base.Init() go after these class specific conditions
        // since the WaveSpawners init will be called in the base.Init();
        
        // We will want the Elite Spawner to have the elite conditions
        // Need to determine exactly what those are, then we can configure here
        
        // Configure the Portal to load us into the correct rooms
        portal?.Init(injectionRoom);
            
    }
    
    public override void EndRoom()
    {
        base.EndRoom();
        
        portal?.Enable(requiredKeyCard);
            
    }
    	
		
}
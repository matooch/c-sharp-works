using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
public class WaveRoom : Room
{
    [Header("Wave Room Defaults")]
    public HordeModeSpawner hordeModeSpawner;
    [HideInInspector]public ShopTrigger buffShop;
    private RoomDebuff roomDebuff = new RoomDebuff();
    public RoomDebuffSO roomDebuffSO => roomDebuff.GetAppliedDebuff();
    
    public override void Init(int _roomID)
    {
        base.Init(_roomID);
        
        //roomDebuff?.Init(this);
        // Here the Wave spawner needs to be initalized
        // Which will spawn all the enemies, and setup the enemies conditions
        
        hordeModeSpawner.Init(this); 
        // We could pass in the room and then the wave spawner has the 
        // room reference here, that way it will always have it incase of 
        // a serialized issue
        
        // The Init on the buff shop will take in the void that opens the barrier
        // There an Action will subscribe to this function, and be Invoked when
        // the buff shop has had a buff selected.
        

        buffShop.Init(() => EnableBarriers(false));

        
    }
    
    public override void StartRoom()
    {
        // This Void should be called once all players in the the room
        // Probably should have a check on the Base Room class to know when all
        // the players are in the room.
        
        base.StartRoom();
        
        // Here the wave spanwer will be called to begin the waves
        
        EnableBarriers(true);
        
        hordeModeSpawner.StartWave();
        // StartCoroutine(StartNextWave()); should be in a function to be called
        
    }
    
    public override void EndRoom()
    {
        // This void will be called when the wave spawner sees that all enemies are defeated
        // This void should be subscribed to an action in the WaveSpawner
        base.EndRoom();
        
        // Here the Randi Buff will be enabled so the player can select a buff
        // and the barriers will be opened once they select (set in initalized).

        hordeModeSpawner.RoomCleared();
    
    }


}
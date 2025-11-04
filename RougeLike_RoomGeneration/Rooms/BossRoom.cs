using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;


public class BossRoom : Room
{
    [Header("Boss Room Defaults")]
    public KeyCard keyCard;
    public Portal portal; // use composition to have portal logic on the class
    public GameObject bigEnemyPortal;
    public LootCrate lootCrate;
    public List<GameObject> boss;
    public List<Transform> spawnPoints = new List<Transform>();
    private EnemySpawn enemySpawn = new EnemySpawn();

    // Make a list if we want waves? But no, then we should use a wave spawner to drive it with boss enemies
    private ActiveEnemies activeEnemies = new ActiveEnemies();
			
    public override void Init(int _roomID)
    {
        base.Init(_roomID);

        enemySpawn.Init(this, boss, null, null);

        lootCrate.Init();
        lootCrate.gameObject.SetActive(false);
        
        // Spawn the boss
        if (!PhotonNetwork.IsConnectedAndReady || PhotonNetwork.IsConnectedAndReady && GameManager.instance.mainPlayer.photonView.Controller.IsMasterClient)
        {
            SpawnBoss(0);
            // Configure the Boss
            activeEnemies.enemies.ForEach(i => i.GetComponent<Enemy>().Init(null, null));
            // The Boss enemy should be configured for client in RPC
        }

        // Pass in the EndRoom() into the onDeath on boss
        
        // Configure the Portal to load us into the correct rooms
        //portal?.Init(null);
    }
    
    public override void StartRoom()
    {
        base.StartRoom();
        // Start the boss in the room
        activeEnemies.enemies.ForEach(i => i.GetComponent<IBoss>().StartBoss());
        //HordeModeManager.current.hordeModeCanvasManager.SetEnemyInfo(enemiesPerWave + " / " + enemiesPerWave, room.roomType != RoomType.BossChamber);
    }
    
    public override void EndRoom()
    {
        base.EndRoom();

        HordeModeManager.current.hordeModeCanvasManager.SetActive(false);
        //HordeModeManager.current.hMShops[(int)HMShopType.UnlockShop].Setup();

        HordeModeManager.current.hordeModeCanvasManager.SetWaveInfo(HMRoomTitle.BossCleared, "");

        //portal?.Enable(keyCard);

        // Set Keycard for the player if it isnt null
        if(keyCard != KeyCard.None)
        {
            GameManager.instance.mainPlayer.playerData.characterData.keycards[(int)keyCard] = true;
            GameManager.instance.mainPlayer.playerData.characterData.Save();
        }

        lootCrate.gameObject.SetActive(true);
    }
    
    
    public void SpawnKeyCard()
{
        // Spawn the keycard since the boss was defeated
    }

    private void SpawnBoss(int index = 0)
    {
        if (PhotonNetwork.IsConnectedAndReady && GameManager.instance.mainPlayer.photonView.Controller.IsMasterClient)
        {
            ExitGames.Client.Photon.Hashtable currentRoomGenomeBreachState = new ExitGames.Client.Photon.Hashtable();
            currentRoomGenomeBreachState.Add($"GenomeBreachState", $"{(int)GBNetworkGameState.SpawningBossEnemy}:{(byte)roomID}");
            //PhotonNetwork.CurrentRoom.SetCustomProperties(currentRoomGenomeBreachState);
        }

                // Spawn portal like a big enemy
        if (bigEnemyPortal != null)
        {
            //pendingPortalSpawns++;
            GameObject portal = Instantiate(bigEnemyPortal, spawnPoints[index].position + new Vector3(0f, 1.5f, 0f), spawnPoints[index].rotation);
            BigEnemyPortalSpawner portalSpawner = portal.GetComponent<BigEnemyPortalSpawner>();
            portalSpawner.Init(HandleEnemyDefeated, activeEnemies, index, spawnPoints[index]);
            
            if (PhotonNetwork.IsConnectedAndReady && GameManager.instance.mainPlayer.photonView.Controller.IsMasterClient)
            {
                RaiseEventSpawnBigEnemyPortal(roomID, 1, index, spawnPoints[index].position, spawnPoints[index].rotation.eulerAngles);
            }

            Debug.Log($"[SpawnBossEnemy] Portal spawning for boss: {boss[index].name}");
        }
        else
        {
            activeEnemies.enemies.Add(enemySpawn.SpawnSpecificEnemy(index, spawnPoints[index], HandleEnemyDefeated));
        }

    }

    public void HandleEnemyDefeated(Enemy enemy)
    {
        //activeEnemies[currentWave - 1].enemies.Remove(enemy);
        // For one boss, just end it
        EndRoom();

        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.RaiseEvent(
                (byte)EventCodes.SlowMotionFinalKill,
                null,
                new RaiseEventOptions { Receivers = ReceiverGroup.All },
                new SendOptions { Reliability = true }
            );
        }
        else if (!PhotonNetwork.IsConnectedAndReady)
        {
            GameManager.instance.SlowMotionKill();
        }

    }

    public void SetBossForHordeModeEvent(object[] data)
    {

        if (data != null && (int)data[1] == roomID)
        {

            PhotonView enemy = PhotonView.Find((int)data[0]);
            

            if (enemy == null)
            {
                return;
            }
            
            Enemy enemyScript = enemy.gameObject.AddComponent<Enemy>();
            enemy.transform.parent = this.transform;
                        
            if (enemyScript != null)
            {
                //activeEnemies[currentWave].Add(enemyScript);
                enemyScript.OnDefeated += HandleEnemyDefeated;

                enemyScript.Init(null, null);

                activeEnemies.enemies.Add(enemyScript);
                
            }
        }
    }

    private void RaiseEventSpawnBigEnemyPortal(int roomID, int _pendingPortalSpawns, int selectedEnemyIndex, Vector3 spawnPoint, Vector3 eulerRotation)
    {
        object[] package = new object[] { roomID, _pendingPortalSpawns, selectedEnemyIndex, spawnPoint, eulerRotation };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.SpawnBigEnemyPortal,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others, CachingOption = EventCaching.DoNotCache},
            new ExitGames.Client.Photon.SendOptions { Reliability = true }
        );
    }
    /*
    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code >= 200) return;
        
        if (!PhotonNetwork.LocalPlayer.IsLocal) return;

        EventCodes e = (EventCodes) photonEvent.Code;
        object[] o = (object[]) photonEvent.CustomData;

        if (e >= EventCodes.SetEnemyForHordeMode && e <= EventCodes.RoomCleared)
        {
            Debug.Log($"OnEvent HordeModeSpawner {(EventCodes)photonEvent.Code} {photonEvent.Sender}");
        }

        Debug.Log($"[Client] OnEvent Received: {(EventCodes)photonEvent.Code}");

        switch (e)
        {
            case EventCodes.SlowMotionFinalKill:
                GameManager.instance.SlowMotionKill();
                break;

            case EventCodes.SetBossForHordeMode:
                SetBossForHordeModeEvent(o);
                break;
        }
    }
    */

}
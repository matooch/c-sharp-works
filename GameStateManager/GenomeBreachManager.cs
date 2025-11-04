using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class GenomeBreachManager : MonoBehaviour, IGameState
{
    public void LoadContent()
    {
        SetupCustomization();
        gameObject.SetActive(true);
        HordeModeManager.current.hordeModeCanvasManager.gameObject.SetActive(false);
        PlayerManager.instance.SpawnCharacter();

        // if host, run this. Client wait for seed
        HordeModeManager.current.PrepareSeed();

        // Raise event, pass bool to everyone that they are setup

        if (PhotonNetwork.IsConnectedAndReady)
        {
            GameManager.instance.isNetworkLoadedReady = true;
        }
    }

    public void UnloadContent()
    {
        gameObject.SetActive(false);
        HordeModeManager.current.Reset();
        //PlayerManager.instance.EnableHumanPlayers(false);
    }

    private void SetupCustomization()
    {
        Customization _customization = Customization.GetCustomization();


        _customization.playerData.Clear();
        _customization.LoadDefaultCharacter(0);
    }

    // Here is an example of a function we could run on say the 'StartGame' button in the title screen
    // From here the GameManager would unload the active state (TitleScreen) and then load the next state that is called (HQ)
    // HQ would then Load its content (THe HQ prefab, then load the PlayerManager, etc)
    // Depending on how we want to load Online, we could have the HQ Manager make the Photon Network connectivity opperate during this Load Content,
    // so we could in theory, have the HQ Manager make the joining of other player possible, then make that possibilty unavailable when we leave the HQ mode,
    // That we we only can join in HQ and not during a run. And as there will not be a player in TitleScreen, not there either.
    public void ExitGame() => GameManager.instance.ChangeState(GameManager.GameMode.TitleScreen);
    public void ReturnToHQ() => GameManager.instance.ChangeState(GameManager.GameMode.HQ);
}
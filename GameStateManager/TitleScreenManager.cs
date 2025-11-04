using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using Rewired;

public class TitleScreenManager : MonoBehaviour, IGameState
{
    private bool isLoaded = false, introSkipped = false, isMovingLogo = false;
    public PlayableDirector timeline;
    public GameObject pressAnyButton, skipIntroText, startGameText, titlescreenRoot, settingsMenu;
    public Image holdToSkipImage;
    public RectTransform titleLogo;
    private Player player;
    private float holdToSkipTimer = 3f;
    public void LoadContent()
    {
        PlayerManager.instance.DestroyHumanPlayers();
        gameObject.SetActive(true);
        isLoaded = true;
        
        if (PhotonNetwork.IsConnectedAndReady)
        {
            GameManager.instance.isNetworkLoadedReady = true;
        }
    }

    public void UnloadContent()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if(!isLoaded) return;

        if(player == null) player = ReInput.players.GetPlayer(0);


        if (introSkipped && player.GetAnyButton()) //RA - 3-16-2025 - Fixes issue where if the player waited for the whole intro sequence to finish then they'd be stuck on the "Press any button to continue" screen
        {
            SkipIntro();

        }

        if (player.GetAnyButton() && !introSkipped)
        {
            holdToSkipTimer -= 1 * Time.deltaTime;
            skipIntroText.SetActive(true);
            holdToSkipImage.fillAmount += .8f / holdToSkipTimer * Time.deltaTime;
            if (holdToSkipTimer <= 0)
            {
                SkipIntro();
            }
        }
        else
        {
            skipIntroText.SetActive(false);
            holdToSkipImage.fillAmount = 0f;
            holdToSkipTimer = 0.5f;
        }

    }

    private void SkipIntro()
    {
        timeline.time = 50f;
        titlescreenRoot.SetActive(true);
        skipIntroText.SetActive(false);
        holdToSkipImage.gameObject.SetActive(false);
        introSkipped = true;
        MoveTitleLogoUp(true);
    }

    void MoveTitleLogoUp(bool isSkip)
    {
        if (isMovingLogo) return;
        StartCoroutine(MoveLogoRoutine(isSkip));
    }

    IEnumerator MoveLogoRoutine(bool isSkip)
    {
        isMovingLogo = true;
        float time = 0;
        float duration = isSkip ? 0 : 0.25f;
        
        while(time < duration)
        {
            titleLogo.anchoredPosition = new Vector3(titleLogo.anchoredPosition.x, Mathf.Lerp(7.31f, 13f, Mathf.Pow(time / duration, 0.25f)));
            time += Time.unscaledDeltaTime;
            yield return null;
        }

        titleLogo.anchoredPosition = new Vector3(titleLogo.anchoredPosition.x, 13f);
        startGameText.SetActive(false);
        // Move logo
    }

    // Here is an example of a function we could run on say the 'StartGame' button in the title screen
    // From here the GameManager would unload the active state (TitleScreen) and then load the next state that is called (HQ)
    // HQ would then Load its content (THe HQ prefab, then load the PlayerManager, etc)
    // Depending on how we want to load Online, we could have the HQ Manager make the Photon Network connectivity opperate during this Load Content,
    // so we could in theory, have the HQ Manager make the joining of other player possible, then make that possibilty unavailable when we leave the HQ mode,
    // That we we only can join in HQ and not during a run. And as there will not be a player in TitleScreen, not there either.
    public void StartGame()
    {
        GameManager.instance.ChangeState(GameManager.GameMode.HQ);
    }
}
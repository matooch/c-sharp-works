using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;
using static ArmorBase;

public class PlayerDisplayCustomization : MonoBehaviour
{
    public Customization customization;
    public PlayerCustomizationType playerCustomizationType;
    public PlayerCustomization playerCustomization => playerData.playerCustomization;
    private PlayerData playerData;
    // Create a declaration of the classes for the armor and weapon visuals
    // Also consider how the Weapon Mods and character mods should show up based on 
    // if they are enabled or not, So either it knows here (which it should since PlayerCustomization is central)
    // The player data effects will be on the other components, but this is for visual

    public Transform leftHand, rightHand;
    public Transform gunHolster, hipHolster_r, hipHolster_l;
    public GameObject extraLeftHandPistol;
    public GameObject skinnedMeshGeo;
    GameObject primary, heavy, character;
    // Stores the spawned weapons prefabs to be accessed and configured when called
    private List<WeaponCustomizationSystem> weaponCustomizationSystems = new List<WeaponCustomizationSystem>();
    // Stores the spawned Character types. Marine Armor, and other characters
    [HideInInspector] public List<ArmorCustomizationManager> armorCustomizationManagers = new List<ArmorCustomizationManager>();

    // Value 16 grabbed from length of Weapon Prefabs on GetCustomInfo;
    public Vector3[] weaponTransforms = new Vector3[17];
    List<GameObject> weapons = new List<GameObject>();
    List<GameObject> characters = new List<GameObject>();
    List<GameObject> armorPieces = new List<GameObject>();

    List<bool> isRightHanded = new List<bool>();
    private SpaceMarine player;
    int primaryWeaponIndex = -1;
    int heavyWeaponIndex = -1;

    public void LoadPlayerCustomization(GetCustomInfo getCustomInfo)
    {   
        customization = getCustomInfo.customization;
        playerData = getCustomInfo.playerData;
        //customization = Customization.GetCustomization(playerNumber);
        //playerCustomization = getCustomInfo.playerData.playerCustomization;
        player = getCustomInfo.player;
        Debug.Log($"LoadPlayerCustomization");
        SpawnCharacter();
        SpawnWeapons();
        SetCustomization();

        // Called from the Customization class to update the models and their colors
    }

    public void LoadPlayerCustomizationForNetworkPlayers(SpaceMarine _player, PlayerData _playerData)
    {
        //playerCustomization = GameManager.instance.mainPlayer.customization.playerData.Find(x => x.playerNumber == player.PV.ViewID).playerCustomization;
        //playerCustomization = customization.playerData.Find(x => x.playerNumber == player.PV.ViewID).playerCustomization;
        playerData = _playerData;
        player = _player;
        /*
        foreach (var item in customization.playerData)
        {
            if (item.playerNumber == player.PV.ViewID)
            {
                playerCustomization = item.playerCustomization;
                break;
            }
        }*/
        /*playerCustomization = player.getCustomInfo.customization.playerData
            .Find(x => x.playerNumber == player.PV.ViewID).playerCustomization;*/
        
        //SpawnCharacter();
        //SpawnWeapons();
        
        /*SetPrimary();
        SetHeavy();
        SetArmor();*/
    }

    public void SetCustomization()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            if (!player.PV.IsMine)
            {
                return;
            }
        }
        Debug.Log($"SetCustomization");
        
        SetPrimary();
        SetHeavy();
        SetArmor();
        SetGenetics();
        

        player.weaponTransformInfo.setWeaponTransforms(player);

//        SetIlluminationSettings();
    }

    public void SetIlluminationSettings()
    {
        player.getCustomInfo.SelfIllumUpdate();
    }

    // Enabling / disabling the customization logic on the player
    public void SetPrimary()
    {
        // Disable the primary that is enabled, and enable the new one
        // Set the color of the primary as well
        // Reference GetCustomInfo setWeapons()

        // ItemClass includes Armor, so minus 4 to get primary at zero
        primaryWeaponIndex = (int)playerCustomization.primary - 4;

        //anim.SetInteger("PrimaryWeaponType", primaryWeaponIndex);

        if(primary != null)
        {
            primary.SetActive(false);
        }

        primary = weapons[primaryWeaponIndex];
        primary.SetActive(true);

        // Pass this weapons customization class to get its color set by the item equipped
        if (!PhotonNetwork.IsConnectedAndReady || player.PV.IsMine)
        {
            SetColor(weaponCustomizationSystems[primaryWeaponIndex]);
            SetAfix(weaponCustomizationSystems[primaryWeaponIndex]);

            playerCustomization.itemColors[4] = weaponCustomizationSystems[primaryWeaponIndex].itemColor;
        }
        else
        {
            SetColor_Network(weaponCustomizationSystems[primaryWeaponIndex], playerCustomization.itemColors[4]);
        }
        
        // Sets any mods that are on the Item
        weaponCustomizationSystems[primaryWeaponIndex].SetWeaponMods(playerCustomization.weaponMods);

        if(playerCustomizationType == PlayerCustomizationType.Scene)
        {
            // Set the Weapon to the player ie set the fireprefab / aim transform / data
            if (!PhotonNetwork.IsConnectedAndReady || player.PV.IsMine)
            {
                weaponCustomizationSystems[primaryWeaponIndex].SetPrimaryWeaponOnPlayer(player);
            }
        }


        // Display the DualPistol in second hand if item is equipped
        if((playerCustomization.primary == ItemClass.DualPistols))
        {
            extraLeftHandPistol.SetActive(true);
            weaponCustomizationSystems[primaryWeaponIndex].SetWeaponMods(playerCustomization.weaponMods);

            WeaponCustomizationSystem dualPistolCustomization = extraLeftHandPistol.GetComponent<WeaponCustomizationSystem>();
            
            if (!PhotonNetwork.IsConnectedAndReady || player.PV.IsMine)
            {
                SetColor(dualPistolCustomization);

                playerCustomization.itemColors[4] = dualPistolCustomization.itemColor;
            }
            else
            {
                SetColor_Network(dualPistolCustomization, playerCustomization.itemColors[4]);
            }

            if(playerCustomizationType == PlayerCustomizationType.UI)
            {
                dualPistolCustomization.SetUIMateial();
                dualPistolCustomization.weaponModsSystem.SetModUIMaterial();
            }

            dualPistolCustomization.SetWeaponMods(playerCustomization.weaponMods);
        }
        else
        {
            extraLeftHandPistol.SetActive(false);
        }
        
        //object[] weaponMods = new object[3];
        //weaponMods = Array.ConvertAll(playerCustomization, item => (object)item);
        
        
        /*object[] weaponMods = playerCustomization.weaponMods.GetType().GetProperties()
            .Select(p => p.GetValue(playerCustomization.weaponMods))
            .ToArray();*/
        if(playerCustomizationType == PlayerCustomizationType.Scene)
        {
            if (PhotonNetwork.IsConnectedAndReady && player.PV.IsMine)
            {
                player.PV.RPC(nameof(RPC_SetPrimaryWeapon), RpcTarget.OthersBuffered, player.PV.ViewID, primaryWeaponIndex, /*weaponMods*/(int)playerCustomization.weaponMods.barrels, (int)playerCustomization.weaponMods.magazines, (int)playerCustomization.weaponMods.scopes, weaponCustomizationSystems[primaryWeaponIndex].itemColor.ToObject());
            }
        }

    }
    public void SetHeavy()
    {
        Debug.Log($"SetHeavy");
        // Disable the heavy that is enabled, and enable the new one
        // Set the color of the heavy as well

        // ItemClass includes Armor, so minus 4 to get primary at zero
        heavyWeaponIndex = (int)playerCustomization.heavy - 4;

        //anim.SetInteger("PrimaryWeaponType", primaryWeaponIndex);

        if(heavy != null)
        {
            heavy.SetActive(false);
        }

        heavy = weapons[heavyWeaponIndex];
        //heavy.SetActive(true);

        // Pass this weapons customization class to get its color set by the item equipped
        if (!PhotonNetwork.IsConnectedAndReady || player.PV.IsMine)
        {
            SetColor(weaponCustomizationSystems[heavyWeaponIndex]);
            SetAfix(weaponCustomizationSystems[heavyWeaponIndex]);
            
            playerCustomization.itemColors[5] = weaponCustomizationSystems[heavyWeaponIndex].itemColor;
        }
        else
        {
            SetColor_Network(weaponCustomizationSystems[heavyWeaponIndex], playerCustomization.itemColors[5]);
        }

        if(playerCustomizationType == PlayerCustomizationType.Scene)
        {
            // Set the Weapon to the player ie set the fireprefab / aim transform / data
            if (!PhotonNetwork.IsConnectedAndReady || player.PV.IsMine)
            {
                weaponCustomizationSystems[heavyWeaponIndex].SetSecondaryWeaponOnPlayer(player);
            }
        }
        
        if(playerCustomizationType == PlayerCustomizationType.Scene)
        {
            if (PhotonNetwork.IsConnectedAndReady && player.PV.IsMine)
            {
                player.PV.RPC(nameof(RPC_SetHeavyWeapon), RpcTarget.OthersBuffered, player.PV.ViewID, heavyWeaponIndex, weaponCustomizationSystems[heavyWeaponIndex].itemColor.ToObject());
            }
        }
    }
    public void SetArmor()
    {
        // Disable the armor that is enabled, and enable the new one
        // Set the color of the armor as well

        if(character != characters[(int)playerCustomization.character])
        {
            character.SetActive(false);

            character = characters[(int)playerCustomization.character];
            character.SetActive(true);

            if(playerCustomization.character != Character.SpaceMarine)
            {
                skinnedMeshGeo.GetComponent<SkinnedMeshRenderer>().enabled = false;
                return;
            }
            else
            {
                skinnedMeshGeo.GetComponent<SkinnedMeshRenderer>().enabled = true;
            }
        }

        // Turn off active armor pieces
        if(armorPieces.Count > 0)
        {
            foreach(GameObject piece in armorPieces)
            {
                piece.SetActive(false);
            }
        }

        ItemColor[] itemColors = new ItemColor[4];
        int[] itemNumber = new int[4];
        
        // Loop through the four armor pieces and activate the appropreate armor piece based on item style
        for(int i = 0; i < playerCustomization.armorStyles.Length; i++)
        {   

            // Get the ArmorCustomization System on the armor piece we want. IE the correct item type and the armor visual value that has been set on PlayerCustomization
            ArmorCustomizationSystem _armorCustomSystem = armorCustomizationManagers[0].armorCustomizationPieces[i].armorCustomizationSystems[(int)playerCustomization.armorStyles[i]];

            // Enable that gameobject
            _armorCustomSystem.gameObject.SetActive(true);

            // Store that Gameobject to an array to be disabled if changes made
            armorPieces.Add(_armorCustomSystem.gameObject);

            // Set the color of the armor piece based on the item in the equipped slot
            if (!PhotonNetwork.IsConnectedAndReady || player.PV.IsMine)
            {
                SetColor(_armorCustomSystem);
                itemColors[i] = _armorCustomSystem.itemColor;
                itemNumber[i] = (int)playerCustomization.armorStyles[i];

                playerCustomization.itemColors[i] = _armorCustomSystem.itemColor;
            }
            else
            {
                SetColor_Network(_armorCustomSystem, playerCustomization.itemColors[i]);
            }
        }

        /*object[] armorStyles = playerCustomization.armorStyles.GetType().GetProperties()
            .Select(p => p.GetValue(playerCustomization.armorStyles))
            .ToArray();*/

        if(playerCustomizationType == PlayerCustomizationType.Scene)
        {
            if (PhotonNetwork.IsConnectedAndReady && player.PV.IsMine)
            {
                player.PV.RPC(nameof(RPC_SetArmor), RpcTarget.OthersBuffered, player.PV.ViewID, (int)playerCustomization.character, itemNumber[0], itemNumber[1], itemNumber[2], itemNumber[3], itemColors[0].ToObject(), itemColors[1].ToObject(), itemColors[2].ToObject(), itemColors[3].ToObject()/*, armorStyles*/);
            }
        }


    }

    public void SetArmor(ItemType _itemType, ItemColor _itemColor)
    {
        SetArmor();

        armorCustomizationManagers[0].armorCustomizationPieces[(int)_itemType].armorCustomizationSystems[(int)playerCustomization.armorStyles[(int)_itemType]].SetColor(_itemColor);
    }

    public void SetGenetics()
    {
        // The gene system needs to come out of the radial wheel.
        // Make a new Gene Manager that enables or disables the animal gene scripts
        // Possibly make them a child of a special class? That way they can be referenced in an array
        // to make enabling and disableing more straightforward?
        // Maybe not nessesary for the work required

        // Currently only needed on the Scene player.
        // Its possible if we want to have some sort of Aura or something visible on the player
        // we could have the UI displayer character have some "Genetic Power" effects enabled here
        if(playerCustomizationType == PlayerCustomizationType.Scene)
        {
            player.geneticPowerManager.SetMutations(playerCustomization.playerGeneticAbilites);
             
            if (PhotonNetwork.IsConnectedAndReady && player.PV.IsMine)
            {
                player.PV.RPC(nameof(RPC_SetGenetics), RpcTarget.OthersBuffered, player.PV.ViewID, playerCustomization.playerGeneticAbilites.ToObject());
            }
        }
    }

    public void SetColor(ItemCustomizationSystem itemCustomization)
    {
        int _slot = 0;
        Item item = customization.equipments[_slot].GetSlots[(int)itemCustomization.itemType].item;

        if(item.Id == -1) return;

        itemCustomization.SetColor(item.itemColor);
    }

    public void SetColor_Network(ItemCustomizationSystem itemCustomization, ItemColor itemColor) //using for setColor based on color that was sent by RPC in customization.playerData.playerCustomization.itemColor[]
    {
        /*Item item = customization.equipments[playerCustomization.saveSlot].GetSlots[(int)itemCustomization.itemType].item;

        if(item.Id == -1) return;*/

        itemCustomization.SetColor(itemColor);
    }

    public void SetAfix(WeaponCustomizationSystem weaponCustomization)
    {
        int _saveSlot = 0;
        Item item = customization.equipments[_saveSlot].GetSlots[(int)weaponCustomization.itemType].item;
        weaponCustomization.SetLegendaryAfix(item.legendaryModifier);
    }

    public void SetCharacterMods()
    {
        // Set the visuals of the character mods on the player
        // Set the logic of the mods to affect the player
    }


    // Spawning the character and weapons for the player
    public void SpawnCharacter()
    {
        /*if (PhotonNetwork.IsConnectedAndReady)
            if (player == null )
            {
                StartCoroutine(WaitAndRemapSkeleton());
                return;
            }*/
        
        for(int i = 0; i < customization.characterPrefabs.Length; i++)
        {
            //if(i >= 1) break;
            
            GameObject characterPrefab = customization.characterPrefabs[i];
            // Will rework to be able to add a different character. Right now just focus on SpaceMarine
            character = Instantiate(characterPrefab, skinnedMeshGeo.transform.parent.transform);
            characters.Add(character);
            armorCustomizationManagers.Add(character.GetComponent<ArmorCustomizationManager>());

            /*if (PhotonNetwork.IsConnectedAndReady  && player != null && !Equals(PhotonNetwork.LocalPlayer, player.PV.Controller))
            {
                StartCoroutine(WaitAndRemapSkeleton(i));
                character.SetActive(false);
                return;
            }*/

            armorCustomizationManagers[i].skeletalRemapper.remapSkeleton(skinnedMeshGeo, playerCustomization.character, playerCustomization.armorStyles[(int)ItemType.Chest], false, skinnedMeshGeo, playerCustomizationType);

            // Add this to set Armor / Set Character logic
            if(playerCustomization.character != Character.SpaceMarine && playerCustomizationType == PlayerCustomizationType.Scene) //The Space Marine character already has this set to the headless base geo
            {
                player.checkIfOnScreen = armorCustomizationManagers[i].skeletalRemapper.checkIfOnScreen;
            }

            if(playerCustomizationType == PlayerCustomizationType.UI)
            {
                character.layer = LayerMask.NameToLayer("UI");

                if(playerCustomization.character == Character.SpaceMarine)
                {
                    // Set tag to UI
                    foreach(ArmorCustomizationPieces pieces in armorCustomizationManagers[i].armorCustomizationPieces)
                    {
                        foreach(ArmorCustomizationSystem custom in pieces.armorCustomizationSystems)
                        {
                            custom.SetUIMateial();

                        }
                    }
                }
                else // EveryoneElse
                {
                    foreach(Transform child in character.transform)
                    {
                        child.gameObject.layer = LayerMask.NameToLayer("UI");
                    }
                }
            }

            character.SetActive(false);

        }

        //lastSpawnedCharacter = character;
        //armorMats.AddRange(character.GetComponentsInChildren<SkinnedMeshRenderer>());
        
        //set ref for armor to be able to sync it for the multiplayer
        //armorSet = character.GetComponentsInChildren<GetArmor>();
        /*
        if (PhotonNetwork.IsConnectedAndReady && player.PV.IsMine)
        {
            foreach (var item in armorSet)
            {
                item.UpdateArmorTypeForRPC(equippedHelmet, equippedChest, equippedArms, equippedLegs);
            }
        }
        */

        // Spawn the marine armor for the player
        // Get the ArmorCustomizationManager on the MarineArmor prefab
        // This will be our access to the armor pieces and their colors

        // if another character is enabled, spawn that type as well, this might change if we have character types
        // more often used.
        // Disable the Marine armor if another character is used, not destroy
    }

    public void SpawnWeapons()
    {
        for(int i = 0; i < customization.weaponPrefabs.Length; i++)
        {
            weapons.Add(Instantiate(customization.weaponPrefabs[i]));
            isRightHanded.Add(new bool());
            isRightHanded[i] = weapons[i].GetComponent<FirePrefab>().isRightHanded;

            if(playerCustomizationType == PlayerCustomizationType.UI)
            {
                Destroy(weapons[i].GetComponent<FirePrefab>());
            }

            WeaponCustomizationSystem weaponCustomizationSystem = weapons[i].GetComponent<WeaponCustomizationSystem>();
            weaponCustomizationSystems.Add(weaponCustomizationSystem);

            // Reorders the Mods to match the order of the enums
            if(weaponCustomizationSystem.weaponModsSystem != null)
            {
                weaponCustomizationSystem.weaponModsSystem.OrderMods();

                if(playerCustomizationType == PlayerCustomizationType.UI)
                {
                    weaponCustomizationSystem.weaponModsSystem.SetModUIMaterial();
                }
            }

            if(playerCustomizationType == PlayerCustomizationType.UI)
            {
                SetupWeaponDataUIPlayer(i);
                weaponCustomizationSystems[i].SetUIMateial();
            }
            else
            {
                SetupWeaponDataScenePlayer(i);
            }

        }
    }

    void SetupWeaponDataScenePlayer(int i)
    {
        //int primaryWeaponIndex = (int)playerCustomization.primary - 4;

        weapons[i].transform.parent = isRightHanded[i] ? rightHand : leftHand;

        weapons[i].transform.localPosition = customization.weaponPrefabs[i].transform.localPosition;
        weapons[i].transform.localRotation = customization.weaponPrefabs[i].transform.localRotation;
    }

    void SetupWeaponDataUIPlayer(int i)
    {
        //weapons[i].transform.parent = isRightHanded[i] ? rightHand : leftHand;
        if(i == 3 || i == 7) // Revolver = 8 // Pistol = 12
        {
            weapons[i].transform.parent =  hipHolster_r;
            if(i == 8) weapons[i].transform.localScale = Vector3.one * 40;
        }
        else
        {
            weapons[i].transform.parent = gunHolster;
        }
        weapons[i].transform.localPosition = weaponTransforms[i]; //player.getCustomInfo.weaponPrefabs[i].transform.localPosition;
        weapons[i].transform.localRotation = Quaternion.Euler(Vector3.zero); //player.getCustomInfo.weaponPrefabs[i].transform.localRotation;


        
    }

    public enum PlayerCustomizationType {Scene, UI};

    #region Photon RPCs functions

    public void WaitAndRestorePrimaryWeaponPlacement()
    {
        StartCoroutine(RestorePrimaryWeaponPlacement());
    }

    [PunRPC]
    private void RPC_SetPrimaryWeapon(int ViewID, int primaryWeaponIndex, /*object[] weaponMods*/int barrels, int magazines, int scopes, object[] itemColor)
    {
        StartCoroutine(WaitAndSetPrimary(ViewID, primaryWeaponIndex, barrels, magazines, scopes, itemColor));
    }

    private IEnumerator WaitAndSetPrimary(int ViewID, int primaryWeaponIndex, /*object[] weaponMods*/int barrels, int magazines, int scopes, object[] itemColor)
    {
        Debug.Log("WaitPrimaryWeapon Called: " + gameObject.name);
        WaitForSeconds waiter = new WaitForSeconds(0.1f);
        
        while (weapons.Count < 1)
        {
            yield return waiter;
        }
        
        if (weapons.Count < 1)
        {
            yield break;
        }
        
        SpaceMarine localPlayer = PhotonView.Find(ViewID).GetComponent<SpaceMarine>();
        
        if(primary != null)
        {
            primary.SetActive(false);
        }
        
        primary = weapons[primaryWeaponIndex];
        primary.SetActive(true);
        
        localPlayer.getCustomInfo.playerDisplayCustomizations[0].primary = weapons[primaryWeaponIndex];
        localPlayer.getCustomInfo.playerDisplayCustomizations[0].primary.SetActive(true);
        
        SetColor_Network(weaponCustomizationSystems[primaryWeaponIndex], ItemColor.FromObject(itemColor, 0));
        // Sets any mods that are on the Item
        WeaponMods mods = new WeaponMods();
        mods.barrels = (Barrels)barrels;
        mods.magazines = (Magazines)magazines;
        mods.scopes = (Scopes)scopes;
        
        PlayerData viewIdData = customization.playerData.Find(x => x.playerNumber == ViewID);
        
        weaponCustomizationSystems[primaryWeaponIndex].SetWeaponMods(mods);
        
        while (viewIdData == null)
        {
            viewIdData = customization.playerData.Find(x => x.playerNumber == ViewID);
            yield return waiter;
        }

        //viewIdData.playerCustomization.heavy = (ItemClass)primaryWeaponIndex + 4;
        viewIdData.SetPrimary((ItemClass)primaryWeaponIndex + 4);
        //Debug.Log($"(ItemClass)primaryWeaponIndex {(ItemClass)primaryWeaponIndex + 4} player {viewIdData.playerNumber}");
        
        if(playerCustomizationType == PlayerCustomizationType.Scene)
        {
            // Set the Weapon to the player ie set the fireprefab / aim transform / data
            weaponCustomizationSystems[primaryWeaponIndex].SetPrimaryWeaponOnPlayer(localPlayer);
        }
        // Display the DualPistol in second hand if item is equipped
        if(viewIdData.playerCustomization.primary == ItemClass.DualPistols/*(playerCustomization.primary == ItemClass.DualPistols)*/)
        {
            extraLeftHandPistol.SetActive(true);
            weaponCustomizationSystems[primaryWeaponIndex].SetWeaponMods(mods);
            WeaponCustomizationSystem dualPistolCustomization = extraLeftHandPistol.GetComponent<WeaponCustomizationSystem>();
            dualPistolCustomization.SetWeaponMods(mods);
            SetColor_Network(dualPistolCustomization, ItemColor.FromObject(itemColor, 0));
        }
        else
        {
            extraLeftHandPistol.SetActive(false);
        }
        	
        yield return null;
    }

    [PunRPC]
    private void RPC_SetHeavyWeapon(int ViewID, int heavyWeaponIndex, object[] itemColor)
    {
        StartCoroutine(WaitAndSetHeavy(ViewID, heavyWeaponIndex, itemColor));
    }
    
    private IEnumerator WaitAndSetHeavy(int ViewID, int heavyWeaponIndex, object[] itemColor)
    {
        WaitForSeconds waiter = new WaitForSeconds(0.1f);
        
        while (weapons.Count < 1)
        {
            yield return waiter;
        }
        
        if (weapons.Count < 1)
        {
            yield break;
        }
        SpaceMarine player = PhotonView.Find(ViewID).GetComponent<SpaceMarine>();
            
        if(heavy != null)
        {
            heavy.SetActive(false);
        }
        heavy = weapons[heavyWeaponIndex];
        //heavy.SetActive(true);
        // Pass this weapons customization class to get its color set by the item equipped
        SetColor_Network(weaponCustomizationSystems[heavyWeaponIndex], ItemColor.FromObject(itemColor,0));
        
        PlayerData viewIdData = customization.playerData.Find(x => x.playerNumber == ViewID);
        
        while (viewIdData == null)
        {
            viewIdData = customization.playerData.Find(x => x.playerNumber == ViewID);
            yield return waiter;
        }
        
        viewIdData.SetHeavy((ItemClass)heavyWeaponIndex + 4);

        //viewIdData.playerCustomization.heavy = (ItemClass)heavyWeaponIndex + 4;
        
        if(playerCustomizationType == PlayerCustomizationType.Scene)
        {
            // Set the Weapon to the player ie set the fireprefab / aim transform / data
            weaponCustomizationSystems[heavyWeaponIndex].SetSecondaryWeaponOnPlayer(player);
        }
        
        player.weaponTransformInfo.setWeaponTransforms(player);
        
        yield return null;
    }

    [PunRPC]
    private void RPC_SetArmor(int ViewID, int characterNumber, int helmet, int chest, int gloves, int legs, object[] itemColor1, object[] itemColor2, object[] itemColor3, object[] itemColor4/*, object[] armorStyles*/)
    {
        StartCoroutine(WaitAndSetArmor(ViewID, characterNumber, helmet, chest, gloves, legs, itemColor1, itemColor2, itemColor3, itemColor4));
    }

    private IEnumerator WaitAndSetArmor(int ViewID, int characterNumber, int helmet, int chest, int gloves, int legs, object[] itemColor1, object[] itemColor2, object[] itemColor3, object[] itemColor4 /*, object[] armorStyles*/)
    {
        WaitForSeconds waiter = new WaitForSeconds(0.1f);
        while (characters.Count < 1)
        {
            yield return waiter;
        }

        if (characters.Count < 1)
        {
           yield break;
        }
        if (character != null)
        {
            character.SetActive(false);
        }
        character = characters[characterNumber];
        character.SetActive(true);
        if(playerCustomization.character != Character.SpaceMarine)
        {
            skinnedMeshGeo.GetComponent<SkinnedMeshRenderer>().enabled = false;
            yield break;
        }
        else
        {
            skinnedMeshGeo.GetComponent<SkinnedMeshRenderer>().enabled = true;
        }
        // Turn off active armor pieces
        if(armorPieces.Count > 0)
        {
            foreach(GameObject piece in armorPieces)
            {
                piece.SetActive(false);
            }
        }
        ItemColor[] itemColors = new ItemColor[4];
        itemColors[0] = ItemColor.FromObject(itemColor1,0);
        itemColors[1] = ItemColor.FromObject(itemColor2,0);
        itemColors[2] = ItemColor.FromObject(itemColor3,0);
        itemColors[3] = ItemColor.FromObject(itemColor4,0);
        PlayerData viewIdData = customization.playerData.Find(x => x.playerNumber == ViewID);
        while (viewIdData == null)
        {
            viewIdData = customization.playerData.Find(x => x.playerNumber == ViewID);
            yield return waiter;
        }
        int localNumber = 0;
        
        // Loop through the four armor pieces and activate the appropreate armor piece based on item style
        for(int i = 0; i < playerCustomization.armorStyles.Length; i++)
        {
            switch (i)
            {
               case 0 :
                   localNumber = helmet;
                   break;
               case 1 :
                   localNumber = chest;
                   break;
               case 2 :
                   localNumber = gloves;
                   break;
               case 3 :
                   localNumber = legs;
                   break;
            }
            ArmorCustomizationSystem _armorCustomSystem = armorCustomizationManagers[0].armorCustomizationPieces[i].armorCustomizationSystems[localNumber];
            viewIdData.playerCustomization.armorStyles[i] = (ArmorStyle)localNumber;
            // Enable that gameobject
            _armorCustomSystem.gameObject.SetActive(true);
            // Store that Gameobject to an array to be disabled if changes made
            armorPieces.Add(_armorCustomSystem.gameObject);
            // Set the color of the armor piece based on the item in the equipped slot
            SetColor_Network(_armorCustomSystem, itemColors[i]);
        }
        yield return null;
    }

    [PunRPC]
    private void RPC_SetGenetics(int ViewID, object[] geneticsAbilities)
    {
        //player.geneticPowerManager.SetMutations(PlayerGeneticAbilites.FromObject(geneticsAbilities));

        StartCoroutine(WaitAndSetGenetics(geneticsAbilities));
    }

    private IEnumerator WaitAndSetGenetics(object[] geneticsAbilities)
    {
        WaitForSeconds waiter = new WaitForSeconds(0.1f);
        while (player is null)
        {
            yield return waiter;
        }
        
        player.geneticPowerManager.SetMutations(PlayerGeneticAbilites.FromObject(geneticsAbilities));
        
        yield return null;
    }

    private IEnumerator WaitAndRemapSkeleton()
    {
        WaitForSeconds waiter = new WaitForSeconds(0.1f);
        while (player is null)
        {
            yield return waiter;
        }

        for(int i = 0; i < customization.characterPrefabs.Length; i++)
        {
            //if(i >= 1) break;
            
            GameObject characterPrefab = customization.characterPrefabs[i];
            // Will rework to be able to add a different character. Right now just focus on SpaceMarine
            character = Instantiate(characterPrefab, skinnedMeshGeo.transform.parent.transform);
            characters.Add(character);
            armorCustomizationManagers.Add(character.GetComponent<ArmorCustomizationManager>());
            
            Debug.Log($"SpawnCharacter {player.PV.Controller.NickName} {PhotonNetwork.LocalPlayer.NickName}");


            //armorCustomizationManagers[i].skeletalRemapper.remapSkeleton(skinnedMeshGeo, playerCustomization.character, playerCustomization.armorStyles[(int)ItemType.Chest], false, skinnedMeshGeo, playerCustomizationType);
            armorCustomizationManagers[i].skeletalRemapper.remapSkeletonForNetworkObjects(skinnedMeshGeo, playerCustomization.character, playerCustomization.armorStyles[(int)ItemType.Chest], false, skinnedMeshGeo, playerCustomizationType);
            
            character.SetActive(false);

        }
    }
    
    private IEnumerator RestorePrimaryWeaponPlacement()
    {
        WaitForSeconds waiter = new WaitForSeconds(0.01f);

        while (player.primaryWeapon == null)
        {
            yield return waiter;
        }
		
        if (player.primaryWeapon.GetComponent<FirePrefab>().isRightHanded)
            player.primaryWeapon.transform.parent = player.rightHand;
        else
            player.primaryWeapon.transform.parent = player.leftHand;

        //		if(animator.transform.parent.name == "HydraCommando (Shotgun)")
        //		if(spaceMarine.primaryWeapon.name == "ShotGun")
        //			spaceMarine.secondaryWeapon.transform.parent = spaceMarine.rightHand;
        if (player.secondaryWeapon.name == "FrostGun")
            player.secondaryWeapon.transform.parent = player.rightHand;

        player.GetComponent<WeaponTransformInfo>().resetPrimaryWeaponTransform();
		
        yield return null;
    }

    #endregion

}
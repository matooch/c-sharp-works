using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using Sirenix.OdinInspector;

[System.Serializable]
public class PlayerCustomization
{
    //public int playerNumber;
    public int saveSlot;
    public Character character = Character.SpaceMarine;
    public ArmorStyle[] armorStyles = {ArmorStyle.Default, ArmorStyle.Default, ArmorStyle.Default, ArmorStyle.Default}; //helmet, chest, gloves, legs; // Make array?

    // Weapons
    public ItemClass primary = ItemClass.AssaultRifle;
    public ItemClass heavy = ItemClass.RocketLauncher;

    // Animal Powers
    public PlayerGeneticAbilites playerGeneticAbilites = new PlayerGeneticAbilites();

    // Character Armor Sync Mods
    public CharacterMods[] characterMods = {CharacterMods.None, CharacterMods.None, CharacterMods.None};


    // Weapon Mods
    public WeaponMods weaponMods = new WeaponMods();

    // Might add the Primary and Secondary Afix values. Maybe I make a strut of "Weapon" that holds the ItemClass, and Afix. Opportunity to add other data, like weapon mods? but that is Primary only

    // Add array of item colors 
    [HideInInspector]
    public ItemColor[] itemColors = new ItemColor[6];

    public void Save(bool isInit = false)
    {
        // Save the player customization data to savefile
        // Will very likely need to have the save slot int passed into
        // this function to know which save file to load

        if ((PhotonNetwork.IsConnectedAndReady /*&& GameManager.instance.mainPlayer != null */&& !Equals(PhotonNetwork.LocalPlayer, GameManager.instance.mainPlayer.photonView.Controller)))
        {
            return;
        }

        if(GameManager.instance.gameMode == GameManager.GameMode.Horde) return;

        // Peform check here to make sure the player is properly loaded and not abonmination

        if(GameManager.instance.isPlayerSetupAndReady || isInit)
        {
            ES3.Save("PlayerCustomization", this, Customization.saveFileName);
        }

    }

    public PlayerCustomization Load()
    {
        // Load the player customization data from savefile
        // Will very likely need to have the save slot int passed into
        // this function to know which save file to load
        
        // Its possible to return a PlayerCustomization here to be used
        // in the customization object/class

        PlayerCustomization playerCustomization = new PlayerCustomization();

        if(GameManager.instance.gameMode != GameManager.GameMode.Horde && ES3.FileExists(Customization.saveFileName) && ES3.KeyExists("PlayerCustomization", Customization.saveFileName))
        {
            return (PlayerCustomization)ES3.Load("PlayerCustomization", Customization.saveFileName);
        }

        playerCustomization.saveSlot = Customization.saveSlot;

        playerCustomization.playerGeneticAbilites = new PlayerGeneticAbilites();

        Save(true);
        
        return playerCustomization;
    }


    // Serialize the entire PlayerCustomization Class to an object array for Photon
    public object[] ToObject()
    {   
        // Serialize the Int values on PlayerCustomization
        List<object> data = new List<object>
        {
            saveSlot,
            (int)character,
            (int)armorStyles[0], (int)armorStyles[1], (int)armorStyles[2], (int)armorStyles[3],
            (int)primary, (int)heavy,
            (int)characterMods[0], (int)characterMods[1], (int)characterMods[2],
            (int)weaponMods.scopes, (int)weaponMods.barrels, (int)weaponMods.magazines,

        };

        // Serialize the ItemColor array (6)

        foreach(ItemColor _itemColor in itemColors)
        {
            data.AddRange(_itemColor.ToObject());
        }


        return data.ToArray();
    }

    // Deserialize the object ary to reconstruct a PlayerCustomization class
    public static PlayerCustomization FromObject(object[] data)
    {
        int index = 0;
        // Take data and reconstruct playercustomization
        PlayerCustomization _playerCustomization = new PlayerCustomization
        {
            saveSlot = (int)data[index++],
            character = (Character)data[index++],
            armorStyles = new ArmorStyle[] {(ArmorStyle)data[index++], (ArmorStyle)data[index++], (ArmorStyle)data[index++], (ArmorStyle)data[index++]},
            primary = (ItemClass)data[index++],
            heavy = (ItemClass)data[index++],
            characterMods = new CharacterMods[] {(CharacterMods)data[index++], (CharacterMods)data[index++], (CharacterMods)data[index++]},

            weaponMods = new WeaponMods{scopes = (Scopes)data[index++], barrels = (Barrels)data[index++], magazines = (Magazines)data[index++]}
        };

        // Deserialize ItemColors
        for (int i = 0; i < _playerCustomization.itemColors.Length; i++)
        {
            _playerCustomization.itemColors[i] = ItemColor.FromObject(data, index);
            index += 12; // ItemColor takes 12 bytes of data. 3 Color32 values. Switched to Color32 so it uses less data than Color, which stores floats
        }

        return _playerCustomization;
    }
    
}

[System.Serializable]
public class CharacterData
{   
    public int playerXp = 0;
    public int playerLevel = 1;
    public int earnedStatPoints = 0;
    public int availableStatPoints = 0;
    public int[] playerStatValues = {0,0,0,0}; // Do we want each stat to start at 1?
    public int techCount = 0;
    public int genomeBucks = 0;
    public bool[] keycards  = new bool[4]; // Make these bools, no need for an int
    //{Purple, Red, Green, Blue}

    // Create a way to track what mods are unlocked
    public List<bool[]> modUnlocks = new List<bool[]>{new bool[5], new bool[5], new bool[5]};
    
    [SerializeField]
    public WeaponRank[] weaponRanks = new WeaponRank[9]; // Amount of Primary Weapons

    public CharacterData()
    {
        this.playerXp = 0;
        this.playerLevel = 1;
        this.earnedStatPoints = 0;
        this.availableStatPoints = 0;
        this.playerStatValues = new int[4];
        this.techCount = 0;
        this.genomeBucks = 0;
        this.keycards = new bool[4];
    }

    public void Save()
    {        
        if((GameManager.instance.isPlayerSetupAndReady))
        {
            ES3.Save("CharacterData", this, Customization.saveFileName);
        }
        // Save the weapon ranks to the save file
    }

    public CharacterData Load()
    {
        CharacterData characterData = new CharacterData();

        if(ES3.FileExists(Customization.saveFileName) && ES3.KeyExists("CharacterData", Customization.saveFileName))
        {   
            characterData = (CharacterData)ES3.Load("CharacterData", Customization.saveFileName);
        }
        
        return characterData;
    }

    public object[] ToObject()
    {
        List<object> data = new List<object>()
        {
            playerXp,
            playerLevel,
            earnedStatPoints,
            availableStatPoints,
            playerStatValues[0], playerStatValues[1], playerStatValues[2], playerStatValues[3],
            techCount,
            genomeBucks,
            keycards[0], keycards[1], keycards[2], keycards[3]
        };

        foreach(int _rank in GetWeaponRankLevels())
        {
            object _data = _rank;
            data.Add(_data);
        }

        return data.ToArray();
    }
    
    // Deserialize the object ary to reconstruct a CharacterData class
    public static CharacterData FromObject(object[] data)
    {
        int index = 0;
        // Take data and reconstruct playercustomization
        CharacterData _characterData = new CharacterData
        {
            playerXp = (int)data[index++],
            playerLevel = (int)data[index++],
            earnedStatPoints = (int)data[index++],
            availableStatPoints = (int)data[index++],
            playerStatValues = new int[] {(int)data[index++],(int)data[index++],(int)data[index++],(int)data[index++]},
            techCount = (int)data[index++],
            genomeBucks = (int)data[index++],
            keycards = new bool[] {(bool)data[index++],(bool)data[index++],(bool)data[index++],(bool)data[index++]},

        };

        return _characterData;
    }

    public void IncreasePlayerLevel()
    {
        this.playerLevel += 1;
        this.earnedStatPoints += 1;
        this.availableStatPoints += 1;

        this.Save();
    }

    public int[] GetWeaponRankLevels()
    {
        int[] _levels = new int[9];

        for(int i = 0; i < _levels.Length; i++)
        {
            _levels[i] = weaponRanks[i].level;
        }
        return _levels;
    }

    public static System.Action<int> genomeBucksUpdate;

    public void AddGenomeBucks(int value)
    {
        genomeBucks += value;

        genomeBucksUpdate?.Invoke(genomeBucks);
    }
}

// Stored as a list on customization
// We store a reference to each player so we can access their customization data and they can save their data appropreately
[System.Serializable]
public class PlayerData
{
    public int playerNumber; // used to get which Photon Player we are referencing
    public PlayerCustomization playerCustomization = new PlayerCustomization();
    public CharacterData characterData = new CharacterData(); 
    public PlayerLevelManager playerLevelManager = new PlayerLevelManager();


    // Setting the playercustomization Values ----------------------------------------------------------
    public void SetPrimary(ItemClass itemClass)
    {
        playerCustomization.primary = itemClass;
    }

    public void SetHeavy(ItemClass itemClass)
    {
        playerCustomization.heavy = itemClass;
    }

    public void SetMods(WeaponMods weaponMods)
    {
        playerCustomization.weaponMods = weaponMods;
    }

    public void SetArmor(ItemType itemType, ArmorStyle armorStyle)
    {
        playerCustomization.armorStyles[(int)itemType] = armorStyle;
    }

    public void Reset()
    {
        playerCustomization = new PlayerCustomization();
        playerCustomization.playerGeneticAbilites = new PlayerGeneticAbilites();
        characterData = new CharacterData();
    }


    // Make a version of this Function that does the upgrading internally. That way we dont need to pass in all the data we already have access to here
    public void SetGenetics(Genes gene, int mutationValue = -1, bool isSecondary = false, int unlockValue = -1)
    {
        if(isSecondary)
        {
           playerCustomization.playerGeneticAbilites.secondaryGene = mutationValue == 0 ?  Genes.None : gene;

            if(mutationValue != -1)
            {
                playerCustomization.playerGeneticAbilites.secondaryMutationLevel[((int)gene) - 1] = mutationValue;
            }
        }
        else
        {
            playerCustomization.playerGeneticAbilites.primaryGene = gene;

            if(mutationValue != -1)
            {
                playerCustomization.playerGeneticAbilites.mutationLevels[((int)gene) - 1] = mutationValue;
            }

            
        }

        if(unlockValue != -1)
        {
            playerCustomization.playerGeneticAbilites.unlockedPowers[((int)gene) - 1].SetUnlock(unlockValue);
        }
    }


}

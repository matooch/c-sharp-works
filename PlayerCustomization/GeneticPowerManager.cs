using BehaviorDesigner.Runtime.Tasks;
using Photon.Pun;
using RootMotion.FinalIK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneticPowerManager : MonoBehaviour
{
    [SerializeField]
    public PlayerGeneticAbilites playerGeneticAbilites;
    public GeneticPower[] geneticPower;

    // 0 means the level is locked
    // 1 - 3 are the levels of the mutation
    private Customization customization;
    private PlayerCustomization playerCustomization;
    private SpaceMarine spaceMarine;
    private bool init = false;

    //The amount of uses a genetic power has before reducing to 0 and either reseting or disabling a power.
    public int geneticCharge;
    public int primary;
    public int secondary;

    // Here we will enable and disable the genetic powers. These exist on the player
    private void Awake()
    {
        spaceMarine = GetComponent<SpaceMarine>();
    }

    public void Update()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            if (!spaceMarine.PV.IsMine)
                return;
        }

        if (playerGeneticAbilites.isSecondaryGeneUnlock && spaceMarine.player.GetButton("WieldSecondaryWeapon"))
        {
            Debug.Log("Secondary Unlocked and held");
            if (spaceMarine.player.GetButtonDown("Use Gene"))
            {
                Debug.Log("Use Secondary Genetic");
                if (secondary != -1)
                {
                    geneticPower[secondary].Trigger(playerGeneticAbilites.secondaryMutationLevel[secondary]);
                }
            }
        } 
        else if (spaceMarine.player.GetButtonDown("Use Gene"))
        {
            Debug.Log("Use Primary Genetic");
            if (primary != -1)
            {
                geneticPower[primary].Trigger(playerGeneticAbilites.mutationLevels[primary]);
                Debug.Log("BAM! Primary used");
            }
        }
    }

    public void Init()
    {
        if(init) return;

        spaceMarine = GetComponent<SpaceMarine>();

        for (int i = 0; i < geneticPower.Length; i++)
        {
            geneticPower[i].Init();
        }
        init = true;
    }


    // Set these also from the animal power statues
    public void SetMutations(PlayerGeneticAbilites _playerGeneticAbilities)
    {
        Init();

        playerGeneticAbilites = _playerGeneticAbilities;

        // Disable current the abilites on player
        foreach(GeneticPower _geneticPower in geneticPower)
        {
            _geneticPower.enabled = false;
            _geneticPower.isSecondary = false;
        }

        primary = (int)playerGeneticAbilites.primaryGene - 1; // Since there is a "None" gene, we minus 1 since Lion is 0 in array
        secondary = (int)playerGeneticAbilites.secondaryGene - 1;

        if(primary != -1)
        {
            // Enable the primary ability
            geneticPower[primary].SetAbility(playerGeneticAbilites.mutationLevels[primary], false);
            geneticPower[primary].ForceCooldownOnSwap();

        }

        // Enable Secondary Ability if unlocked
        if (secondary != -1)
        {
            geneticPower[secondary].SetAbility(playerGeneticAbilites.secondaryMutationLevel[secondary], true);
            geneticPower[secondary].ForceCooldownOnSwap();

        }

    }
    

    // Saving and Loading Happens on the PlayerCustomization class since the we are already saving the PlayerCustomization class 
    public void SaveGenetics()
    {
        ES3.Save("GeneticAbilities " + playerCustomization.saveSlot, playerGeneticAbilites, Customization.saveFileName);

    }

    public void LoadGenetics()
    {
        if(ES3.FileExists(Customization.saveFileName) && ES3.KeyExists("GeneticAbilities " + playerCustomization.saveSlot, Customization.saveFileName))
        {
            playerGeneticAbilites = (PlayerGeneticAbilites)ES3.Load("GeneticAbilities " + playerCustomization.saveSlot, Customization.saveFileName);
        }
        else
        {
            playerGeneticAbilites = new PlayerGeneticAbilites();
            //SaveGenetics();
        }

        //customization.SetGenetics(playerGeneticAbilites);
    }

    public void SetTemporaryMutations(Genes _gene, int _value = 1)
    {
        //When picking up a genetic ability power up, add a genetic charge to the player.
        //We should reset this if the genetic power is a different gene than currently equipped.
        //TO DO: Compare _gene to equipped gene and decide logic then proceed.
        geneticCharge += 1;
        StartCoroutine(SetTemporaryMutationsEnumerator(_gene, _value));
    }

    IEnumerator SetTemporaryMutationsEnumerator(Genes _gene, int _value)
    {
        //Starting stuff
        Genes previousGene = playerGeneticAbilites.primaryGene;

        // Create a new list of the previous mutation settings
        List<int> previousMutations = new List<int>();

        for(int i = 0 ; i < playerGeneticAbilites.mutationLevels.Length; i++)
        {
            previousMutations.Add(playerGeneticAbilites.mutationLevels[i]);
        }

        // We dont need to change the unlock value as that was causing the issue of them retaining after use
        spaceMarine.getCustomInfo.playerData.SetGenetics(_gene, _value, false);
        spaceMarine.getCustomInfo.SetPlayerCustomization();
        // Enable the primary ability
        SetMutations(playerGeneticAbilites);

        if(GameManager.instance.gameMode == GameManager.GameMode.Horde)
        {
            // A non temporary application of animal powers
            yield break;
        }

        // add timer - Replace timer with charge.
//        yield return new WaitForSeconds(5);
        yield return new WaitUntil(() => geneticCharge == 0);

        // Reapply all prevous settings during this temporary use
        playerGeneticAbilites.mutationLevels = previousMutations.ToArray();

        spaceMarine.getCustomInfo.playerData.SetGenetics(previousGene, previousMutations[((int)previousGene) - 1], false);
        spaceMarine.getCustomInfo.SetPlayerCustomization();

        SetMutations(playerGeneticAbilites);

    }

    public void RecalculateMagicCosts()
    {
        foreach (var power in geneticPower)
        {
            power.BuffMagicAmount(); // Or ResetAndRebuffMagicAmount() if needed
        }
    }

}

[System.Serializable]
public class PlayerGeneticAbilites
{
    public Genes primaryGene = Genes.Lion, secondaryGene;
    public int[] mutationLevels = {1, 0, 0 ,0, 0, 0, 0, 0};
    public int[] secondaryMutationLevel = new int[8];
    //public int[] unlockedMutations = {1, 0, 0 ,0, 0, 0, 0, 0};
    public AnimalPowerUnlock[] unlockedPowers;

    public bool isSecondaryGeneUnlock;

    public PlayerGeneticAbilites()
    {
        primaryGene = Genes.Lion;
        secondaryGene = Genes.None;
        mutationLevels = new int[] {1, 0, 0 ,0, 0, 0, 0, 0};
        unlockedPowers = new AnimalPowerUnlock[]
        {
            new AnimalPowerUnlock{level1 = true},
            new AnimalPowerUnlock(),
            new AnimalPowerUnlock(),
            new AnimalPowerUnlock(),
            new AnimalPowerUnlock(),
            new AnimalPowerUnlock(),
            new AnimalPowerUnlock(),
            new AnimalPowerUnlock(),
        
        };
    }

    public object[] ToObject()
    {
        return new object[] 
        {
            (int)primaryGene, 
            (int)secondaryGene, 
            mutationLevels[0],
            mutationLevels[1],
            mutationLevels[2],
            mutationLevels[3],
            mutationLevels[4],
            mutationLevels[5],
            mutationLevels[6],
            mutationLevels[7]

        };
    }

    public int GetMutationLevel(Genes _gene, bool isSecondary)
    {
        if(_gene == Genes.None) return 0;
        return isSecondary ? secondaryMutationLevel[(int)_gene - 1] : mutationLevels[(int)_gene - 1];
    }

    public bool GetMutationUnlock(Genes _gene, int _mutation)
    {
        if(_gene == Genes.None) return false;
        return unlockedPowers[(int)_gene - 1].GetUnlock(_mutation);
    }

    public int GetSecondaryMutation()
    {
        return GetMutationLevel(secondaryGene, true);
    }

    public int GetPrimaryMutaion()
    {
        return GetMutationLevel(secondaryGene, false);
    }

    public void SetSecondaryPower()
    {
        isSecondaryGeneUnlock = true;
    }

    public static PlayerGeneticAbilites FromObject(object[] data)
    {
        int index = 0;
        
        PlayerGeneticAbilites geneticAbilites = new PlayerGeneticAbilites()
        {
            primaryGene = (Genes)data[index++],
            secondaryGene = (Genes)data[index++],
            mutationLevels = new []{(int)data[index++],(int)data[index++],(int)data[index++],(int)data[index++],(int)data[index++],(int)data[index++],(int)data[index++],(int)data[index++]}
        };
        return geneticAbilites;
    }
}

[System.Serializable]
public struct AnimalPowerUnlock 
{
    public bool level1, level2, level3;

    public bool GetUnlock(int value)
    {
        if(value == 1) return level1;
        if(value == 2) return level2;
        if(value == 3) return level3;

        return false;
    }

    public void SetUnlock(int value)
    {
        if(value == 1) level1 = true;
        if(value == 2) level2 = true;
        if(value == 3) level3 = true;
    }
}

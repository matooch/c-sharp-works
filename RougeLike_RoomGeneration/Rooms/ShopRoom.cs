using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopRoom : Room
{
//    public ShopTrigger[] shopTriggers;

    public override void Init(int _roomID)
    {
        base.Init(_roomID);

    }

    public override void StartRoom()
    {
        base.StartRoom();
        GameManager.instance.GenomeTrackFadeOut();
        EndRoom();
    }

    public override void EndRoom()
    {
        base.EndRoom();

        EnableBarriers(false);
    }
}

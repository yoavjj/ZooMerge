using UnityEngine;

public class LevelArtController_Popup : LevelArtController
{
    protected override void ShowLvlStage(int stageId)
    {
        base.ShowLvlStage(stageId);

        // After the base spawns it, play idle
        PlayIdle();
    }
}
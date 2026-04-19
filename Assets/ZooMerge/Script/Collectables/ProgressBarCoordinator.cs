using UnityEngine;
using static BallEventManager;

public class ProgressBarCoordinator
{
    private readonly EnemyStripBuilder stripBuilder;
    private readonly EnemyIconController iconController;
    private readonly RectTransform widthTarget;

    public ProgressBarCoordinator(
        EnemyStripBuilder stripBuilder,
        EnemyIconController iconController,
        RectTransform widthTarget)
    {
        this.stripBuilder = stripBuilder;
        this.iconController = iconController;
        this.widthTarget = widthTarget;
    }

    public void RebuildStrip(MergeLevel level, EnemyProgressConfig config, float width)
    {
        iconController.Clear();

        stripBuilder.Build(
            iconController.GetRawList(),
            config,
            width,
            level.enemy_data,
            i => { /* building index context no longer needed */ },
            out float _
        );

        iconController.SortByPosition();
    }

    public int GetInitialIconIndex(GameOverReason reason)
    {
        int total = iconController.Icons.Count;

        if (total == 0)
            return -1;

        if (reason == GameOverReason.Won)
            return total - 1;

        return Mathf.Clamp(MergeLevelManager.CurrentEnemyIndex, 0, total - 1);
    }
}

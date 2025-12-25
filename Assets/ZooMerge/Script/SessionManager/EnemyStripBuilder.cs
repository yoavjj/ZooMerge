using UnityEngine;
using System.Collections.Generic;

public class EnemyStripBuilder
{
    private readonly RectTransform container;
    private readonly GameObject linePrefab;
    private readonly BallSet ballSet;

    public EnemyStripBuilder(
        RectTransform container,
        GameObject linePrefab,
        BallSet ballSet)
    {
        this.container = container;
        this.linePrefab = linePrefab;
        this.ballSet = ballSet;
    }

    public void Build(
        List<EnemyIconNode> icons,
        EnemyProgressConfig config,
        float containerWidth,
        List<EnemyData> enemies,
        System.Action<int> registerIndexCallback,
        out float finalLineXPos)
    {
        finalLineXPos = 0f;
        icons.Clear();

        if (enemies == null || enemies.Count == 0)
            return;

        config.GetPadding(enemies.Count, out float leftPadding, out float rightPadding);

        float usableW = Mathf.Max(0f, containerWidth - leftPadding - rightPadding);
        float spacing = usableW / (enemies.Count * 2 - 1);

        for (int i = 0; i < enemies.Count; i++)
        {
            registerIndexCallback?.Invoke(i);

            var iconPrefab = ballSet.GetEnemyIconPrefabById(enemies[i].id.ToString());
            if (iconPrefab != null)
            {
                var iconGO = Object.Instantiate(iconPrefab, container);
                Place(iconGO.transform as RectTransform, leftPadding + spacing * (i * 2));
            }

            if (i < enemies.Count - 1 && linePrefab != null)
            {
                var lineGO = Object.Instantiate(linePrefab, container);
                float x = leftPadding + spacing * (i * 2 + 1);
                Place(lineGO.transform as RectTransform, x);

                if (i == enemies.Count - 2)
                    finalLineXPos = x;
            }
        }
    }

    private static void Place(RectTransform rt, float x)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0f);
    }
}

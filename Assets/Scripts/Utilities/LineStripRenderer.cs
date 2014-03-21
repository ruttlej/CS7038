﻿using System.Collections.Generic;
using UnityEngine;

public class LineStripRenderer : Component
{
    private readonly List<LineRenderer> linePool;
    private int enabledLinesCount;
    private readonly Component parent;
	private readonly Material material;

	public LineStripRenderer(Component parent, Material material)
    {
        linePool = new List<LineRenderer>();
        enabledLinesCount = 0;
        this.parent = parent;
		this.material = material;
    }

    public void Draw(List<Vector3> points)
    {
        var newLinesCount = points.Count - 1;

        EnableRenderers(newLinesCount);

        for (var i = 0; i < newLinesCount; i++)
        {
            linePool[i].SetPosition(0, points[i]);
            linePool[i].SetPosition(1, points[i + 1]);
        }
    }

    private void EnableRenderers(int newLinesCount)
    {
        for (var i = newLinesCount; i < linePool.Count; i++)
        {
            linePool[i].enabled = false;
        }

        for (var i = enabledLinesCount; i < newLinesCount; i++)
        {
            if (i < linePool.Count)
            {
                linePool[i].enabled = true;
            }
            else
            {
                var laser = new GameObject("Laser");
                laser.transform.parent = parent.transform;

                var line = laser.AddComponent<LineRenderer>();
				line.material = material;
				line.sortingOrder = -1000 + 1;
                line.SetWidth(0.1f, 0.1f);
                line.SetVertexCount(2);

                linePool.Add(line);
            }
        }

        enabledLinesCount = newLinesCount;
    }
}

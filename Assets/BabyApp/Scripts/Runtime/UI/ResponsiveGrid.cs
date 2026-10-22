using UnityEngine;
using UnityEngine.UI;

namespace BabyApp.UI
{
    /// <summary>
    /// 讓 GridLayoutGroup 的格子永遠是正方形、而且剛好塞滿可用空間：
    /// 4 顆排 2×2、5～6 顆排 3×2，依此類推。螢幕尺寸或按鈕數量變了就重算。
    /// </summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    public class ResponsiveGrid : MonoBehaviour
    {
        [SerializeField] private float spacing = 36f;
        [SerializeField] private float maxCell = 520f;

        private GridLayoutGroup _grid;
        private RectTransform _rt;

        private void Awake()
        {
            _grid = GetComponent<GridLayoutGroup>();
            _rt = (RectTransform)transform;
            _grid.spacing = new Vector2(spacing, spacing);
            _grid.childAlignment = TextAnchor.MiddleCenter;
            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        }

        private void OnEnable() => Recalculate();
        private void OnRectTransformDimensionsChange() => Recalculate();
        private void OnTransformChildrenChanged() => Recalculate();

        private void Recalculate()
        {
            if (_grid == null || _rt == null) return;

            int count = 0;
            foreach (Transform child in transform)
            {
                if (child.gameObject.activeSelf) count++;
            }
            if (count == 0) return;

            int columns = count <= 4 ? Mathf.Min(count, 2) : Mathf.CeilToInt(Mathf.Sqrt(count));
            if (count == 3) columns = 3;
            int rows = Mathf.CeilToInt((float)count / columns);

            var size = _rt.rect.size;
            float cellByWidth = (size.x - spacing * (columns - 1)) / columns;
            float cellByHeight = (size.y - spacing * (rows - 1)) / rows;
            float cell = Mathf.Min(cellByWidth, cellByHeight, maxCell);
            if (cell <= 0f) return;

            _grid.constraintCount = columns;
            _grid.cellSize = new Vector2(cell, cell);
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class BoardTileView : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
{
    [SerializeField] private SpriteRenderer _spriteRenderer = null!;
    [SerializeField] private TextMesh? _labelText;

    public int Row { get; private set; }
    public int Col { get; private set; }
    public BoardController? Controller { get; private set; }

    public void Initialize(BoardController controller, int row, int col)
    {
        Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        Row = row;
        Col = col;
    }

    public void SetTile(IslandQuest.Match3.Tile tile)
    {
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = GetColorForType(tile.Type);
        }

        if (_labelText != null)
        {
            // Boosters get a distinct two-letter code so they can't be mistaken
            // for a plain tile — a single letter collides (LeafWheel vs Leaf,
            // SolarFlare/SporeCloud vs Sun).
            var label = tile.Booster != IslandQuest.Match3.BoosterType.None
                ? GetBoosterLabel(tile.Booster)
                : tile.Type.ToString().Substring(0, 1);
            if (tile.HasCreditBag)
                label += "*";
            _labelText.text = label;
        }
    }

    private static string GetBoosterLabel(IslandQuest.Match3.BoosterType booster)
    {
        return booster switch
        {
            IslandQuest.Match3.BoosterType.BloomBurst => "BB",
            IslandQuest.Match3.BoosterType.LeafWheel => "LW",
            IslandQuest.Match3.BoosterType.TidalClear => "TC",
            IslandQuest.Match3.BoosterType.SolarFlare => "SF",
            IslandQuest.Match3.BoosterType.SporeCloud => "SC",
            IslandQuest.Match3.BoosterType.DeepSurge => "DS",
            _ => "?",
        };
    }

    private static Color GetColorForType(IslandQuest.Match3.TileType type)
    {
        return type switch
        {
            IslandQuest.Match3.TileType.Flower => new Color(0.90f, 0.40f, 0.90f),
            IslandQuest.Match3.TileType.Leaf => new Color(0.20f, 0.85f, 0.25f),
            IslandQuest.Match3.TileType.Wave => new Color(0.25f, 0.55f, 0.95f),
            IslandQuest.Match3.TileType.Sun => new Color(0.98f, 0.85f, 0.20f),
            IslandQuest.Match3.TileType.Mushroom => new Color(1.00f, 0.55f, 0.10f),
            IslandQuest.Match3.TileType.Coral => new Color(0.90f, 0.25f, 0.25f),
            _ => Color.white,
        };
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        Controller?.OnTilePointerDown(this);
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        Controller?.OnTilePointerEnter(this);
    }

    void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    {
        Controller?.OnTilePointerUp(this);
    }
}

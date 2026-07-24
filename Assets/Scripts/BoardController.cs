using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IslandQuest.Match3;

public sealed class BoardController : MonoBehaviour
{
    [SerializeField] private BoardTileView tilePrefab = null!;
    [SerializeField] private Transform tilesParent = null!;
    [SerializeField] private LevelDataAsset? levelDataAsset;
    [SerializeField] private float tileSpacing = 1.1f;
    [SerializeField] private float swapAnimationDuration = 0.12f;
    [SerializeField] private float cascadeStepDelay = 0.15f;
    // When a coordinator (Task 16's GameFlowController) drives the board it
    // calls Initialize itself with the chosen level, so auto-init on Start is
    // turned off in that scene. scene1 (Task 9's standalone demo) leaves it on.
    [SerializeField] private bool initializeOnStart = true;

    /// <summary>Raised once per <b>committed</b> move (a swap that matched or a
    /// manual booster activation) with that move's fully-resolved
    /// <see cref="CascadeResult"/>. Not raised for a reverted no-match swap,
    /// which doesn't count as a move. Task 16's session layer folds these into a
    /// <see cref="LevelSession"/>.</summary>
    public event Action<CascadeResult>? MoveResolved;

    /// <summary>When false, the board ignores pointer input — used by the
    /// coordinator to freeze the board once a level is won or lost.</summary>
    public bool InputEnabled { get; set; } = true;

    private Board? _board;
    private BoardConfig? _config;
    private System.Random? _rng;
    private readonly Dictionary<(int Row, int Col), BoardTileView> _tileViews = new();
    private (int Row, int Col)? _selectedCell;
    private bool _dragging;
    private bool _isBusy;

    private void Start()
    {
        if (!initializeOnStart)
            return;

        if (_board is null)
        {
            if (levelDataAsset != null)
                Initialize(levelDataAsset.ToLevelData());
            else
                Initialize(new BoardConfig());
        }
    }

    public void Initialize(BoardConfig config)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));
        _config = config;
        _rng = config.Seed.HasValue ? new System.Random(config.Seed.Value) : new System.Random();
        _board = BoardGenerator.Generate(config);
        CreateBoardViews();
        RefreshAllTiles();
    }

    public void Initialize(LevelData levelData, int? seed = null)
    {
        if (levelData is null) throw new ArgumentNullException(nameof(levelData));
        Initialize(levelData.ToBoardConfig(seed));
    }

    private void CreateBoardViews()
    {
        if (_board is null)
            throw new InvalidOperationException("Board must be initialized before creating tile views.");

        foreach (Transform child in tilesParent)
            Destroy(child.gameObject);

        _tileViews.Clear();

        for (int r = 0; r < _board.Rows; r++)
        {
            for (int c = 0; c < _board.Columns; c++)
            {
                var view = Instantiate(tilePrefab, tilesParent);
                view.Initialize(this, r, c);
                view.transform.localPosition = GetTilePosition(r, c);
                _tileViews[(r, c)] = view;
            }
        }
    }

    private Vector3 GetTilePosition(int row, int col)
    {
        float x = col * tileSpacing;
        float y = -row * tileSpacing;
        return new Vector3(x, y, 0f);
    }

    public void OnTilePointerDown(BoardTileView tileView)
    {
        if (_board is null || _isBusy || !InputEnabled)
            return;

        _selectedCell = (tileView.Row, tileView.Col);
        _dragging = true;
    }

    public void OnTilePointerEnter(BoardTileView tileView)
    {
        if (!_dragging || _selectedCell is null || _board is null || _isBusy || !InputEnabled)
            return;

        var target = (tileView.Row, tileView.Col);
        if (!AreAdjacent(_selectedCell.Value, target))
            return;

        StartCoroutine(SwapAndResolveCoroutine(_selectedCell.Value, target));
        _selectedCell = null;
        _dragging = false;
    }

    public void OnTilePointerUp(BoardTileView tileView)
    {
        _selectedCell = null;
        _dragging = false;
    }

    private static bool AreAdjacent((int Row, int Col) a, (int Row, int Col) b)
    {
        int rowDelta = Math.Abs(a.Row - b.Row);
        int colDelta = Math.Abs(a.Col - b.Col);
        return (rowDelta == 1 && colDelta == 0) || (rowDelta == 0 && colDelta == 1);
    }

    private IEnumerator SwapAndResolveCoroutine((int Row, int Col) source, (int Row, int Col) target)
    {
        if (_board is null || _config is null)
            yield break;

        _isBusy = true;

        if (_rng is null)
            _rng = _config.Seed.HasValue ? new System.Random(_config.Seed.Value) : new System.Random();

        var sourceView = _tileViews[source];
        var targetView = _tileViews[target];
        var sourcePosition = sourceView.transform.localPosition;
        var targetPosition = targetView.transform.localPosition;

        yield return AnimateSwap(sourceView, targetView, sourcePosition, targetPosition);

        // Requirement 5c precedence (design.md §3.6): attempt manual booster
        // activation first. If it fires, it has already committed the swap and
        // returned the cleared cells — feed those straight into the cascade.
        var manual = SwapEngine.TryManualActivationSwap(_board, source.Row, source.Col, target.Row, target.Col, _rng);
        if (manual.Triggered)
        {
            RefreshAllTiles();
            yield return new WaitForSeconds(cascadeStepDelay);

            var manualResult = CascadeEngine.ResolveCascadeFrom(_board, manual.ClearedCells, _config, _rng);
            RefreshAllTiles();
            yield return new WaitForSeconds(cascadeStepDelay);

            _isBusy = false;
            MoveResolved?.Invoke(manualResult);
            yield break;
        }

        // Otherwise fall through to the ordinary match-or-revert swap.
        var result = SwapEngine.TrySwap(_board, source.Row, source.Col, target.Row, target.Col);
        if (!result.Success)
        {
            yield return AnimateSwap(sourceView, targetView, targetPosition, sourcePosition);
            _isBusy = false;
            yield break;
        }

        RefreshAllTiles();
        yield return new WaitForSeconds(cascadeStepDelay);

        var cascadeResult = CascadeEngine.ResolveCascade(_board, _config, _rng);
        RefreshAllTiles();
        yield return new WaitForSeconds(cascadeStepDelay);

        _isBusy = false;
        MoveResolved?.Invoke(cascadeResult);
    }

    private IEnumerator AnimateSwap(BoardTileView sourceView, BoardTileView targetView, Vector3 sourcePosition, Vector3 targetPosition)
    {
        float elapsed = 0f;

        while (elapsed < swapAnimationDuration)
        {
            float t = elapsed / swapAnimationDuration;
            sourceView.transform.localPosition = Vector3.Lerp(sourcePosition, targetPosition, t);
            targetView.transform.localPosition = Vector3.Lerp(targetPosition, sourcePosition, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        sourceView.transform.localPosition = targetPosition;
        targetView.transform.localPosition = sourcePosition;
    }

    private void RefreshAllTiles()
    {
        if (_board is null)
            return;

        foreach (var kvp in _tileViews)
        {
            var position = kvp.Key;
            var view = kvp.Value;
            view.SetTile(_board[position.Row, position.Col]);
            view.transform.localPosition = GetTilePosition(position.Row, position.Col);
        }
    }
}

using System;
using System.Collections;
using UnityEngine;
using IslandQuest.Match3;

/// <summary>
/// Task 16b coordinator: ties the level catalog, the board, and the
/// <see cref="LevelSession"/> together into a playable loop —
/// <b>level-select → play → results → (replay / next / menu)</b>.
///
/// The level-select grid and the results panel are built <i>procedurally</i>
/// (white-square <see cref="SpriteRenderer"/> backgrounds + built-in-font
/// <see cref="TextMesh"/> labels, clicked through the scene's
/// <c>Physics2DRaycaster</c> via <see cref="ClickableView"/>), so the scene YAML
/// only needs a camera, an EventSystem, this object, and the board. Best-star
/// records live in an in-memory <see cref="LevelRecordStore"/> (persistence is a
/// future Core/SaveSystem concern — see Task 8 / design.md §7.4).
/// </summary>
public sealed class GameFlowController : MonoBehaviour
{
    [SerializeField] private BoardController board = null!;
    // Parent of the board visuals; hidden while the level-select menu is up.
    [SerializeField] private GameObject boardRoot = null!;

    // Camera framing from scene1: an orthographic view centred on (4.4, -4.4)
    // spanning the 9×9 board (x 0..8.8, y 0..-8.8). The menus lay out inside it.
    private static readonly Vector3 ViewCenter = new Vector3(4.4f, -4.4f, 0f);

    // Menu framing shows just the board footprint; play framing zooms out and
    // shifts up so a HUD strip opens above the board (the board otherwise fills
    // the whole view). Centre X stays on the board (4.4).
    private const float MenuOrthoSize = 5f, MenuCenterY = -4.4f;
    private const float PlayOrthoSize = 5.7f, PlayCenterY = -3.7f;

    private readonly ILevelRecordStore _records = new LevelRecordStore();

    private GameObject? _levelSelectRoot;
    private GameObject? _resultsRoot;
    private GameObject? _hudRoot;
    private TextMesh? _hudLine1;
    private TextMesh? _hudLine2;

    private LevelData? _currentLevel;
    private LevelSession? _session;

    private void Start()
    {
        board.MoveResolved += OnMoveResolved;
        ShowLevelSelect();
    }

    private void OnDestroy()
    {
        if (board != null)
            board.MoveResolved -= OnMoveResolved;
    }

    // ---- Flow -------------------------------------------------------------

    private void ShowLevelSelect()
    {
        _session = null;
        _currentLevel = null;
        board.InputEnabled = false;

        DestroyResults();
        DestroyHud();
        if (boardRoot != null) boardRoot.SetActive(false);
        SetCameraFraming(MenuOrthoSize, MenuCenterY);

        // Rebuilt each time so best-star records refresh after a level is beaten.
        if (_levelSelectRoot != null) Destroy(_levelSelectRoot);
        _levelSelectRoot = BuildLevelSelect();
    }

    private void StartLevel(LevelData level)
    {
        _currentLevel = level;
        _session = new LevelSession(level);

        if (_levelSelectRoot != null) _levelSelectRoot.SetActive(false);
        DestroyResults();

        if (boardRoot != null) boardRoot.SetActive(true);
        SetCameraFraming(PlayOrthoSize, PlayCenterY);
        board.InputEnabled = true;
        board.Initialize(level);   // fresh random board (Task 9 render path)

        BuildHud();
        RefreshHud();
    }

    private void OnMoveResolved(CascadeResult move)
    {
        // Only meaningful while a level is actually in progress.
        if (_session == null || _session.Outcome != LevelOutcome.InProgress)
            return;

        _session.ApplyMove(move);
        RefreshHud();

        if (_session.Outcome != LevelOutcome.InProgress)
            EndLevel();
    }

    private void EndLevel()
    {
        if (_session == null || _currentLevel == null)
            return;

        board.InputEnabled = false;

        var result = _session.GetResult();
        bool won = _session.Outcome == LevelOutcome.Won;
        if (won)
            _records.Record(_currentLevel.LevelNumber, result.Stars);

        // A dedicated results screen: hide the board + HUD so it isn't a dim
        // overlay on the live tiles (StartLevel re-shows the board on replay/next).
        DestroyHud();
        if (boardRoot != null) boardRoot.SetActive(false);

        _resultsRoot = BuildResults(_currentLevel, won, result);
    }

    private static void SetCameraFraming(float orthoSize, float centerY)
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.orthographicSize = orthoSize;
        var p = cam.transform.position;
        cam.transform.position = new Vector3(ViewCenter.x, centerY, p.z);
    }

    // ---- In-play HUD (procedural) -----------------------------------------

    private void BuildHud()
    {
        DestroyHud();
        _hudRoot = new GameObject("HUD");
        _hudRoot.transform.SetParent(transform, false);

        // A strip across the top of the play framing (view top ≈ y 2.0, board
        // top edge ≈ y 0.5), so the panel sits clear of the top board row.
        MakePanel(_hudRoot.transform, new Vector3(ViewCenter.x, 1.35f, 0f), new Vector2(11.5f, 1.4f),
            new Color(0.08f, 0.10f, 0.16f, 0.85f), sortingOrder: 20);

        _hudLine1 = MakeLabel(_hudRoot.transform, new Vector3(ViewCenter.x, 1.62f, -0.2f),
            "", new Color(0.95f, 0.9f, 0.6f), scale: 0.1f, sortingOrder: 21);
        _hudLine2 = MakeLabel(_hudRoot.transform, new Vector3(ViewCenter.x, 1.05f, -0.2f),
            "", Color.white, scale: 0.1f, sortingOrder: 21);
    }

    private void RefreshHud()
    {
        if (_hudLine1 == null || _hudLine2 == null || _session == null || _currentLevel == null)
            return;

        var obj = _currentLevel.Objective;
        _hudLine1.text = $"L{_currentLevel.LevelNumber}  ·  {ObjectiveGoalText(obj)}";
        _hudLine2.text =
            $"{ProgressText(obj, _session)}      Moves {_session.MovesRemaining}/{_currentLevel.MoveLimit}      Score {_session.Score}";
    }

    private void DestroyHud()
    {
        if (_hudRoot != null)
        {
            Destroy(_hudRoot);
            _hudRoot = null;
        }
        _hudLine1 = null;
        _hudLine2 = null;
    }

    private static string ObjectiveGoalText(LevelObjective obj) => obj.Type switch
    {
        LevelObjectiveType.Score => $"Goal: reach {obj.Target} pts",
        LevelObjectiveType.Collect => $"Goal: clear {obj.Target} tiles",
        LevelObjectiveType.CollectBags => $"Goal: collect {obj.Target} bags",
        _ => "Goal: —",
    };

    private static string ProgressText(LevelObjective obj, LevelSession session) => obj.Type switch
    {
        LevelObjectiveType.Score => $"Points {session.Score}/{obj.Target}",
        LevelObjectiveType.Collect => $"Tiles {session.TilesCleared}/{obj.Target}",
        LevelObjectiveType.CollectBags => $"Bags {session.BagsCollected}/{obj.Target}",
        _ => "",
    };

    private LevelData? NextLevel(LevelData after)
    {
        var all = LevelData.AllLevels;
        for (int i = 0; i < all.Count - 1; i++)
            if (all[i].LevelNumber == after.LevelNumber)
                return all[i + 1];
        return null; // already the last level
    }

    // ---- Level-select UI (procedural) -------------------------------------

    private GameObject BuildLevelSelect()
    {
        var root = new GameObject("LevelSelect");
        root.transform.SetParent(transform, false);

        MakeLabel(root.transform, new Vector3(ViewCenter.x, 0.15f, 0f),
            $"Coconut Isle   (stars {_records.TotalStars}/{LevelData.LevelCount * 3})",
            Color.white, scale: 0.08f);

        // 30 levels in a 5×6 grid inside the board's footprint.
        const int cols = 5;
        const float colStep = 1.75f, rowStep = 1.5f;
        float startX = ViewCenter.x - (cols - 1) * 0.5f * colStep;
        float startY = -1.1f;

        var all = LevelData.AllLevels;
        for (int i = 0; i < all.Count; i++)
        {
            var level = all[i];
            int col = i % cols;
            int row = i / cols;
            var pos = new Vector3(startX + col * colStep, startY - row * rowStep, 0f);

            int best = _records.GetBestStars(level.LevelNumber);
            string label = $"L{level.LevelNumber}\n{DifficultyText(level.Difficulty)}\n{best}/3";

            var captured = level; // avoid the classic loop-capture bug
            MakeButton(root.transform, pos, new Vector2(1.55f, 1.2f),
                DifficultyColor(level.Difficulty), label, Color.black,
                () => StartLevel(captured), labelScale: 0.085f, labelLineSpacing: 0.6f);
        }

        return root;
    }

    // ---- Results UI (procedural) ------------------------------------------

    // Centre of the play-framing view (board hidden here), used to lay out the
    // full-screen results art.
    private static readonly Vector3 ResultsCenter = new Vector3(4.4f, -3.7f, 0f);

    private GameObject BuildResults(LevelData level, bool won, LevelResult result)
    {
        var root = new GameObject("Results");
        root.transform.SetParent(transform, false);

        // --- Nature backdrop (flat colour bands: sky + island ground) ---
        MakePanel(root.transform, ResultsCenter, new Vector2(28f, 20f),
            won ? new Color(0.53f, 0.80f, 0.92f) : new Color(0.40f, 0.52f, 0.62f), sortingOrder: 20); // sky
        MakePanel(root.transform, ResultsCenter + new Vector3(0f, -4.6f, 0.02f), new Vector2(28f, 8f),
            won ? new Color(0.46f, 0.73f, 0.42f) : new Color(0.40f, 0.55f, 0.40f), sortingOrder: 20); // ground

        // --- Placeholder hero ("Mia") — a real character sprite + Animator
        // drops in here later; this stand-in conveys the win/loss mood + a bob. ---
        BuildHero(root.transform, ResultsCenter + new Vector3(0f, 3.2f, -0.1f), won);

        // --- Parchment card holding the readable text ---
        MakePanel(root.transform, ResultsCenter + new Vector3(0f, -1.2f, -0.2f), new Vector2(7.8f, 5.4f),
            new Color(0.97f, 0.94f, 0.84f), sortingOrder: 21);

        var ink = new Color(0.24f, 0.18f, 0.10f);
        MakeLabel(root.transform, ResultsCenter + new Vector3(0f, 0.9f, -0.3f),
            won ? $"Level {level.LevelNumber} — You Win!" : $"Level {level.LevelNumber} — Failed",
            won ? new Color(0.20f, 0.45f, 0.16f) : new Color(0.62f, 0.20f, 0.16f), scale: 0.13f, sortingOrder: 22);

        BuildStars(root.transform, ResultsCenter + new Vector3(0f, -0.2f, -0.3f), result.Stars);

        MakeLabel(root.transform, ResultsCenter + new Vector3(0f, -1.4f, -0.3f),
            $"+{result.CreditPayout} credits", new Color(0.78f, 0.55f, 0.12f), scale: 0.12f, sortingOrder: 22);
        MakeLabel(root.transform, ResultsCenter + new Vector3(0f, -2.3f, -0.3f),
            $"Score {_session!.Score}", ink, scale: 0.09f, sortingOrder: 22);

        // --- Buttons ---
        var buttonY = ResultsCenter.y - 4.5f;
        var green = new Color(0.28f, 0.55f, 0.30f);
        var muted = new Color(0.55f, 0.58f, 0.52f);

        MakeButton(root.transform, new Vector3(ResultsCenter.x - 2.3f, buttonY, -0.3f), new Vector2(1.9f, 0.95f),
            green, "Replay", Color.white, () => StartLevel(level));

        var next = NextLevel(level);
        MakeButton(root.transform, new Vector3(ResultsCenter.x, buttonY, -0.3f), new Vector2(1.9f, 0.95f),
            next != null ? green : muted, next != null ? "Next" : "Next (—)", Color.white,
            () => { if (next != null) StartLevel(next); else ShowLevelSelect(); });

        MakeButton(root.transform, new Vector3(ResultsCenter.x + 2.3f, buttonY, -0.3f), new Vector2(1.9f, 0.95f),
            green, "Menu", Color.white, ShowLevelSelect);

        return root;
    }

    /// <summary>Placeholder "Mia" hero: a name tag + a big text face whose
    /// expression reflects the result, with a looping bob. Swap this for a real
    /// character sprite + <c>Animator</c> when the art exists — the win/loss flag
    /// is the trigger the animation would key off.</summary>
    private void BuildHero(Transform parent, Vector3 pos, bool won)
    {
        var hero = new GameObject("Hero");
        hero.transform.SetParent(parent, false);
        hero.transform.localPosition = pos;

        MakePanel(hero.transform, new Vector3(0f, 0f, 0f), new Vector2(1.9f, 1.9f),
            new Color(0.99f, 0.86f, 0.71f), sortingOrder: 22);                 // face
        MakeLabel(hero.transform, new Vector3(0f, 0.05f, -0.1f), won ? "^_^" : "T_T",
            new Color(0.2f, 0.15f, 0.1f), scale: 0.16f, sortingOrder: 23);      // expression
        MakeLabel(hero.transform, new Vector3(0f, -1.45f, -0.1f), "Mia",
            Color.white, scale: 0.1f, sortingOrder: 23);                        // name tag

        StartCoroutine(BobHero(hero.transform, won));
    }

    private static IEnumerator BobHero(Transform hero, bool won)
    {
        float t = 0f;
        float amp = won ? 0.18f : 0.06f;   // a lively hop when winning, a gentle sway when not
        float speed = won ? 4.5f : 2.0f;
        var basePos = hero.localPosition;
        while (hero != null)
        {
            t += Time.deltaTime * speed;
            float offset = Mathf.Abs(Mathf.Sin(t)) * amp;
            hero.localPosition = basePos + new Vector3(0f, offset, 0f);
            yield return null;
        }
    }

    /// <summary>Three star glyphs, earned ones gold, the rest greyed.</summary>
    private void BuildStars(Transform parent, Vector3 center, int stars)
    {
        const float step = 0.85f;
        var gold = new Color(1.0f, 0.80f, 0.15f);
        var grey = new Color(0.72f, 0.70f, 0.62f);
        for (int i = 0; i < 3; i++)
        {
            bool earned = i < stars;
            MakeLabel(parent, center + new Vector3((i - 1) * step, 0f, 0f),
                earned ? "★" : "☆", earned ? gold : grey, scale: 0.22f, sortingOrder: 22);
        }
    }

    private void DestroyResults()
    {
        if (_resultsRoot != null)
        {
            Destroy(_resultsRoot);
            _resultsRoot = null;
        }
    }

    // ---- Procedural building blocks ---------------------------------------

    private static Sprite? _whiteSprite;
    private static Sprite WhiteSprite
    {
        get
        {
            if (_whiteSprite == null)
            {
                var tex = Texture2D.whiteTexture;
                _whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), tex.width);
            }
            return _whiteSprite!;
        }
    }

    private static Font? _uiFont;
    private static Font UiFont
    {
        get
        {
            if (_uiFont == null)
                _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _uiFont!;
        }
    }

    /// <summary>A flat coloured quad (a scaled 1×1 white sprite).</summary>
    private static SpriteRenderer MakePanel(Transform parent, Vector3 pos, Vector2 size, Color color, int sortingOrder)
    {
        var go = new GameObject("Panel");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = WhiteSprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        return sr;
    }

    /// <summary>A clickable button: an unscaled root carrying the collider +
    /// <see cref="ClickableView"/>, a scaled background quad, and a centred
    /// label. Background/label live on separate objects so the button's collider
    /// stays at the true world size regardless of the background's scale.</summary>
    private ClickableView MakeButton(Transform parent, Vector3 pos, Vector2 size, Color bg, string text, Color textColor, Action onClick,
        float labelScale = 0.11f, float labelLineSpacing = 1f)
    {
        var root = new GameObject("Button");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;

        var bgGo = new GameObject("BG");
        bgGo.transform.SetParent(root.transform, false);
        bgGo.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = bgGo.AddComponent<SpriteRenderer>();
        sr.sprite = WhiteSprite;
        sr.color = bg;
        sr.sortingOrder = 21;

        var col = root.AddComponent<BoxCollider2D>();
        col.size = size;

        var click = root.AddComponent<ClickableView>();
        click.Clicked = onClick;

        MakeLabel(root.transform, new Vector3(0f, 0f, -0.1f), text, textColor, scale: labelScale, sortingOrder: 22, lineSpacing: labelLineSpacing);
        return click;
    }

    private TextMesh MakeLabel(Transform parent, Vector3 localPos, string text, Color color, float scale, int sortingOrder = 1, float lineSpacing = 1f)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(scale, scale, scale);

        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.font = UiFont;
        tm.fontSize = 48;              // rendered big then scaled down for crisp glyphs
        tm.characterSize = 1f;
        tm.lineSpacing = lineSpacing;  // tightened for the stacked level-button labels
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;

        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = UiFont.material;
        mr.sortingOrder = sortingOrder;
        return tm;
    }

    private static string DifficultyText(Difficulty d) => d switch
    {
        Difficulty.Easy => "Easy",
        Difficulty.Hard => "Hard",
        Difficulty.VeryHard => "V.Hard",
        _ => d.ToString(),
    };

    private static Color DifficultyColor(Difficulty d) => d switch
    {
        Difficulty.Easy => new Color(0.35f, 0.62f, 0.40f),
        Difficulty.Hard => new Color(0.72f, 0.55f, 0.28f),
        Difficulty.VeryHard => new Color(0.70f, 0.34f, 0.34f),
        _ => new Color(0.4f, 0.4f, 0.4f),
    };
}

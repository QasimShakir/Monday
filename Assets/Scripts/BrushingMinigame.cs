using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Runs the 2D brushing minigame.
// Put this on an object that stays active in the scene (NOT on the canvas).
// The canvas is shown and hidden by Open() and Close().
//
// Gameplay:
//  - Scrub back and forth: quick direction changes build a combo that cleans faster.
//  - Don't brush too hard: moving too fast cleans nothing and hurts; too much and you wince.
//  - Tartar: some darker patches need extra scrubbing.
//  - Germs wander over the teeth and dirty them again until you brush them away.
//  - The game closes by itself at 100% (with no germs left).
public class BrushingMinigame : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Canvas canvas;                          // BrushingCanvas
    [SerializeField] private RectTransform plaqueGrid;               // PlaqueGrid (child of Teeth)
    [SerializeField] private RectTransform brush;                    // Brush image
    [SerializeField] private TextMeshProUGUI cleanlinessText;        // CleanlinessText
    [SerializeField] private Slider cleanlinessBar;                  // CleanlinessBar
    [Tooltip("Optional. Short messages: combo, too hard, germs...")]
    [SerializeField] private TextMeshProUGUI feedbackText;
    [Tooltip("Old 'Press Esc to finish' object. No longer used; hidden while open.")]
    [SerializeField] private GameObject hint;

    [Header("Player Scripts To Disable While Open")]
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private PlayerInteractor playerInteractor;

    [Header("Plaque Grid")]
    [SerializeField] private int columns = 8;
    [SerializeField] private int rows = 4;
    [SerializeField] private Color plaqueColor = new Color(0.8f, 0.65f, 0.15f, 1f);
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.85f;

    [Header("Brushing")]
    [SerializeField] private float brushRadius = 80f;       // canvas units
    [SerializeField] private float cleanPerUnit = 0.004f;   // plaque removed per canvas unit of mouse travel
    [SerializeField] private float finishDelay = 0.8f;      // pause on "Done." before closing

    [Header("Scrubbing Combo")]
    [Tooltip("A stroke must be at least this long (canvas units) before reversing counts.")]
    [SerializeField] private float minStroke = 25f;
    [Tooltip("Seconds without a reversal before the combo drops.")]
    [SerializeField] private float comboWindow = 0.5f;
    [SerializeField] private int maxCombo = 4;
    [Tooltip("Extra cleaning per combo level (0.5 = x3 at combo 4).")]
    [SerializeField] private float comboBonus = 0.5f;

    [Header("Too Hard")]
    [Tooltip("Brush speed (canvas units per second) above which it's too hard.")]
    [SerializeField] private float maxSafeSpeed = 3000f;
    [SerializeField] private float painPerSecond = 1.5f;
    [SerializeField] private float painRecovery = 0.6f;
    [SerializeField] private float winceTime = 1.2f;
    [SerializeField] private Color hardBrushColor = new Color(1f, 0.35f, 0.35f, 1f);

    [Header("Tartar")]
    [SerializeField, Range(0f, 1f)] private float tartarChance = 0.15f;
    [SerializeField] private float tartarToughness = 3f;
    [SerializeField] private Color tartarColor = new Color(0.45f, 0.3f, 0.1f, 1f);

    [Header("Germs")]
    [Tooltip("Optional. Without a sprite germs are small squares.")]
    [SerializeField] private Sprite germSprite;
    [SerializeField] private Color germColor = new Color(0.45f, 0.85f, 0.3f, 1f);
    [SerializeField] private int germCount = 3;
    [Tooltip("Cleanliness % at which more germs show up.")]
    [SerializeField] private float extraGermsAt = 60f;
    [SerializeField] private int extraGermCount = 2;
    [SerializeField] private float germSize = 40f;          // canvas units
    [SerializeField] private float germSpeed = 120f;        // canvas units per second
    [SerializeField] private float spreadInterval = 1.5f;   // seconds between re-dirtying a spot
    [SerializeField] private float spreadAmount = 0.3f;     // plaque added to the spot under the germ

    private class Germ
    {
        public RectTransform rect;
        public Vector2 velocity;     // canvas units per second
        public float spreadTimer;
        public float phase;
    }

    private float[] plaque;                       // 1 = fully covered, 0 = clean
    private float[] toughness;                    // 1 = normal, tartarToughness = tartar
    private Image[] cells;
    private readonly List<Germ> germs = new List<Germ>();
    private bool isOpen;
    private bool extraGermsSpawned;
    private float cleanliness;
    private float scale = 1f;                     // canvas scale factor
    private Vector2 lastMouse;

    private Image brushImage;
    private Color brushBaseColor;

    private int combo;
    private float comboTimer;
    private float strokeLength;
    private Vector2 strokeDir;

    private float pain;
    private float winceTimer;

    private float feedbackTimer;
    private float finishTimer = -1f;              // >= 0 while showing "Done."

    public float Cleanliness => cleanliness;
    public bool IsOpen => isOpen;

    // Call this from the sink's Interactable (On Interact).
    public void Open()
    {
        if (isOpen) return;

        // Inspector slots can get cleared when scripts change; find the player scripts if so
        if (playerController == null) playerController = FindFirstObjectByType<FirstPersonController>();
        if (playerInteractor == null) playerInteractor = FindFirstObjectByType<PlayerInteractor>();

        isOpen = true;

        canvas.gameObject.SetActive(true);
        scale = canvas.scaleFactor;

        if (playerController != null) playerController.enabled = false;
        if (playerInteractor != null) playerInteractor.enabled = false;
        SetCrosshairVisible(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (hint != null) hint.SetActive(false);

        brushImage = brush.GetComponent<Image>();
        if (brushImage != null) brushBaseColor = brushImage.color;

        combo = 0; comboTimer = 0f; strokeLength = 0f; strokeDir = Vector2.zero;
        pain = 0f; winceTimer = 0f; finishTimer = -1f;
        extraGermsSpawned = false;

        BuildGrid();
        SpawnGerms(germCount);
        UpdateHud();
        lastMouse = Input.mousePosition;

        ShowFeedback("Scrub back and forth.", 2f);
    }

    private void Close()
    {
        isOpen = false;
        OralHygiene.SetPlaque(100f - cleanliness); // remember progress for this session
        ClearGerms();
        if (brushImage != null) brushImage.color = brushBaseColor;
        canvas.gameObject.SetActive(false);

        if (playerController != null) playerController.enabled = true;
        if (playerInteractor != null) playerInteractor.enabled = true;
        SetCrosshairVisible(true);

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (!isOpen || cells == null) return;

        float dt = Time.deltaTime;
        Vector2 mouse = Input.mousePosition;
        brush.position = mouse;

        TickFeedback(dt);

        // Finished: hold on "Done." for a moment, then close by itself
        if (finishTimer >= 0f)
        {
            finishTimer -= dt;
            if (finishTimer < 0f) Close();
            return;
        }

        if (comboTimer > 0f)
        {
            comboTimer -= dt;
            if (comboTimer <= 0f) combo = 0;
        }

        bool tooHard = false;

        if (winceTimer > 0f)
        {
            winceTimer -= dt;
        }
        else if (Input.GetMouseButton(0))
        {
            Vector2 delta = (mouse - lastMouse) / scale;
            float travel = delta.magnitude;
            float speed = travel / Mathf.Max(dt, 0.0001f);

            if (speed > maxSafeSpeed)
            {
                tooHard = true;
                combo = 0;
                pain += painPerSecond * dt;
                ShowFeedback("Too hard! Ease up.", 0.6f);

                if (pain >= 1f)
                {
                    pain = 0f;
                    winceTimer = winceTime;
                    ShowFeedback("Ow.", winceTime);
                }
            }
            else
            {
                if (travel > 0f)
                {
                    TrackStroke(delta);
                    Scrub(mouse, travel * (1f + combo * comboBonus));
                }
                PopGerms(mouse);
            }
        }

        if (!tooHard) pain = Mathf.Max(0f, pain - painRecovery * dt);
        if (brushImage != null)
            brushImage.color = (tooHard || winceTimer > 0f) ? hardBrushColor : brushBaseColor;

        lastMouse = mouse;

        UpdateGerms(dt);

        if (!extraGermsSpawned && cleanliness >= extraGermsAt)
        {
            extraGermsSpawned = true;
            SpawnGerms(extraGermCount);
            ShowFeedback("They're multiplying...", 1.5f);
        }

        // Done: rounds to 100% and nothing left to re-dirty the teeth
        if (cleanliness >= 99.5f)
        {
            if (germs.Count == 0)
            {
                finishTimer = finishDelay;
                AnalyticsManager.CompleteTask(AnalyticsManager.BrushingTask);
                if (brushImage != null) brushImage.color = brushBaseColor;
                ShowFeedback("Done.", finishDelay + 1f);
            }
            else
            {
                ShowFeedback("Get the germs!", 0.6f);
            }
        }
    }

    // Builds the combo from back-and-forth strokes. Long, straight drags don't count.
    private void TrackStroke(Vector2 delta)
    {
        if (delta.magnitude < 3f) return; // ignore hand jitter

        Vector2 dir = delta.normalized;
        strokeLength += delta.magnitude;

        if (strokeDir != Vector2.zero && Vector2.Dot(dir, strokeDir) < -0.3f)
        {
            if (strokeLength >= minStroke)
            {
                combo = Mathf.Min(maxCombo, combo + 1);
                comboTimer = comboWindow;
                if (combo >= 2) ShowFeedback("Scrub x" + (1f + combo * comboBonus).ToString("0.#"), 0.6f);
            }
            strokeLength = 0f;
        }
        strokeDir = dir;
    }

    private void Scrub(Vector2 mouse, float travel)
    {
        float radius = brushRadius * scale;
        float radiusSqr = radius * radius;
        float amount = travel * cleanPerUnit;
        bool changed = false;

        for (int i = 0; i < cells.Length; i++)
        {
            if (plaque[i] <= 0f) continue;

            Vector2 cellCenter = cells[i].rectTransform.position; // screen pixels on an Overlay canvas
            if ((cellCenter - mouse).sqrMagnitude > radiusSqr) continue;

            plaque[i] = Mathf.Max(0f, plaque[i] - amount / toughness[i]);
            UpdateCellVisual(i);
            changed = true;
        }

        if (changed) UpdateHud();
    }

    // ---------- Germs ----------

    private void SpawnGerms(int count)
    {
        Rect area = GridScreenRect();
        Transform parent = plaqueGrid.parent;

        for (int n = 0; n < count; n++)
        {
            var go = new GameObject("Germ", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.transform.SetSiblingIndex(plaqueGrid.GetSiblingIndex() + 1); // above plaque
            go.GetComponent<LayoutElement>().ignoreLayout = true;

            var image = go.GetComponent<Image>();
            image.sprite = germSprite;
            image.color = germColor;
            image.raycastTarget = false;

            var rect = (RectTransform)go.transform;
            rect.sizeDelta = Vector2.one * germSize;
            rect.position = new Vector2(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax));

            germs.Add(new Germ
            {
                rect = rect,
                velocity = Random.insideUnitCircle.normalized * germSpeed,
                spreadTimer = Random.Range(0.5f, spreadInterval),
                phase = Random.Range(0f, 10f),
            });
        }
    }

    private void UpdateGerms(float dt)
    {
        if (germs.Count == 0) return;
        Rect area = GridScreenRect();

        foreach (Germ g in germs)
        {
            // Wander: slowly turn, bounce off the edges of the teeth
            g.velocity = Quaternion.Euler(0f, 0f, Random.Range(-90f, 90f) * dt) * g.velocity;
            Vector2 pos = (Vector2)g.rect.position + g.velocity * scale * dt;

            if (pos.x < area.xMin || pos.x > area.xMax) g.velocity.x = -g.velocity.x;
            if (pos.y < area.yMin || pos.y > area.yMax) g.velocity.y = -g.velocity.y;
            pos.x = Mathf.Clamp(pos.x, area.xMin, area.xMax);
            pos.y = Mathf.Clamp(pos.y, area.yMin, area.yMax);
            g.rect.position = pos;

            // Wobble so they look alive
            float wobble = 1f + Mathf.Sin((Time.time + g.phase) * 8f) * 0.12f;
            g.rect.localScale = new Vector3(wobble, 2f - wobble, 1f);

            g.spreadTimer -= dt;
            if (g.spreadTimer <= 0f)
            {
                g.spreadTimer = spreadInterval;
                Dirty(NearestCell(pos), spreadAmount);
            }
        }
    }

    private void PopGerms(Vector2 mouse)
    {
        float reach = brushRadius * scale * 0.6f;
        float reachSqr = reach * reach;

        for (int i = germs.Count - 1; i >= 0; i--)
        {
            if (((Vector2)germs[i].rect.position - mouse).sqrMagnitude > reachSqr) continue;

            Destroy(germs[i].rect.gameObject);
            germs.RemoveAt(i);
            ShowFeedback(germs.Count == 0 ? "Got them all." : "Got one.", 0.8f);
        }
    }

    private void ClearGerms()
    {
        foreach (Germ g in germs)
            if (g.rect != null) Destroy(g.rect.gameObject);
        germs.Clear();
    }

    private void Dirty(int cell, float amount)
    {
        if (cell < 0) return;
        plaque[cell] = Mathf.Min(1f, plaque[cell] + amount);
        UpdateCellVisual(cell);
        UpdateHud();
    }

    private int NearestCell(Vector2 screenPos)
    {
        int best = -1;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < cells.Length; i++)
        {
            float d = ((Vector2)cells[i].rectTransform.position - screenPos).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = i; }
        }
        return best;
    }

    private Rect GridScreenRect()
    {
        var corners = new Vector3[4];
        plaqueGrid.GetWorldCorners(corners); // screen pixels on an Overlay canvas
        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }

    // ---------- Grid ----------

    private void BuildGrid()
    {
        // Remove any test cells placed in the editor
        for (int i = plaqueGrid.childCount - 1; i >= 0; i--)
        {
            GameObject old = plaqueGrid.GetChild(i).gameObject;
            old.SetActive(false);
            Destroy(old);
        }

        var layout = plaqueGrid.GetComponent<GridLayoutGroup>();
        if (layout == null)
            layout = plaqueGrid.gameObject.AddComponent<GridLayoutGroup>();

        Canvas.ForceUpdateCanvases();
        layout.padding = new RectOffset();
        layout.spacing = Vector2.zero;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = columns;
        layout.cellSize = new Vector2(plaqueGrid.rect.width / columns, plaqueGrid.rect.height / rows);

        int count = columns * rows;
        plaque = new float[count];
        toughness = new float[count];
        cells = new Image[count];

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("PlaqueCell", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(plaqueGrid, false);

            cells[i] = go.GetComponent<Image>();
            cells[i].raycastTarget = false;

            plaque[i] = OralHygiene.Plaque / 100f; // every cell starts at the stored average
            toughness[i] = Random.value < tartarChance ? tartarToughness : 1f;
        }

        ScatterPlaque();

        for (int i = 0; i < count; i++) UpdateCellVisual(i);

        LayoutRebuilder.ForceRebuildLayoutImmediate(plaqueGrid);
    }

    // Moves plaque between random cells so the pattern looks uneven
    // while the overall average stays equal to the stored value.
    private void ScatterPlaque()
    {
        int count = plaque.Length;
        for (int n = 0; n < count * 2; n++)
        {
            int a = Random.Range(0, count);
            int b = Random.Range(0, count);
            float move = Mathf.Min(Random.Range(0f, 0.3f), plaque[a], 1f - plaque[b]);
            plaque[a] -= move;
            plaque[b] += move;
        }
    }

    private void UpdateCellVisual(int i)
    {
        Color c = toughness[i] > 1f ? tartarColor : plaqueColor;
        c.a = plaque[i] * maxAlpha;
        cells[i].color = c;
    }

    private void UpdateHud()
    {
        float total = 0f;
        for (int i = 0; i < plaque.Length; i++) total += plaque[i];
        cleanliness = (1f - total / plaque.Length) * 100f;

        if (cleanlinessText != null)
            cleanlinessText.text = "Cleanliness: " + Mathf.RoundToInt(cleanliness) + "%";

        if (cleanlinessBar != null)
        {
            cleanlinessBar.minValue = 0f;
            cleanlinessBar.maxValue = 100f;
            cleanlinessBar.value = cleanliness;
        }
    }

    // ---------- Feedback text ----------

    private void ShowFeedback(string message, float duration)
    {
        if (feedbackText == null) return;
        feedbackText.text = message;
        feedbackText.gameObject.SetActive(true);
        feedbackTimer = duration;
    }

    private void TickFeedback(float dt)
    {
        if (feedbackText == null || feedbackTimer <= 0f) return;
        feedbackTimer -= dt;
        if (feedbackTimer <= 0f) feedbackText.text = "";
    }

    private void SetCrosshairVisible(bool visible)
    {
        if (playerInteractor == null) return;

        if (playerInteractor.promptText != null)
            playerInteractor.promptText.gameObject.SetActive(false);

        if (playerInteractor.crosshair != null)
            playerInteractor.crosshair.gameObject.SetActive(visible);
    }
}

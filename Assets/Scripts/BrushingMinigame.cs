using UnityEngine;

// Runs the 2D brushing minigame.
// Put this on an object that stays active in the scene (NOT on the canvas).
// The canvas is shown and hidden by Open() and Close().
public class BrushingMinigame : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Canvas canvas;                          // BrushingCanvas
    [SerializeField] private RectTransform plaqueGrid;               // PlaqueGrid (child of Teeth)
    [SerializeField] private RectTransform brush;                    // Brush image
    [SerializeField] private TMPro.TextMeshProUGUI cleanlinessText;  // CleanlinessText
    [SerializeField] private UnityEngine.UI.Slider cleanlinessBar;   // CleanlinessBar
    [SerializeField] private GameObject hint;                        // Optional "Press Esc to finish" object

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
    [SerializeField] private float exitThreshold = 90f;     // cleanliness % needed before Esc works

    private float[] plaque;                       // 1 = fully covered, 0 = clean
    private UnityEngine.UI.Image[] cells;
    private bool isOpen;
    private float cleanliness;
    private float scale = 1f;                     // canvas scale factor
    private Vector2 lastMouse;

    public float Cleanliness => cleanliness;
    public bool IsOpen => isOpen;

    // Call this from the sink's Interactable (On Interact).
    public void Open()
    {
        if (isOpen) return;
        isOpen = true;

        canvas.gameObject.SetActive(true);
        scale = canvas.scaleFactor;

        playerController.enabled = false;
        playerInteractor.enabled = false;
        SetCrosshairVisible(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        BuildGrid();
        UpdateHud();
        lastMouse = Input.mousePosition;
    }

    private void Close()
    {
        isOpen = false;
        OralHygiene.SetPlaque(100f - cleanliness); // remember progress for this session
        canvas.gameObject.SetActive(false);

        playerController.enabled = true;
        playerInteractor.enabled = true;
        SetCrosshairVisible(true);

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (!isOpen) return;

        Vector2 mouse = Input.mousePosition;
        brush.position = mouse;

        // Only mouse movement cleans, so the player has to scrub
        if (Input.GetMouseButton(0))
        {
            float travel = (mouse - lastMouse).magnitude / scale;
            if (travel > 0f) Scrub(mouse, travel);
        }
        lastMouse = mouse;

        if (Input.GetKeyDown(KeyCode.Escape) && cleanliness >= exitThreshold)
            Close();
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

            plaque[i] = Mathf.Max(0f, plaque[i] - amount);
            UpdateCellVisual(i);
            changed = true;
        }

        if (changed) UpdateHud();
    }

    private void BuildGrid()
    {
        // Remove any test cells placed in the editor
        for (int i = plaqueGrid.childCount - 1; i >= 0; i--)
        {
            GameObject old = plaqueGrid.GetChild(i).gameObject;
            old.SetActive(false);
            Destroy(old);
        }

        var layout = plaqueGrid.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (layout == null)
            layout = plaqueGrid.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();

        Canvas.ForceUpdateCanvases();
        layout.padding = new RectOffset();
        layout.spacing = Vector2.zero;
        layout.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = columns;
        layout.cellSize = new Vector2(plaqueGrid.rect.width / columns, plaqueGrid.rect.height / rows);

        int count = columns * rows;
        plaque = new float[count];
        cells = new UnityEngine.UI.Image[count];

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("PlaqueCell", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(plaqueGrid, false);

            cells[i] = go.GetComponent<UnityEngine.UI.Image>();
            cells[i].raycastTarget = false;

            plaque[i] = OralHygiene.Plaque / 100f; // every cell starts at the stored average
        }

        ScatterPlaque();

        for (int i = 0; i < count; i++) UpdateCellVisual(i);

        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(plaqueGrid);
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
        Color c = plaqueColor;
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

        if (hint != null) hint.SetActive(cleanliness >= exitThreshold);
    }

    private void SetCrosshairVisible(bool visible)
    {
        if (playerInteractor.promptText != null)
            playerInteractor.promptText.gameObject.SetActive(false);

        if (playerInteractor.crosshair != null)
            playerInteractor.crosshair.gameObject.SetActive(visible);
    }
}

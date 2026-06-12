using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Settings for each difficulty stage
/// </summary>
[System.Serializable]
public struct StageSettings
{
    public string stageName;
    public int gridSize;
    public int pathLength;
    public int numberOfTurns;
    public float displayTime;
    public float segmentDelay;
    public int numberOfSnakes;
    public int numberOfDummySnakes;
    [Range(0f, 1f)]
    public float flipColorsProbability;
    public float delayBeforeRecall;
}

public class GridMarkers : MonoBehaviour
{
    // Grid settings (configurable)
    [Header("Grid Settings")]
    public int gridSize = 7;  // Square grid: gridSize x gridSize cells (0 to gridSize-1)
    public GameObject gridPointPrefab;  // Optional: custom prefab for grid points
    public Color gridPointColor = new Color(0.12f, 0.16f, 0.2f, 1f);  // Dark blue-gray (matches dark theme)
    
    // Grid bounds (computed from gridSize)
    private int MaxX => gridSize - 1;
    private int MaxY => gridSize - 1;

    // Current grid position (integers)
    private int gridX = 0;
    private int gridY = 0;

    // Grid layout
    private const float CELL_SIZE = 2f;
    private float originX = 0f;  // Will be calculated to center grid
    private float originY = 0f;  // Will be calculated to center grid
    
    // Dynamic grid visuals
    private List<GameObject> gridPointObjects = new List<GameObject>();
    
    // Color direction indicator objects
    private GameObject indicatorFrom;
    private GameObject indicatorArrow;
    private GameObject indicatorTo;

    // Path display settings
    [Header("Path Display")]
    public GameObject pathSegmentPrefab;
    public int pathLength = 7;
    public int numberOfTurns = 1;
    public float displayTime = 3f;
    public float segmentDelay = 0.3f;
    public bool flipColors = false;  // If true, Red=Head, Green=Tail (reversed)
    public float delayBeforeRecall = 0f;  // Seconds to wait after path disappears before player can trace

    // Path colors
    public Color tailColor = Color.red;
    public Color bodyColor = Color.blue;
    public Color headColor = Color.green;
    
    // Multiple snakes settings
    [Header("Multiple Snakes")]
    [Range(1, 3)]
    public int numberOfSnakes = 1;  // 1-3 real snakes to memorize and trace in order
    [Range(0, 2)]
    public int numberOfDummySnakes = 0;  // 0-2 dummy snakes (distractors, don't trace)
    public Color snake2BodyColor = Color.cyan;
    public Color snake3BodyColor = Color.magenta;
    public Color dummySnakeColor = new Color(0.6f, 0.6f, 0.6f, 1f);  // Gray for dummy snakes
    
    // Computed colors (swapped if flipColors is true)
    private Color ActualTailColor => flipColors ? headColor : tailColor;
    private Color ActualHeadColor => flipColors ? tailColor : headColor;

    // UI Elements
    [Header("UI")]
    public Text titleText;        // "PathFinder" title above grid
    public Text messageText;
    public Text scoreText;
    public Text sessionTimerText;
    public Text flipIndicatorText; // Shows color mode indicator

    // Score tracking
    private int pathsCount = 0;
    private int successCount = 0;
    private int failCount = 0;

    // Chances system
    [Header("Gameplay")]
    public int chancesPerPath = 1;  // Always 1 (no retries per path)
    private int remainingChances = 0;
    
    // Stage system
    [Header("Stage Progression")]
    public bool useStageProgression = true;  // Enable/disable stage system
    public int pathsPerStage = 3;  // Paths to complete before evaluating stage
    public int successesToAdvance = 2;  // Successes needed to advance (out of pathsPerStage)
    public int maxStageRetries = 2;  // Max retries before forced advance
    
    public StageSettings[] stages = new StageSettings[]
    {
        // Stage 0: Benchmark (always same for comparison)
        new StageSettings { stageName = "Benchmark", gridSize = 6, pathLength = 5, numberOfTurns = 2, 
            displayTime = 0.5f, segmentDelay = 0.05f, numberOfSnakes = 1, numberOfDummySnakes = 0, 
            flipColorsProbability = 0.2f, delayBeforeRecall = 0f },
        // Stage 1: Easy
        new StageSettings { stageName = "Easy", gridSize = 5, pathLength = 4, numberOfTurns = 1, 
            displayTime = 0.5f, segmentDelay = 0.05f, numberOfSnakes = 1, numberOfDummySnakes = 0, 
            flipColorsProbability = 0.2f, delayBeforeRecall = 0f },
        // Stage 2: Medium
        new StageSettings { stageName = "Medium", gridSize = 6, pathLength = 5, numberOfTurns = 2, 
            displayTime = 0.4f, segmentDelay = 0.05f, numberOfSnakes = 1, numberOfDummySnakes = 0, 
            flipColorsProbability = 0.3f, delayBeforeRecall = 1f },
        // Stage 3: Hard
        new StageSettings { stageName = "Hard", gridSize = 7, pathLength = 6, numberOfTurns = 3, 
            displayTime = 0.3f, segmentDelay = 0.04f, numberOfSnakes = 2, numberOfDummySnakes = 1, 
            flipColorsProbability = 0.4f, delayBeforeRecall = 2f },
        // Stage 4: Very Hard
        new StageSettings { stageName = "Very Hard", gridSize = 7, pathLength = 7, numberOfTurns = 4, 
            displayTime = 0.2f, segmentDelay = 0.03f, numberOfSnakes = 2, numberOfDummySnakes = 1, 
            flipColorsProbability = 0.5f, delayBeforeRecall = 3f },
        // Stage 5: Expert
        new StageSettings { stageName = "Expert", gridSize = 8, pathLength = 7, numberOfTurns = 4, 
            displayTime = 0.1f, segmentDelay = 0.02f, numberOfSnakes = 3, numberOfDummySnakes = 2, 
            flipColorsProbability = 0.6f, delayBeforeRecall = 3f }
    };
    
    // Stage tracking
    private int currentStage = 0;
    private int pathsInCurrentStage = 0;
    private int successesInCurrentStage = 0;
    private int currentStageAttempt = 1;  // How many times tried current stage
    private int maxStageReached = 0;
    private int benchmarkSuccesses = 0;
    private int benchmarkTotal = 0;
    
    // Stage UI
    [Header("Stage UI")]
    public Text stageText;  // Shows "Stage 3" 
    public Text stageDifficultyText;  // Shows "HARD"
    public Text stageIndicatorsText;  // Shows "⬡ ⬡ ⬡ ⬢ ⬢ ⬢"

    // Session timer
    private float sessionSecondsRemaining;
    private int sessionDurationTotalSeconds;
    private bool sessionEnded;
    private float sessionPlaySecondsAccumulated;
    private bool csvRowCommitted;

    // Game states
    private enum GameState
    {
        Idle,           // Initial state, press Space to start
        ShowingPath,    // Displaying target path
        WaitForTail,    // User moves to find tail, press Space to start drawing
        Drawing,        // User is drawing their path
        Success,        // User completed path correctly
        Fail            // User made a mistake
    }

    private GameState currentState = GameState.Idle;

    // Target path (generated) - for single snake (legacy, now uses allTargetPaths[0])
    private List<Vector2Int> targetPath = new List<Vector2Int>();
    private List<GameObject> targetPathSegments = new List<GameObject>();
    
    // Multiple snakes data
    private List<List<Vector2Int>> allTargetPaths = new List<List<Vector2Int>>();  // Real snakes only
    private List<List<Vector2Int>> dummyPaths = new List<List<Vector2Int>>();      // Dummy snakes
    private List<List<GameObject>> allTargetPathSegments = new List<List<GameObject>>();
    private List<List<GameObject>> dummyPathSegments = new List<List<GameObject>>();
    private int currentSnakeIndex = 0;  // Index into traceOrder (which snake to trace next)
    private List<int> savedDisplayOrder = new List<int>();  // Saved display order for retries
    private List<int> traceOrder = new List<int>();  // Order to trace real snakes (indices into allTargetPaths)
    private bool isRetry = false;  // Track if this is a retry (don't reshuffle)

    // User's drawn path
    private List<Vector2Int> userPath = new List<Vector2Int>();
    private List<GameObject> userPathSegments = new List<GameObject>();

    // Track visited cells during drawing
    private HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int>();
    
    // Track all cells used by all snakes (to prevent overlap during generation)
    private HashSet<Vector2Int> allUsedCells = new HashSet<Vector2Int>();

    // Reference to player's sprite renderer
    private SpriteRenderer playerSpriteRenderer;
    
    // Timing measurements for ML data
    private float sessionStartTime;  // When session started
    private float pathStartTime;     // When player can start tracing
    private float firstMoveTime;     // When player made first input
    private bool firstMoveMade;      // Track if first move recorded
    private int totalPathNumber;     // Total paths in this session (for CSV)

    // ML adaptive difficulty (optional, set on registration screen)
    private bool useMlAdaptive;
    private PathfinderMlPredictor mlPredictor;
    private readonly List<MlPathHistoryEntry> sessionPathHistory = new List<MlPathHistoryEntry>();
    private float lastPredictedPSuccess = -1f;
    const float MlHighThreshold = 0.75f;
    const float MlLowThreshold = 0.45f;

    struct MlPathHistoryEntry
    {
        public int Stage;
        public int Success;
        public float PathDurationMs;
    }

    void Start()
    {
        // Set camera background to dark space color (matches theme)
        if (Camera.main != null)
        {
            Camera.main.backgroundColor = new Color(0.05f, 0.05f, 0.12f, 1f);  // Deep space dark blue
        }
        
        // Initialize stage system
        if (useStageProgression && stages.Length > 0)
        {
            currentStage = 0;
            pathsInCurrentStage = 0;
            successesInCurrentStage = 0;
            currentStageAttempt = 1;
            maxStageReached = 0;
            totalPathNumber = 0;
            ApplyStageSettings(stages[0]);
        }
        
        CenterGridInView();
        GenerateGridVisuals();
        PositionUIRelativeToGrid();
        UpdateFlipIndicator();
        UpdateStageUI();
        
        gridX = 0;
        gridY = 0;
        UpdateWorldPosition();
        currentState = GameState.Idle;
        sessionEnded = false;
        sessionDurationTotalSeconds = PlayerPrefs.GetInt("GameDurationSeconds", 300);  // Default 5 minutes
        sessionSecondsRemaining = sessionDurationTotalSeconds;
        sessionPlaySecondsAccumulated = 0f;
        sessionStartTime = Time.time;
        csvRowCommitted = false;
        PlayerPrefs.SetInt("SessionNumber", CsvSessionExporter.GetNextSessionNumber(PlayerPrefs.GetString("PlayerName", "Guest")));

        useMlAdaptive = PlayerPrefs.GetInt("UseMlAdaptive", 0) == 1;
        if (useMlAdaptive)
        {
            mlPredictor = new PathfinderMlPredictor();
            if (!mlPredictor.TryLoad())
            {
                Debug.LogWarning("Pathfinder ML: adaptive mode disabled — model failed to load.");
                useMlAdaptive = false;
            }
        }

        ShowMessage(useMlAdaptive ? "Press SPACE to start (ML adaptive)" : "Press SPACE to start");
        UpdateScoreDisplay();
        UpdateSessionTimerDisplay();

        playerSpriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    /// <summary>
    /// Apply settings from a stage to the game parameters
    /// </summary>
    void ApplyStageSettings(StageSettings stage)
    {
        int oldGridSize = gridSize;
        
        gridSize = stage.gridSize;
        pathLength = stage.pathLength;
        numberOfTurns = stage.numberOfTurns;
        displayTime = stage.displayTime;
        segmentDelay = stage.segmentDelay;
        numberOfSnakes = stage.numberOfSnakes;
        numberOfDummySnakes = stage.numberOfDummySnakes;
        delayBeforeRecall = stage.delayBeforeRecall;
        
        // FlipColors is random per path, decided in GenerateNewPath based on flipColorsProbability
        
        // Regenerate grid if size changed
        if (oldGridSize != gridSize && gridPointObjects.Count > 0)
        {
            CenterGridInView();
            GenerateGridVisuals();
            PositionUIRelativeToGrid();
        }
    }
    
    /// <summary>
    /// Update the trophy-style stage progress UI
    /// </summary>
    void UpdateStageUI()
    {
        if (!useStageProgression) return;
        
        // Update stage number text
        if (stageText != null)
        {
            stageText.text = "Stage " + currentStage;
        }
        
        // Update difficulty name text
        if (stageDifficultyText != null && currentStage < stages.Length)
        {
            stageDifficultyText.text = stages[currentStage].stageName.ToUpper();
        }
        
        // Update indicators (filled hexagons for completed, empty for remaining)
        if (stageIndicatorsText != null)
        {
            string indicators = "";
            for (int i = 0; i < stages.Length; i++)
            {
                if (i < currentStage)
                    indicators += "⬢ ";  // Filled - completed
                else if (i == currentStage)
                    indicators += "★ ";  // Star - current
                else
                    indicators += "⬡ ";  // Empty - not reached
            }
            stageIndicatorsText.text = indicators.Trim();
        }
    }
    
    /// <summary>
    /// Handle stage progression after a path is completed
    /// </summary>
    void HandleStageProgression(bool success)
    {
        if (!useStageProgression) return;
        
        pathsInCurrentStage++;
        if (success) successesInCurrentStage++;
        
        // Track benchmark stats
        if (currentStage == 0)
        {
            benchmarkTotal++;
            if (success) benchmarkSuccesses++;
        }

        if (currentStage > maxStageReached)
            maxStageReached = currentStage;

        if (useMlAdaptive)
            return;
        
        // Check if we've completed enough paths in this stage
        if (pathsInCurrentStage >= pathsPerStage)
        {
            if (successesInCurrentStage >= successesToAdvance)
            {
                // Success! Advance to next stage
                AdvanceToNextStage();
            }
            else
            {
                // Not enough successes
                currentStageAttempt++;
                if (currentStageAttempt > maxStageRetries)
                {
                    // Stuck too long, force advance
                    AdvanceToNextStage();
                }
                else
                {
                    // Retry same stage
                    pathsInCurrentStage = 0;
                    successesInCurrentStage = 0;
                    ShowMessage("Retry Stage " + currentStage + "!");
                }
            }
            UpdateStageUI();
        }
    }
    
    /// <summary>
    /// Advance to the next difficulty stage
    /// </summary>
    void AdvanceToNextStage()
    {
        currentStage++;
        if (currentStage > maxStageReached)
            maxStageReached = currentStage;
            
        pathsInCurrentStage = 0;
        successesInCurrentStage = 0;
        currentStageAttempt = 1;
        
        if (currentStage < stages.Length)
        {
            ApplyStageSettings(stages[currentStage]);
            ShowMessage("Stage " + currentStage + ": " + stages[currentStage].stageName + "!");
        }
        else
        {
            ShowMessage("All stages complete! Amazing!");
        }
    }
    
    void UpdateFlipIndicator()
    {
        // Update text indicator if assigned
        if (flipIndicatorText != null)
        {
            if (flipColors)
            {
                flipIndicatorText.text = "⚠ FLIPPED";
                flipIndicatorText.color = Color.yellow;
            }
            else
            {
                flipIndicatorText.text = "";  // Hide text when normal
            }
        }
        
        // Create visual color indicator icons
        CreateColorDirectionIndicator();
    }
    
    void CreateColorDirectionIndicator()
    {
        // Destroy existing indicators
        if (indicatorFrom != null) Destroy(indicatorFrom);
        if (indicatorArrow != null) Destroy(indicatorArrow);
        if (indicatorTo != null) Destroy(indicatorTo);
        
        // Calculate position: left of grid (vertically centered), horizontal layout
        float gridWorldWidth = (gridSize - 1) * CELL_SIZE;
        float gridWorldHeight = (gridSize - 1) * CELL_SIZE;
        float gridTopY = gridWorldHeight / 2f;
        
        float indicatorStartX = -CELL_SIZE * 0.5f;  // Centered horizontally (moved right)
        float indicatorY = gridTopY + CELL_SIZE * 1.2f;  // Above the grid (moved down)
        float spacing = CELL_SIZE * 0.8f;
        
        // Create "From" color square (tail color) - left
        if (pathSegmentPrefab != null)
        {
            indicatorFrom = Instantiate(pathSegmentPrefab, 
                new Vector3(indicatorStartX, indicatorY, -0.2f), Quaternion.identity);
            indicatorFrom.transform.localScale = Vector3.one * 5f;  // Adjusted for 4x prefab scale
            SpriteRenderer sr = indicatorFrom.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = ActualTailColor;
        }
        
        // Create arrow (black bar pointing right) - middle
        if (pathSegmentPrefab != null)
        {
            indicatorArrow = Instantiate(pathSegmentPrefab,
                new Vector3(indicatorStartX + spacing, indicatorY, -0.2f), Quaternion.identity);
            indicatorArrow.transform.localScale = new Vector3(10f, 1.2f, 1f);  // Arrow shape (adjusted for 4x prefab scale)
            SpriteRenderer sr = indicatorArrow.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = Color.black;
        }
        
        // Create "To" color square (head color) - right
        if (pathSegmentPrefab != null)
        {
            indicatorTo = Instantiate(pathSegmentPrefab,
                new Vector3(indicatorStartX + spacing * 2, indicatorY, -0.2f), Quaternion.identity);
            indicatorTo.transform.localScale = Vector3.one * 5f;  // Adjusted for 4x prefab scale
            SpriteRenderer sr = indicatorTo.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = ActualHeadColor;
        }
    }
    
    void CenterGridInView()
    {
        // Calculate grid dimensions
        float gridWorldWidth = (gridSize - 1) * CELL_SIZE;
        float gridWorldHeight = (gridSize - 1) * CELL_SIZE;
        
        // Center the grid at world origin (0,0)
        originX = -gridWorldWidth / 2f;
        originY = -gridWorldHeight / 2f;
        
        // Adjust camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            // Center camera at origin
            mainCam.transform.position = new Vector3(0f, 0f, mainCam.transform.position.z);
            
            // Adjust orthographic size to fit grid + UI margins
            if (mainCam.orthographic)
            {
                float verticalSize = (gridWorldHeight / 2f) + CELL_SIZE * 4f;  // Space for title and message
                float horizontalSize = ((gridWorldWidth / 2f) + CELL_SIZE * 5f) / mainCam.aspect;  // Space for score
                mainCam.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
            }
        }
    }
    
    void PositionUIRelativeToGrid()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;
        
        // Calculate grid boundaries in world space (grid is now centered at 0,0)
        float gridWorldWidth = (gridSize - 1) * CELL_SIZE;
        float gridWorldHeight = (gridSize - 1) * CELL_SIZE;
        float gridTopY = gridWorldHeight / 2f;
        float gridBottomY = -gridWorldHeight / 2f;
        float gridRightX = gridWorldWidth / 2f;
        
        // Convert world positions to screen positions
        Vector3 gridTopCenter = mainCam.WorldToScreenPoint(new Vector3(0f, gridTopY + CELL_SIZE * 5f, 0f));
        Vector3 gridBottomCenter = mainCam.WorldToScreenPoint(new Vector3(0f, gridBottomY - CELL_SIZE * 2.5f, 0f));
        Vector3 gridTopRight = mainCam.WorldToScreenPoint(new Vector3(gridRightX + CELL_SIZE * 4f, gridTopY, 0f));
        Vector3 gridMidRight = mainCam.WorldToScreenPoint(new Vector3(gridRightX + CELL_SIZE * 4f, 0f, 0f));
        
        // Position title above grid center
        if (titleText != null)
        {
            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            if (titleRect != null)
            {
                titleRect.position = gridTopCenter;
            }
        }
        
        // Position message text below grid (centered)
        if (messageText != null)
        {
            RectTransform msgRect = messageText.GetComponent<RectTransform>();
            if (msgRect != null)
            {
                msgRect.position = gridBottomCenter;
            }
        }
        
        // Position timer to the right of the grid (top right)
        if (sessionTimerText != null)
        {
            RectTransform timerRect = sessionTimerText.GetComponent<RectTransform>();
            if (timerRect != null)
            {
                timerRect.position = gridTopRight;
            }
        }
        
        // Position score to the right of the grid (middle right)
        if (scoreText != null)
        {
            RectTransform scoreRect = scoreText.GetComponent<RectTransform>();
            if (scoreRect != null)
            {
                scoreRect.position = gridMidRight;
            }
        }
        
        // Position flip indicator below title, above grid
        if (flipIndicatorText != null)
        {
            RectTransform flipRect = flipIndicatorText.GetComponent<RectTransform>();
            if (flipRect != null)
            {
                Vector3 flipIndicatorPos = mainCam.WorldToScreenPoint(new Vector3(0f, gridTopY + CELL_SIZE * 3.5f, 0f));
                flipRect.position = flipIndicatorPos;
            }
        }
    }
    
    void GenerateGridVisuals()
    {
        // Clear existing grid points
        foreach (GameObject obj in gridPointObjects)
        {
            if (obj != null) Destroy(obj);
        }
        gridPointObjects.Clear();
        
        // Generate new grid points based on gridSize
        // Use the same prefab as path segments to get same size
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                float worldX = originX + (x * CELL_SIZE);
                float worldY = originY + (y * CELL_SIZE);
                
                GameObject gridPoint;
                if (pathSegmentPrefab != null)
                {
                    // Use same prefab as path segments (same size as red/green squares)
                    gridPoint = Instantiate(pathSegmentPrefab, new Vector3(worldX, worldY, 0.1f), Quaternion.identity);
                }
                else if (gridPointPrefab != null)
                {
                    gridPoint = Instantiate(gridPointPrefab, new Vector3(worldX, worldY, 0.1f), Quaternion.identity);
                }
                else
                {
                    // Create a simple square if no prefab assigned
                    gridPoint = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    gridPoint.transform.position = new Vector3(worldX, worldY, 0.1f);
                    gridPoint.transform.localScale = new Vector3(1f, 1f, 1f);
                    
                    // Remove collider (not needed for visual)
                    Collider col = gridPoint.GetComponent<Collider>();
                    if (col != null) Destroy(col);
                }
                
                // Set gray color
                SpriteRenderer sr = gridPoint.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = gridPointColor;
                }
                else
                {
                    Renderer rend = gridPoint.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        rend.material.color = gridPointColor;
                    }
                }
                
                gridPointObjects.Add(gridPoint);
            }
        }
    }

    void OnApplicationQuit()
    {
        if (csvRowCommitted)
            return;
        FinalizeSessionCsv("ApplicationQuit");
    }

    void Update()
    {
        if (!sessionEnded && sessionSecondsRemaining > 0f)
            sessionPlaySecondsAccumulated += Time.deltaTime;

        if (!sessionEnded)
        {
            sessionSecondsRemaining -= Time.deltaTime;
            if (sessionSecondsRemaining <= 0f)
            {
                sessionSecondsRemaining = 0f;
                OnSessionTimeExpired();
            }
        }

        UpdateSessionTimerDisplay();

        if (sessionEnded)
            return;

        switch (currentState)
        {
            case GameState.Idle:
                HandleIdleState();
                break;
            case GameState.WaitForTail:
                HandleWaitForTailState();
                break;
            case GameState.Drawing:
                HandleDrawingState();
                break;
            case GameState.Success:
            case GameState.Fail:
                break;
        }
    }

    void UpdateSessionTimerDisplay()
    {
        if (sessionTimerText == null)
            return;

        if (sessionEnded && sessionSecondsRemaining <= 0f)
        {
            sessionTimerText.text = "0:00";
            return;
        }

        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(sessionSecondsRemaining));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        sessionTimerText.text = minutes + ":" + seconds.ToString("00");
    }

    void OnSessionTimeExpired()
    {
        if (sessionEnded)
            return;

        sessionEnded = true;
        StopAllCoroutines();
        ClearTargetPathSegments();
        ClearUserPath();
        SetPlayerVisible(true);
        currentState = GameState.Idle;
        ShowMessage("Time's up!");
        UpdateSessionTimerDisplay();
        FinalizeSessionCsv("TimeUp");
    }

    void FinalizeSessionCsv(string endReason)
    {
        if (csvRowCommitted)
            return;
        WriteSessionEndStats(endReason);
        CsvSessionExporter.AppendRow(BuildCsvPayload(endReason));
        csvRowCommitted = true;
        PathfinderRegistrationSnapshot.Clear();
    }

    SessionCsvPayload BuildCsvPayload(string endReason)
    {
        string sessionId = PlayerPrefs.GetString("SessionId", "");
        bool useSnapshot = PathfinderRegistrationSnapshot.HasData &&
            !string.IsNullOrEmpty(sessionId) &&
            sessionId == PathfinderRegistrationSnapshot.CapturedSessionId;

        string playerName = useSnapshot
            ? PathfinderRegistrationSnapshot.PlayerName
            : PlayerPrefs.GetString("PlayerName", "Guest");
        int playerAge = useSnapshot
            ? PathfinderRegistrationSnapshot.PlayerAge
            : PlayerPrefs.GetInt("PlayerAge", 0);
        string playerGender = useSnapshot
            ? PathfinderRegistrationSnapshot.PlayerGender
            : PlayerPrefs.GetString("PlayerGender", "");
        int isRegistered = useSnapshot
            ? PathfinderRegistrationSnapshot.IsRegistered
            : PlayerPrefs.GetInt("IsRegistered", 0);
        int gameDurationSeconds = useSnapshot
            ? PathfinderRegistrationSnapshot.GameDurationSeconds
            : PlayerPrefs.GetInt("GameDurationSeconds", 300);
        int mlAdaptive = useSnapshot
            ? PathfinderRegistrationSnapshot.UseMlAdaptive
            : PlayerPrefs.GetInt("UseMlAdaptive", 0);

        float difficultyScore = CalculateDifficultyScore(pathLength, numberOfTurns, displayTime, segmentDelay);
        float successRate = pathsCount > 0 ? (float)successCount / pathsCount : 0f;
        float performanceGrade = CalculatePerformanceGrade(successRate, difficultyScore);

        // Calculate benchmark score
        float benchmarkScore = benchmarkTotal > 0 ? (float)benchmarkSuccesses / benchmarkTotal : 0f;
        
        return new SessionCsvPayload
        {
            EndReason = endReason,
            PlayerName = playerName,
            PlayerAge = playerAge,
            PlayerGender = playerGender,
            IsRegistered = isRegistered,
            GameDurationSeconds = gameDurationSeconds,
            GridSize = gridSize,
            PathLength = pathLength,
            NumberOfTurns = numberOfTurns,
            DisplayTime = displayTime,
            SegmentDelay = segmentDelay,
            FlipColors = flipColors ? 1 : 0,
            DelayBeforeRecall = delayBeforeRecall,
            NumberOfSnakes = numberOfSnakes,
            NumberOfDummySnakes = numberOfDummySnakes,
            ChancesPerPath = chancesPerPath,
            PathsTotal = pathsCount,
            Success = successCount,
            Fail = failCount,
            SecondsPlayed = Mathf.RoundToInt(sessionPlaySecondsAccumulated),
            DifficultyScore = difficultyScore,
            SuccessRate = successRate,
            PerformanceGrade = performanceGrade,
            MaxStageReached = maxStageReached,
            BenchmarkScore = benchmarkScore,
            MlAdaptive = mlAdaptive
        };
    }

    void ApplyMlAdaptiveStageBeforePath()
    {
        if (!useMlAdaptive || mlPredictor == null || !mlPredictor.IsLoaded || !useStageProgression)
            return;

        float[] features = BuildMlFeatureVector();
        float p = mlPredictor.Predict(features);
        lastPredictedPSuccess = p;

        int prevStage = currentStage;
        int newStage = currentStage;
        if (p > MlHighThreshold)
            newStage = Mathf.Min(currentStage + 1, stages.Length - 1);
        else if (p < MlLowThreshold)
            newStage = Mathf.Max(currentStage - 1, 0);

        if (newStage != prevStage)
        {
            currentStage = newStage;
            currentStageAttempt = 1;
            pathsInCurrentStage = 0;
            successesInCurrentStage = 0;
            if (currentStage > maxStageReached)
                maxStageReached = currentStage;
            ApplyStageSettings(stages[currentStage]);
            CenterGridInView();
            GenerateGridVisuals();
            PositionUIRelativeToGrid();
            UpdateStageUI();
        }
    }

    float[] BuildMlFeatureVector()
    {
        StageSettings stage = (currentStage < stages.Length) ? stages[currentStage] : stages[0];

        int pathsDone = sessionPathHistory.Count;
        float successRate = 0f;
        float benchmarkRate = 0f;
        float stageRate = 0f;
        float avgDuration = 0f;
        float lastSuccess = 0f;

        if (pathsDone > 0)
        {
            int successes = 0;
            int benchSuccesses = 0;
            int benchTotal = 0;
            int stageSuccesses = 0;
            int stageTotal = 0;
            float durationSum = 0f;
            int durationCount = 0;

            foreach (MlPathHistoryEntry entry in sessionPathHistory)
            {
                successes += entry.Success;
                if (entry.Stage == 0)
                {
                    benchTotal++;
                    benchSuccesses += entry.Success;
                }
                if (entry.Stage == currentStage)
                {
                    stageTotal++;
                    stageSuccesses += entry.Success;
                }
                if (entry.PathDurationMs > 0f)
                {
                    durationSum += entry.PathDurationMs;
                    durationCount++;
                }
            }

            successRate = (float)successes / pathsDone;
            benchmarkRate = benchTotal > 0 ? (float)benchSuccesses / benchTotal : 0f;
            stageRate = stageTotal > 0 ? (float)stageSuccesses / stageTotal : 0f;
            avgDuration = durationCount > 0 ? durationSum / durationCount : 0f;
            lastSuccess = sessionPathHistory[pathsDone - 1].Success;
        }

        return new float[]
        {
            currentStage,
            currentStageAttempt,
            stage.gridSize,
            stage.pathLength,
            stage.numberOfTurns,
            stage.numberOfSnakes,
            stage.numberOfDummySnakes,
            0f,
            stage.displayTime,
            stage.segmentDelay,
            stage.delayBeforeRecall,
            pathsDone,
            successRate,
            benchmarkRate,
            stageRate,
            avgDuration,
            lastSuccess
        };
    }

    void RecordCompletedPathForMl(bool success, float pathDurationMs)
    {
        if (!useMlAdaptive)
            return;

        sessionPathHistory.Add(new MlPathHistoryEntry
        {
            Stage = currentStage,
            Success = success ? 1 : 0,
            PathDurationMs = pathDurationMs
        });
        currentStageAttempt++;
    }

    /// <summary>
    /// Calculates difficulty score based on game parameters.
    /// Formula: (PathLength * 10) + (Turns * 15) - (DisplayTime * 5) - (SegmentDelay * 10)
    /// Higher score = harder task.
    /// </summary>
    static float CalculateDifficultyScore(int pathLen, int turns, float dispTime, float segDelay)
    {
        return (pathLen * 10f) + (turns * 15f) - (dispTime * 5f) - (segDelay * 10f);
    }

    /// <summary>
    /// Calculates performance grade normalized against default difficulty (67).
    /// Formula: SuccessRate * 100 * (DifficultyScore / 67)
    /// </summary>
    static float CalculatePerformanceGrade(float successRate, float difficultyScore)
    {
        const float BaseDifficulty = 67f;
        return successRate * 100f * (difficultyScore / BaseDifficulty);
    }

    void WriteSessionEndStats(string endReason)
    {
        int played = Mathf.RoundToInt(sessionPlaySecondsAccumulated);
        PlayerPrefs.SetInt("Stat_PathsTotal", pathsCount);
        PlayerPrefs.SetInt("Stat_Success", successCount);
        PlayerPrefs.SetInt("Stat_Fail", failCount);
        PlayerPrefs.SetString("Stat_EndReason", endReason);
        PlayerPrefs.SetString("Stat_SecondsPlayed", played.ToString());
        PlayerPrefs.Save();
    }

    bool SessionBlocksInput()
    {
        return sessionEnded;
    }

    void HandleIdleState()
    {
        if (SessionBlocksInput())
            return;

        HandleMovement();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            GenerateNewPath();
        }
    }

    void HandleWaitForTailState()
    {
        if (SessionBlocksInput())
            return;

        HandleMovement();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartDrawing();
        }
    }

    void HandleDrawingState()
    {
        if (SessionBlocksInput())
            return;

        // Space to finish and check the path (at any time)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CheckResult();
            return;
        }

        // Handle movement and drawing
        Vector2Int oldPos = new Vector2Int(gridX, gridY);
        bool moved = HandleMovementAndReturnMoved();

        if (moved)
        {
            Vector2Int newPos = new Vector2Int(gridX, gridY);

            // Check if revisiting a cell
            if (visitedCells.Contains(newPos))
            {
                remainingChances--;
                Debug.Log("Crossed path! Remaining chances: " + remainingChances);

                if (remainingChances > 0)
                {
                    ShowMessage("Oops! " + remainingChances + " chance(s) left");
                    ClearUserPath();
                    currentState = GameState.Fail;
                    StartCoroutine(RetryWithSamePath());
                }
                else
                {
                    pathsCount++;
                    failCount++;
                    UpdateScoreDisplay();
                    currentState = GameState.Fail;
                    StartCoroutine(HandleFinalFailure());
                }
                return;
            }

            // Add new position to user path
            AddUserPathSegment(newPos);

            // Check if path is complete (reached target length)
            if (userPath.Count == targetPath.Count)
            {
                // Change last segment to head color
                if (userPathSegments.Count > 0)
                {
                    SpriteRenderer sr = userPathSegments[userPathSegments.Count - 1].GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = ActualHeadColor;
                }
                ShowMessage("Done! Press SPACE to check");
            }
        }
    }

    void HandleMovement()
    {
        HandleMovementAndReturnMoved();
    }

    bool HandleMovementAndReturnMoved()
    {
        bool moved = false;

        if (Input.GetKeyDown(KeyCode.RightArrow) && gridX < MaxX)
        {
            gridX++;
            moved = true;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) && gridX > 0)
        {
            gridX--;
            moved = true;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow) && gridY < MaxY)
        {
            gridY++;
            moved = true;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) && gridY > 0)
        {
            gridY--;
            moved = true;
        }

        if (moved)
        {
            // Record first move time for ML data
            if (!firstMoveMade && (currentState == GameState.WaitForTail || currentState == GameState.Drawing))
            {
                firstMoveTime = Time.time;
                firstMoveMade = true;
            }
            UpdateWorldPosition();
        }

        return moved;
    }

    void UpdateWorldPosition()
    {
        float worldX = originX + (gridX * CELL_SIZE);
        float worldY = originY + (gridY * CELL_SIZE);
        transform.position = new Vector3(worldX, worldY, -0.05f);
    }

    void GenerateNewPath()
    {
        if (SessionBlocksInput())
            return;

        ApplyMlAdaptiveStageBeforePath();

        // Increment total path counter
        totalPathNumber++;
        
        // Randomly decide flipColors based on current stage probability
        if (useStageProgression && currentStage < stages.Length)
        {
            flipColors = Random.value < stages[currentStage].flipColorsProbability;
            UpdateFlipIndicator();
        }

        // Clear all previous paths
        allTargetPaths.Clear();
        dummyPaths.Clear();
        allUsedCells.Clear();
        currentSnakeIndex = 0;
        isRetry = false;  // New path, will shuffle display order
        savedDisplayOrder.Clear();

        // Reset chances for new path set
        remainingChances = chancesPerPath;

        // Generate all real snakes
        bool allValid = true;
        for (int snakeIdx = 0; snakeIdx < numberOfSnakes; snakeIdx++)
        {
            List<Vector2Int> snakePath = new List<Vector2Int>();
            bool validPath = false;
            
            for (int attempt = 0; attempt < 100 && !validPath; attempt++)
            {
                validPath = TryGenerateSnakePath(snakePath);
            }
            
            if (validPath)
            {
                allTargetPaths.Add(snakePath);
                // Add all cells AND their neighbors to used set (1-cell buffer between snakes)
                foreach (var cell in snakePath)
                {
                    allUsedCells.Add(cell);
                    // Add all 8 neighboring cells as buffer
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            allUsedCells.Add(new Vector2Int(cell.x + dx, cell.y + dy));
                        }
                    }
                }
            }
            else
            {
                allValid = false;
                Debug.LogWarning("Could not generate valid path for snake " + (snakeIdx + 1));
                break;
            }
        }
        
        // Generate dummy snakes (distractors)
        if (allValid)
        {
            for (int dummyIdx = 0; dummyIdx < numberOfDummySnakes; dummyIdx++)
            {
                List<Vector2Int> dummyPath = new List<Vector2Int>();
                bool validPath = false;
                
                for (int attempt = 0; attempt < 100 && !validPath; attempt++)
                {
                    validPath = TryGenerateSnakePath(dummyPath);
                }
                
                if (validPath)
                {
                    dummyPaths.Add(dummyPath);
                    // Add all cells AND their neighbors to used set (1-cell buffer between snakes)
                    foreach (var cell in dummyPath)
                    {
                        allUsedCells.Add(cell);
                        // Add all 8 neighboring cells as buffer
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                allUsedCells.Add(new Vector2Int(cell.x + dx, cell.y + dy));
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("Could not generate dummy snake " + (dummyIdx + 1) + ", continuing without it");
                }
            }
        }

        // Set targetPath to first snake for compatibility
        if (allTargetPaths.Count > 0)
        {
            targetPath = allTargetPaths[0];
        }

        if (allValid && allTargetPaths.Count == numberOfSnakes)
        {
            StartCoroutine(DisplayAllSnakesCoroutine());
        }
        else
        {
            Debug.LogWarning("Could not generate all snakes");
        }
    }
    
    bool TryGenerateSnakePath(List<Vector2Int> path)
    {
        path.Clear();
        HashSet<Vector2Int> usedCells = new HashSet<Vector2Int>(allUsedCells);  // Include already used cells

        // Random start position (not already used)
        int startX, startY;
        int maxStartAttempts = 50;
        Vector2Int startPos;
        do
        {
            startX = Random.Range(0, gridSize);
            startY = Random.Range(0, gridSize);
            startPos = new Vector2Int(startX, startY);
            maxStartAttempts--;
        } while (usedCells.Contains(startPos) && maxStartAttempts > 0);
        
        if (usedCells.Contains(startPos))
            return false;
            
        path.Add(startPos);
        usedCells.Add(startPos);

        // Directions: 0=right, 1=up, 2=left, 3=down
        int[] dx = { 1, 0, -1, 0 };
        int[] dy = { 0, 1, 0, -1 };

        // Pick random initial direction
        int currentDir = Random.Range(0, 4);

        // Calculate segment lengths between turns
        List<int> segmentLengths = DivideLength(pathLength - 1, numberOfTurns + 1);

        int currentX = startX;
        int currentY = startY;

        for (int seg = 0; seg < segmentLengths.Count; seg++)
        {
            int segLength = segmentLengths[seg];

            // Move in current direction for segLength cells
            for (int i = 0; i < segLength; i++)
            {
                currentX += dx[currentDir];
                currentY += dy[currentDir];

                // Check bounds
                if (currentX < 0 || currentX > MaxX || currentY < 0 || currentY > MaxY)
                {
                    return false; // Invalid path - out of bounds
                }

                Vector2Int newPos = new Vector2Int(currentX, currentY);

                // Check if cell already used (path crosses itself or another snake)
                if (usedCells.Contains(newPos))
                {
                    return false; // Invalid path - crosses itself or another snake
                }

                path.Add(newPos);
                usedCells.Add(newPos);
            }

            // Turn 90 degrees (if not last segment)
            if (seg < segmentLengths.Count - 1)
            {
                // Turn left or right randomly
                if (Random.Range(0, 2) == 0)
                    currentDir = (currentDir + 1) % 4; // Turn left
                else
                    currentDir = (currentDir + 3) % 4; // Turn right
            }
        }

        return true;
    }

    bool TryGeneratePath()
    {
        targetPath.Clear();
        HashSet<Vector2Int> usedCells = new HashSet<Vector2Int>();

        // Random start position
        int startX = Random.Range(0, gridSize);
        int startY = Random.Range(0, gridSize);
        Vector2Int startPos = new Vector2Int(startX, startY);
        targetPath.Add(startPos);
        usedCells.Add(startPos);

        // Directions: 0=right, 1=up, 2=left, 3=down
        int[] dx = { 1, 0, -1, 0 };
        int[] dy = { 0, 1, 0, -1 };

        // Pick random initial direction
        int currentDir = Random.Range(0, 4);

        // Calculate segment lengths between turns
        List<int> segmentLengths = DivideLength(pathLength - 1, numberOfTurns + 1);

        int currentX = startX;
        int currentY = startY;

        for (int seg = 0; seg < segmentLengths.Count; seg++)
        {
            int segLength = segmentLengths[seg];

            // Move in current direction for segLength cells
            for (int i = 0; i < segLength; i++)
            {
                currentX += dx[currentDir];
                currentY += dy[currentDir];

                // Check bounds
                if (currentX < 0 || currentX > MaxX || currentY < 0 || currentY > MaxY)
                {
                    return false; // Invalid path - out of bounds
                }

                Vector2Int newPos = new Vector2Int(currentX, currentY);

                // Check if cell already used (path crosses itself)
                if (usedCells.Contains(newPos))
                {
                    return false; // Invalid path - crosses itself
                }

                targetPath.Add(newPos);
                usedCells.Add(newPos);
            }

            // Turn 90 degrees (if not last segment)
            if (seg < segmentLengths.Count - 1)
            {
                // Turn left or right randomly
                if (Random.Range(0, 2) == 0)
                    currentDir = (currentDir + 1) % 4; // Turn left
                else
                    currentDir = (currentDir + 3) % 4; // Turn right
            }
        }

        return true;
    }

    List<int> DivideLength(int totalLength, int parts)
    {
        List<int> lengths = new List<int>();

        if (parts <= 0 || totalLength <= 0)
        {
            lengths.Add(totalLength);
            return lengths;
        }

        int remaining = totalLength;
        for (int i = 0; i < parts - 1; i++)
        {
            // Each part gets at least 1, leave enough for remaining parts
            int maxForThis = remaining - (parts - 1 - i);
            int minForThis = 1;
            int len = Random.Range(minForThis, maxForThis + 1);
            lengths.Add(len);
            remaining -= len;
        }
        lengths.Add(remaining); // Last part gets the rest

        return lengths;
    }

    Color GetSnakeBodyColor(int snakeIndex)
    {
        switch (snakeIndex)
        {
            case 0: return bodyColor;        // Snake 1: Blue
            case 1: return snake2BodyColor;  // Snake 2: Cyan
            case 2: return snake3BodyColor;  // Snake 3: Magenta
            default: return bodyColor;
        }
    }
    
    IEnumerator DisplayAllSnakesCoroutine()
    {
        if (sessionEnded)
            yield break;

        currentState = GameState.ShowingPath;
        ClearAllTargetPathSegments();
        SetPlayerVisible(false);

        int totalSnakes = numberOfSnakes + dummyPaths.Count;
        string msg = totalSnakes > 1 
            ? "Watch " + totalSnakes + " snakes!" 
            : "Watch the path!";
        if (dummyPaths.Count > 0)
            msg += " (Some are FAKE!)";
        if (flipColors)
            msg += " (FLIPPED)";
        ShowMessage(msg);

        // Initialize segment lists for each snake
        allTargetPathSegments.Clear();
        dummyPathSegments.Clear();
        for (int s = 0; s < numberOfSnakes; s++)
        {
            allTargetPathSegments.Add(new List<GameObject>());
        }
        for (int s = 0; s < dummyPaths.Count; s++)
        {
            dummyPathSegments.Add(new List<GameObject>());
        }
        
        // Build display order - use saved order on retry, shuffle only on first display
        List<int> displayOrder;
        if (isRetry && savedDisplayOrder.Count > 0)
        {
            // Use the same order as before
            displayOrder = new List<int>(savedDisplayOrder);
        }
        else
        {
            // Real snakes first (shuffled among themselves); dummy snakes always after
            displayOrder = new List<int>();
            for (int i = 0; i < numberOfSnakes; i++)
                displayOrder.Add(i);  // Real snakes: 0, 1, 2...

            for (int i = numberOfSnakes - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int temp = displayOrder[i];
                displayOrder[i] = displayOrder[j];
                displayOrder[j] = temp;
            }

            for (int i = 0; i < dummyPaths.Count; i++)
                displayOrder.Add(-(i + 1));  // Dummy snakes: -1, -2... always last

            // Save for potential retry
            savedDisplayOrder = new List<int>(displayOrder);
            
            // Build trace order: real snakes in the order they appear in displayOrder
            traceOrder.Clear();
            foreach (int id in displayOrder)
            {
                if (id >= 0)  // Real snake (not dummy)
                    traceOrder.Add(id);
            }
        }

        // Display real snakes (random order), then dummy snakes
        int realSnakeDisplayNum = 0;
        for (int displayIdx = 0; displayIdx < displayOrder.Count; displayIdx++)
        {
            if (sessionEnded)
                yield break;
            
            int snakeId = displayOrder[displayIdx];
            bool isDummy = snakeId < 0;
            int actualIdx = isDummy ? (-snakeId - 1) : snakeId;
            
            List<Vector2Int> snakePath = isDummy ? dummyPaths[actualIdx] : allTargetPaths[actualIdx];
            Color snakeBodyColor = isDummy ? dummySnakeColor : GetSnakeBodyColor(actualIdx);
            
            if (totalSnakes > 1)
            {
                if (isDummy)
                {
                    ShowMessage("IGNORE this one (dummy)");
                }
                else
                {
                    realSnakeDisplayNum++;
                    // Show which number to trace this snake (1st, 2nd, 3rd based on display order)
                    ShowMessage("Snake #" + realSnakeDisplayNum + " - REMEMBER THIS!");
                }
                yield return new WaitForSeconds(0.5f);
            }

            // Spawn segments one by one with delay
            for (int i = 0; i < snakePath.Count; i++)
            {
                if (sessionEnded)
                    yield break;

                Vector2Int gridPos = snakePath[i];
                float worldX = originX + (gridPos.x * CELL_SIZE);
                float worldY = originY + (gridPos.y * CELL_SIZE);

                GameObject segment = Instantiate(pathSegmentPrefab,
                    new Vector3(worldX, worldY, 0f), Quaternion.identity);

                // Set color based on position
                SpriteRenderer sr = segment.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    if (isDummy)
                    {
                        // Dummy snakes are all gray - no tail/head colors
                        sr.color = dummySnakeColor;
                    }
                    else
                    {
                        if (i == 0)
                            sr.color = ActualTailColor;      // First = tail
                        else if (i == snakePath.Count - 1)
                            sr.color = ActualHeadColor;      // Last = head
                        else
                            sr.color = snakeBodyColor;       // Middle = body (snake-specific color)
                    }
                }

                if (isDummy)
                {
                    dummyPathSegments[actualIdx].Add(segment);
                }
                else
                {
                    allTargetPathSegments[actualIdx].Add(segment);
                }
                targetPathSegments.Add(segment);  // Also add to legacy list for cleanup

                // Wait before showing next segment
                yield return new WaitForSeconds(segmentDelay);
            }
            
            // Small pause between snakes
            if (displayIdx < displayOrder.Count - 1)
            {
                yield return new WaitForSeconds(0.3f);
            }
        }

        if (sessionEnded)
            yield break;

        string memMsg = "Memorize the COLORED ones!";
        if (numberOfSnakes == 1 && dummyPaths.Count == 0)
            memMsg = "Memorize it!";
        else if (dummyPaths.Count == 0)
            memMsg = "Memorize all " + numberOfSnakes + "!";
        ShowMessage(memMsg);

        // Wait for display time with all paths visible
        yield return new WaitForSeconds(displayTime);

        if (sessionEnded)
            yield break;

        // Remove all segments
        ClearAllTargetPathSegments();
        
        // Delay before recall (memory retention test)
        if (delayBeforeRecall > 0f)
        {
            ShowMessage("Wait...");
            SetPlayerVisible(false);
            yield return new WaitForSeconds(delayBeforeRecall);
            
            if (sessionEnded)
                yield break;
        }

        // Transition to wait for tail state - start with first snake in trace order
        currentState = GameState.WaitForTail;
        currentSnakeIndex = 0;  // Index into traceOrder
        targetPath = allTargetPaths[traceOrder[0]];  // First snake to trace (in display order)
        SetPlayerVisible(true);
        
        // Start timing for this path
        pathStartTime = Time.time;
        firstMoveMade = false;

        if (numberOfSnakes > 1)
            ShowMessage("Trace Snake #1! Find the " + (flipColors ? "GREEN" : "RED") + " tail");
        else
            ShowMessage("Your turn!");
    }
    
    void ClearAllTargetPathSegments()
    {
        foreach (var segmentList in allTargetPathSegments)
        {
            foreach (GameObject seg in segmentList)
            {
                if (seg != null) Destroy(seg);
            }
        }
        allTargetPathSegments.Clear();
        
        // Also clear legacy list
        foreach (GameObject seg in targetPathSegments)
        {
            if (seg != null) Destroy(seg);
        }
        targetPathSegments.Clear();
    }

    IEnumerator DisplayTargetPathCoroutine(bool isNewPath)
    {
        // Legacy function - redirect to new multi-snake display
        yield return StartCoroutine(DisplayAllSnakesCoroutine());
    }

    void ClearTargetPathSegments()
    {
        foreach (GameObject seg in targetPathSegments)
        {
            if (seg != null) Destroy(seg);
        }
        targetPathSegments.Clear();
    }

    void StartDrawing()
    {
        if (SessionBlocksInput())
            return;

        currentState = GameState.Drawing;
        userPath.Clear();
        visitedCells.Clear();
        ClearUserPath();

        // Add starting position as tail
        Vector2Int startPos = new Vector2Int(gridX, gridY);
        AddUserPathSegment(startPos, true); // true = is tail

        ShowMessage("Drawing... trace the path!");
    }

    void AddUserPathSegment(Vector2Int gridPos, bool isTail = false)
    {
        userPath.Add(gridPos);
        visitedCells.Add(gridPos);

        float worldX = originX + (gridPos.x * CELL_SIZE);
        float worldY = originY + (gridPos.y * CELL_SIZE);

        GameObject segment = Instantiate(pathSegmentPrefab,
            new Vector3(worldX, worldY, -0.1f), Quaternion.identity); // Slightly in front

        SpriteRenderer sr = segment.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = isTail ? ActualTailColor : bodyColor;
        }

        userPathSegments.Add(segment);
    }

    void ClearUserPath()
    {
        foreach (GameObject seg in userPathSegments)
        {
            if (seg != null) Destroy(seg);
        }
        userPathSegments.Clear();
        userPath.Clear();
        visitedCells.Clear();
    }

    void CheckResult()
    {
        if (SessionBlocksInput())
            return;

        bool success = true;

        // Check if paths match for current snake
        if (userPath.Count != targetPath.Count)
        {
            success = false;
        }
        else
        {
            for (int i = 0; i < userPath.Count; i++)
            {
                if (!userPath[i].Equals(targetPath[i]))
                {
                    success = false;
                    break;
                }
            }
        }

        if (success)
        {
            // Check if there are more snakes to trace
            if (currentSnakeIndex < numberOfSnakes - 1)
            {
                // Show message before incrementing
                int completedSnake = currentSnakeIndex + 1;  // 1-indexed for display
                int nextSnake = currentSnakeIndex + 2;       // 1-indexed for display
                
                // Move to next snake in trace order
                currentSnakeIndex++;
                targetPath = allTargetPaths[traceOrder[currentSnakeIndex]];
                ClearUserPath();
                currentState = GameState.WaitForTail;
                ShowMessage("Snake #" + completedSnake + " done! Now trace Snake #" + nextSnake + "!");
            }
            else
            {
                // All snakes completed!
                pathsCount++;
                successCount++;
                UpdateScoreDisplay();
                // Log path data to CSV
                LogPathToCSV(true);
                
                // Handle stage progression
                HandleStageProgression(true);
                
                ShowMessage(numberOfSnakes > 1 ? "All snakes complete!" : "Great job!");
                currentState = GameState.Success;
                StartCoroutine(HandleSuccess());
            }
        }
        else
        {
            remainingChances--;
            Debug.Log("Wrong path! Remaining chances: " + remainingChances);

            if (remainingChances > 0)
            {
                ShowMessage("Try again! Snake #" + (currentSnakeIndex + 1));
                ClearUserPath();
                currentState = GameState.Fail;
                StartCoroutine(RetryWithSamePath());
            }
            else
            {
                pathsCount++;
                failCount++;
                UpdateScoreDisplay();
                // Log path data to CSV
                LogPathToCSV(false);
                
                // Handle stage progression
                HandleStageProgression(false);
                
                currentState = GameState.Fail;
                StartCoroutine(HandleFinalFailure());
            }
        }
    }
    
    /// <summary>
    /// Log path attempt data to per-user CSV file
    /// </summary>
    void LogPathToCSV(bool success)
    {
        float pathEndTime = Time.time;
        float pathDurationMs = (pathEndTime - pathStartTime) * 1000f;
        float firstMoveDelayMs = firstMoveMade ? (firstMoveTime - pathStartTime) * 1000f : -1f;
        float timeInSessionMs = (pathEndTime - sessionStartTime) * 1000f;
        
        CsvPathExporter.AppendRow(new PathCsvPayload
        {
            PlayerName = PlayerPrefs.GetString("PlayerName", "Unknown"),
            PlayerAge = PlayerPrefs.GetInt("PlayerAge", 0),
            PlayerGender = PlayerPrefs.GetString("PlayerGender", "Unknown"),
            SessionNumber = PlayerPrefs.GetInt("SessionNumber", 1),
            SessionDate = System.DateTime.Now.ToString("yyyy-MM-dd"),
            PathNumber = totalPathNumber,
            Stage = currentStage,
            StageName = (currentStage < stages.Length) ? stages[currentStage].stageName : "Unknown",
            StageAttempt = currentStageAttempt,
            GridSize = gridSize,
            PathLength = pathLength,
            NumberOfTurns = numberOfTurns,
            NumberOfSnakes = numberOfSnakes,
            NumberOfDummySnakes = numberOfDummySnakes,
            FlipColors = flipColors ? 1 : 0,
            DisplayTime = displayTime,
            SegmentDelay = segmentDelay,
            DelayBeforeRecall = delayBeforeRecall,
            Success = success ? 1 : 0,
            PathDurationMs = pathDurationMs,
            FirstMoveDelayMs = firstMoveDelayMs,
            TimeInSessionMs = timeInSessionMs,
            MlAdaptive = useMlAdaptive ? 1 : 0,
            PredictedPSuccess = useMlAdaptive ? lastPredictedPSuccess : -1f
        });

        RecordCompletedPathForMl(success, pathDurationMs);
    }

    IEnumerator RetryWithSamePath()
    {
        yield return new WaitForSeconds(1.5f);
        if (sessionEnded)
            yield break;
        // Reset to first snake and show all again in SAME order
        currentSnakeIndex = 0;
        targetPath = allTargetPaths[traceOrder[0]];  // First snake in trace order
        isRetry = true;  // Keep same display order (and trace order)
        StartCoroutine(DisplayAllSnakesCoroutine());
    }

    IEnumerator HandleSuccess()
    {
        yield return new WaitForSeconds(1.5f);
        if (sessionEnded)
            yield break;
        ClearUserPath();
        GenerateNewPath();
    }

    IEnumerator HandleFinalFailure()
    {
        // Show message
        ShowMessage("Nice try! Here's the correct path...");
        ClearUserPath();
        SetPlayerVisible(false);

        yield return new WaitForSeconds(1f);

        if (sessionEnded)
            yield break;

        // Show the correct path
        for (int i = 0; i < targetPath.Count; i++)
        {
            if (sessionEnded)
                yield break;

            Vector2Int gridPos = targetPath[i];
            float worldX = originX + (gridPos.x * CELL_SIZE);
            float worldY = originY + (gridPos.y * CELL_SIZE);

            GameObject segment = Instantiate(pathSegmentPrefab,
                new Vector3(worldX, worldY, 0f), Quaternion.identity);

            SpriteRenderer sr = segment.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (i == 0)
                    sr.color = ActualTailColor;
                else if (i == targetPath.Count - 1)
                    sr.color = ActualHeadColor;
                else
                    sr.color = bodyColor;
            }

            targetPathSegments.Add(segment);
            yield return new WaitForSeconds(segmentDelay);
        }

        if (sessionEnded)
            yield break;

        // Wait for user to see the correct path
        yield return new WaitForSeconds(displayTime);

        if (sessionEnded)
            yield break;

        // Clear and show new path message
        ClearTargetPathSegments();
        ShowMessage("Let's try a new one!");

        yield return new WaitForSeconds(1.5f);

        if (sessionEnded)
            yield break;

        // Generate new path
        GenerateNewPath();
    }

    void ShowMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
        Debug.Log(message);
    }

    void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = "Paths: " + pathsCount + "\nSuccess: " + successCount + "\nFail: " + failCount;
        }
    }

    void SetPlayerVisible(bool visible)
    {
        if (playerSpriteRenderer != null)
        {
            playerSpriteRenderer.enabled = visible;
        }
    }
}

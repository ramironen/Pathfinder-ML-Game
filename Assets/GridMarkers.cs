using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GridMarker : MonoBehaviour
{
    // Grid settings (configurable)
    [Header("Grid Settings")]
    public int gridSize = 7;  // Square grid: gridSize x gridSize cells (0 to gridSize-1)
    public GameObject gridPointPrefab;  // Optional: custom prefab for grid points
    public Color gridPointColor = new Color(0.5f, 0.5f, 0.5f, 1f);  // Gray, solid
    
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

    // Path colors
    public Color tailColor = Color.red;
    public Color bodyColor = Color.blue;
    public Color headColor = Color.green;
    
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
    public int chancesPerPath = 2;
    private int remainingChances = 0;

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

    // Target path (generated)
    private List<Vector2Int> targetPath = new List<Vector2Int>();
    private List<GameObject> targetPathSegments = new List<GameObject>();

    // User's drawn path
    private List<Vector2Int> userPath = new List<Vector2Int>();
    private List<GameObject> userPathSegments = new List<GameObject>();

    // Track visited cells during drawing
    private HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int>();

    // Reference to player's sprite renderer
    private SpriteRenderer playerSpriteRenderer;

    void Start()
    {
        CenterGridInView();
        GenerateGridVisuals();
        PositionUIRelativeToGrid();
        UpdateFlipIndicator();
        
        gridX = 0;
        gridY = 0;
        UpdateWorldPosition();
        currentState = GameState.Idle;
        sessionEnded = false;
        sessionDurationTotalSeconds = PlayerPrefs.GetInt("GameDurationSeconds", 60);
        sessionSecondsRemaining = sessionDurationTotalSeconds;
        sessionPlaySecondsAccumulated = 0f;
        csvRowCommitted = false;
        ShowMessage("Press SPACE to start");
        UpdateScoreDisplay();
        UpdateSessionTimerDisplay();

        playerSpriteRenderer = GetComponent<SpriteRenderer>();
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
        float gridLeftX = -gridWorldWidth / 2f;
        
        float indicatorStartX = gridLeftX - CELL_SIZE * 4f;  // Left of grid
        float indicatorY = 0f;  // Vertically centered
        float spacing = CELL_SIZE * 0.8f;
        
        // Create "From" color square (tail color) - left
        if (pathSegmentPrefab != null)
        {
            indicatorFrom = Instantiate(pathSegmentPrefab, 
                new Vector3(indicatorStartX, indicatorY, -0.2f), Quaternion.identity);
            indicatorFrom.transform.localScale = Vector3.one * 0.6f;  // Smaller
            SpriteRenderer sr = indicatorFrom.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = ActualTailColor;
        }
        
        // Create arrow (black bar pointing right) - middle
        if (pathSegmentPrefab != null)
        {
            indicatorArrow = Instantiate(pathSegmentPrefab,
                new Vector3(indicatorStartX + spacing, indicatorY, -0.2f), Quaternion.identity);
            indicatorArrow.transform.localScale = new Vector3(1.2f, 0.15f, 1f);  // Longer and thinner (arrow-like)
            SpriteRenderer sr = indicatorArrow.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = Color.black;
        }
        
        // Create "To" color square (head color) - right
        if (pathSegmentPrefab != null)
        {
            indicatorTo = Instantiate(pathSegmentPrefab,
                new Vector3(indicatorStartX + spacing * 2, indicatorY, -0.2f), Quaternion.identity);
            indicatorTo.transform.localScale = Vector3.one * 0.6f;  // Smaller
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
            : PlayerPrefs.GetInt("GameDurationSeconds", 60);

        float difficultyScore = CalculateDifficultyScore(pathLength, numberOfTurns, displayTime, segmentDelay);
        float successRate = pathsCount > 0 ? (float)successCount / pathsCount : 0f;
        float performanceGrade = CalculatePerformanceGrade(successRate, difficultyScore);

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
            ChancesPerPath = chancesPerPath,
            PathsTotal = pathsCount,
            Success = successCount,
            Fail = failCount,
            SecondsPlayed = Mathf.RoundToInt(sessionPlaySecondsAccumulated),
            DifficultyScore = difficultyScore,
            SuccessRate = successRate,
            PerformanceGrade = performanceGrade
        };
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

        targetPath.Clear();

        // Reset chances for new path
        remainingChances = chancesPerPath;

        // Try to generate valid path (max 100 attempts)
        bool validPath = false;
        for (int attempt = 0; attempt < 100 && !validPath; attempt++)
        {
            validPath = TryGeneratePath();
        }

        if (validPath)
        {
            StartCoroutine(DisplayTargetPathCoroutine(true));
        }
        else
        {
            Debug.LogWarning("Could not generate valid path");
        }
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

    IEnumerator DisplayTargetPathCoroutine(bool isNewPath)
    {
        if (sessionEnded)
            yield break;

        currentState = GameState.ShowingPath;
        ClearTargetPathSegments();
        SetPlayerVisible(false);

        if (flipColors)
            ShowMessage("FLIPPED! Green=Start, Red=End");
        else
            ShowMessage("Watch the path!");

        // Spawn segments one by one with delay
        for (int i = 0; i < targetPath.Count; i++)
        {
            if (sessionEnded)
                yield break;

            Vector2Int gridPos = targetPath[i];
            float worldX = originX + (gridPos.x * CELL_SIZE);
            float worldY = originY + (gridPos.y * CELL_SIZE);

            GameObject segment = Instantiate(pathSegmentPrefab,
                new Vector3(worldX, worldY, 0f), Quaternion.identity);

            // Set color based on position (use Actual colors which respect flipColors)
            SpriteRenderer sr = segment.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (i == 0)
                    sr.color = ActualTailColor;      // First = tail
                else if (i == targetPath.Count - 1)
                    sr.color = ActualHeadColor;      // Last = head
                else
                    sr.color = bodyColor;            // Middle = body (blue)
            }

            targetPathSegments.Add(segment);

            // Wait before showing next segment
            yield return new WaitForSeconds(segmentDelay);
        }

        if (sessionEnded)
            yield break;

        ShowMessage("Memorize it!");

        // Wait for display time with full path visible
        yield return new WaitForSeconds(displayTime);

        if (sessionEnded)
            yield break;

        // Remove all segments
        ClearTargetPathSegments();

        // Transition to wait for tail state
        currentState = GameState.WaitForTail;
        SetPlayerVisible(true);
        ShowMessage("Your turn!");
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

        // Check if paths match
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
            pathsCount++;
            successCount++;
            UpdateScoreDisplay();
            ShowMessage("Great job!");
            currentState = GameState.Success;
            StartCoroutine(HandleSuccess());
        }
        else
        {
            remainingChances--;
            Debug.Log("Wrong path! Remaining chances: " + remainingChances);

            if (remainingChances > 0)
            {
                ShowMessage("Try again!");
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
        }
    }

    IEnumerator RetryWithSamePath()
    {
        yield return new WaitForSeconds(1.5f);
        if (sessionEnded)
            yield break;
        StartCoroutine(DisplayTargetPathCoroutine(false));
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

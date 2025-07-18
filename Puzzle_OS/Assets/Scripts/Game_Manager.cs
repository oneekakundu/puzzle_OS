using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;  // <-- Added for TextMeshPro support

public class GameManager : MonoBehaviour
{
    [SerializeField] private Transform gameTransform;
    [SerializeField] private Transform piecePrefab;

    [Header("Audio")]
    [SerializeField] private AudioClip moveSFX;
    private AudioSource audioSource;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI moveCounterText;  // UI reference

    [SerializeField] private TextMeshProUGUI timerText;     // timer


    private List<Transform> pieces;
    private int emptyLocation;
    private int size;
    private bool shuffling = false;

    private int moveCount = 0;  // Tracks number of moves

    private float elapsedTime = 0f;
    private bool timerRunning = false;      //timer var


    private void CreateGamePieces(float gapThickness)
    {
        float width = 1f / size;
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                Transform piece = Instantiate(piecePrefab, gameTransform);
                pieces.Add(piece);
                piece.localPosition = new Vector3(
                    -1 + (2 * width * col) + width,
                    +1 - (2 * width * row) - width,
                    0
                );
                piece.localScale = ((2 * width) - gapThickness) * Vector3.one;
                piece.name = $"{(row * size) + col}";

                if (row == size - 1 && col == size - 1)
                {
                    emptyLocation = (size * size) - 1;
                    piece.gameObject.SetActive(false);
                }
                else
                {
                    float gap = gapThickness / 2;
                    Mesh mesh = piece.GetComponent<MeshFilter>().mesh;
                    Vector2[] uv = new Vector2[4];
                    uv[0] = new Vector2((width * col) + gap, 1 - ((width * (row + 1)) - gap));
                    uv[1] = new Vector2((width * (col + 1)) - gap, 1 - ((width * (row + 1)) - gap));
                    uv[2] = new Vector2((width * col) + gap, 1 - ((width * row) + gap));
                    uv[3] = new Vector2((width * (col + 1)) - gap, 1 - ((width * row) + gap));
                    mesh.uv = uv;
                }
            }
        }
    }

    void Start()
    {
        pieces = new List<Transform>();
        size = 4;
        CreateGamePieces(0.01f);

        // Audio setup
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = 1f;

        // Initialize move counter UI
        moveCount = 0;
        UpdateMoveCounterUI();
    }

    void Update()
    {
        //update timer
        if (timerRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerUI();
        }

        if (!shuffling && CheckCompletion())
        {
            timerRunning = false;   //stop timer
            shuffling = true;
            StartCoroutine(WaitShuffle(0.5f));
        }

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(
                Camera.main.ScreenToWorldPoint(Input.mousePosition),
                Vector2.zero
            );

            if (hit)
            {
                for (int i = 0; i < pieces.Count; i++)
                {
                    if (pieces[i] == hit.transform)
                    {
                        if (TryMove(i, -size, size)) break;
                        if (TryMove(i, +size, size)) break;
                        if (TryMove(i, -1, 0)) break;
                        if (TryMove(i, +1, size - 1)) break;
                    }
                }
            }
        }
    }

    // Attempt a swap and count move if successful
    private bool TryMove(int i, int offset, int colCheck)
    {
        if ((i % size) != colCheck && (i + offset) == emptyLocation)
        {
            // Swap positions & pieces in list
            (pieces[i], pieces[i + offset]) = (pieces[i + offset], pieces[i]);
            (pieces[i].localPosition, pieces[i + offset].localPosition) =
                (pieces[i + offset].localPosition, pieces[i].localPosition);
            emptyLocation = i;

            // Play audio feedback
            if (!shuffling && moveSFX && audioSource)
                audioSource.PlayOneShot(moveSFX);

            // Count and update move display
            if (!shuffling)
            {
                moveCount++;
                UpdateMoveCounterUI();
            }

            return true;
        }
        return false;
    }

    private void UpdateMoveCounterUI()
    {
        if (moveCounterText != null)
            moveCounterText.text = $"Moves: {moveCount}";
    }

    private bool CheckCompletion()
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i].name != $"{i}")
                return false;
        }
        return true;
    }

    private IEnumerator WaitShuffle(float duration)
    {
        yield return new WaitForSeconds(duration);
        Shuffle();
        shuffling = false;
    }

    private void Shuffle()
    {
        int count = 0;
        int last = emptyLocation;
        while (count < size * size * size)
        {
            int rnd = Random.Range(0, size * size);
            if (rnd == last) continue;
            last = emptyLocation;

            if (TryMove(rnd, -size, size)
                || TryMove(rnd, +size, size)
                || TryMove(rnd, -1, 0)
                || TryMove(rnd, +1, size - 1))
            {
                count++;
            }
        }

        // Reset move counter after shuffling
        moveCount = 0;
        UpdateMoveCounterUI();
        //start timer
        elapsedTime = 0f;
        timerRunning = true;
        UpdateTimerUI();

    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);
            timerText.text = $"Time: {minutes:00}:{seconds:00}";
        }
    }

}

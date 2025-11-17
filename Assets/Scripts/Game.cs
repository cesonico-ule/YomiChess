using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using TMPro;
using Unity.Collections;
// using UnityEngine.Debug.log;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
// using System.Diagnostics; Collides with debug.log

public class Game : NetworkBehaviour {

	public GameObject chesspiece;
	public static Game Instance;

	// UI References - assigned in the Inspector
	public GameObject gameOverPanel;
	public TextMeshProUGUI winnerText;
	public Button newGameButton;

	// Positions and team for each piece
	private GameObject[,] positions = new GameObject[8, 8];
	private GameObject[] playerBlack = new GameObject[16];
	private GameObject[] playerWhite = new GameObject[16];

	// private string currentPlayer = "white"; // This might be useless or work to determine the player in a match against a bot (I ended up using other parameter
	// Game mode variables - This is now usefull for the classic mode
	private NetworkVariable<bool> isClassicMode = new NetworkVariable<bool>(false);
	private NetworkVariable<bool> isWhiteTurn = new NetworkVariable<bool>(true); // White always starts

	// UI References for win counter
	public TextMeshProUGUI whiteWinsText;
	public TextMeshProUGUI blackWinsText;

	// Win counters (synced across network)
	private NetworkVariable<int> whiteWins = new NetworkVariable<int>(0);
	private NetworkVariable<int> blackWins = new NetworkVariable<int>(0);

	private bool gameOver = false;

	// Store the mode LOCALLY (not networked yet)
	private bool localGameModeChoice = false;

	// Track if callback has already been set up
	private bool networkCallbackSetup = false;

	private void Awake() { // Make sure there is only one instance of the game controller
		if (Instance != null && Instance != this) {
			Destroy(gameObject);
		} else {
			Instance = this;
		}
	}


	// Start is called before the first frame update
	void Start() {
		// Hide game over panel at start
		if (gameOverPanel != null) {
			gameOverPanel.SetActive(false);
		}
		// Set up new game button
		if (newGameButton != null) {
			newGameButton.onClick.AddListener(OnNewGameClicked);
		}

		// Initialize win counter display
		UpdateWinCounterDisplay();
		// Now the network connection won't start here -> moved to StartGameSetup

	}

	public void StartGameSetup() { // new version of the method
		// If callback was already set up, remove it first to avoid duplicates
		if (networkCallbackSetup) {
			NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
		}

		// Now the callback is set up
		NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
		networkCallbackSetup = true;
	}


	private void OnClientConnected(ulong clientId) {
		Debug.Log("Client with id " + clientId + " connected to the server");

		// If we're the HOST (server), set the game mode NOW when client connects
		if (NetworkManager.Singleton.IsHost) {
			// Set the game mode based on what the host selected
			isClassicMode.Value = localGameModeChoice;
			Debug.Log("Host set game mode to: " + (isClassicMode.Value ? "Classic" : "Chess Kune Do"));

			// If we have 2 players, start the game
			if (NetworkManager.Singleton.ConnectedClients.Count == 2) {
				Debug.Log("Game is ready to begin");
				isWhiteTurn.Value = true;
				SetUpBoard();
			}
		}

		// If this is a CLIENT that just connected
		if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost) {
			Debug.Log("Client connected, will verify game mode...");
			StartCoroutine(VerifyGameModeAfterConnection());
		}
	}

	public void SetUpBoard() {

		// if (!IsServer) return; // Bugged for some reason

		playerWhite = new GameObject[] {
			Create("white_king", 4, 0), Create("white_queen", 3, 0),
			Create("white_rook", 0, 0), Create("white_rook", 7, 0),
			Create("white_bishop", 2, 0), Create("white_bishop", 5, 0),
			Create("white_knight", 1, 0), Create("white_knight", 6, 0),
			Create("white_pawn", 0, 1), Create("white_pawn", 1, 1), Create("white_pawn", 2, 1), Create("white_pawn", 3, 1),
			Create("white_pawn", 4, 1), Create("white_pawn", 5, 1), Create("white_pawn", 6, 1), Create("white_pawn", 7, 1)
		};

		playerBlack = new GameObject[] {
			Create("black_king", 4, 7), Create("black_queen", 3, 7),
			Create("black_rook", 0, 7), Create("black_rook", 7, 7),
			Create("black_bishop", 2, 7), Create("black_bishop", 5, 7),
			Create("black_knight", 1, 7), Create("black_knight", 6, 7),
			Create("black_pawn", 0, 6), Create("black_pawn", 1, 6), Create("black_pawn", 2, 6), Create("black_pawn", 3, 6),
			Create("black_pawn", 4, 6), Create("black_pawn", 5, 6), Create("black_pawn", 6, 6), Create("black_pawn", 7, 6)
		};

		// Set all positions on the position board
		for (int i = 0; i < playerWhite.Length; i++) {
			SetPosition(playerWhite[i]);
			SetPosition(playerBlack[i]);
		}

		// Setup chess pieces
		// Instantiate(chesspiece, new Vector3(0, 0, -1), Quaternion.identity);
	}


	public GameObject Create(string name, int x, int y) {
		GameObject obj = Instantiate(chesspiece, new Vector3(0, 0, -1), Quaternion.identity);
		NetworkObject netObj = obj.GetComponent<NetworkObject>();


		Chessman cm = obj.GetComponent<Chessman>();
		FixedString32Bytes nn = new FixedString32Bytes(name);
		cm.pieceName.Value = nn;
		cm.SetXBoard(x);
		cm.SetYBoard(y);

		cm.controller = GameObject.FindGameObjectWithTag("GameController");

		cm.Activate(); // Sugested removal

		netObj.Spawn();
		// obj.GetComponent<NetworkObject>().Spawn(); // Previous attempt
		return obj;
	}

	public void SetPosition(GameObject obj) {
		// Set the position of the chess piece on the board
		// Chessman cm = obj.GetComponent<Chessman>();
		// positions[cm.GetXBoard(), cm.GetYBoard()] = obj;


		/*
		if (cm != null) {
			cm.SetCoordinates();
			// Update the positions array if needed
			controller.GetComponent<Game>().SetPosition(obj);
		}
		*/

		// New RPC method
		Chessman cm = obj.GetComponent<Chessman>();
		int x = cm.GetXBoard();
		int y = cm.GetYBoard();

		positions[x, y] = obj;

		// Sync to all clients if we're the server
		if (NetworkManager.Singleton.IsServer) {
			NetworkObject netObj = obj.GetComponent<NetworkObject>();
			UpdatePositionClientRpc(netObj, x, y);
		}

	}

	public void SetPositionEmpty(int x, int y) {
		// Set the position of the chess piece on the board to empty
		positions[x, y] = null;
		// Sync to all clients if we're the server
		if (NetworkManager.Singleton.IsServer) {
			SetPositionEmptyClientRpc(x, y);
		}
	}

	public GameObject GetPosition(int x, int y) {
		// Get the position of the chess piece on the board
		return positions[x, y];
	}

	public bool PositionOnBoard(int x, int y) {
		if (x < 0 || x > 7 || y < 0 || y > 7) {
			return false;
		} else {
			return true;
		}
	}

	public string GetCurrentPlayerColor() {
		// Host plays white, client plays black
		if (NetworkManager.Singleton.IsHost) {
			return "white";
		} else {
			return "black";
		}
	}

	public bool IsGameOver() {
		return gameOver;
	}

	public void CheckForKingCapture(GameObject capturedPiece) {
		if (capturedPiece == null) return;

		Chessman chess = capturedPiece.GetComponent<Chessman>();
		string pieceName = chess.pieceName.Value.ToString().Trim();

		if (pieceName == "white_king" || pieceName == "black_king") {
			string winner = pieceName == "white_king" ? "Black" : "White";
			if (NetworkManager.Singleton.IsServer) {
				// Increment the appropriate win counter
				if (winner == "White") {
					whiteWins.Value++;
				} else {
					blackWins.Value++;
				}
				GameOverClientRpc(winner);
			}
		}
	}

	public void CleanupGame() { // New method to clean up the game state when returning to menu, made it just in case anything breaks

		// Destroy all pieces on the board
		for (int x = 0; x < 8; x++) {
			for (int y = 0; y < 8; y++) {
				if (positions[x, y] != null) {
					GameObject piece = positions[x, y];
					NetworkObject netObj = piece.GetComponent<NetworkObject>();

					// Only despawn if we're the server and the object is spawned
					if (netObj != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && netObj.IsSpawned) {
						netObj.Despawn();
					}

					Destroy(piece);
					positions[x, y] = null;
				}
			}
		}

		// Destroy all move plates
		GameObject[] movePlates = GameObject.FindGameObjectsWithTag("MovePlate");
		foreach (GameObject mp in movePlates) {
			Destroy(mp);
		}

		// Reset game state
		gameOver = false;
		if (gameOverPanel != null) {
			gameOverPanel.SetActive(false);
		}

		// Clear player arrays
		playerWhite = new GameObject[16];
		playerBlack = new GameObject[16];

		// Reset turn to white
		if (NetworkManager.Singleton.IsServer) {
			isWhiteTurn.Value = true;

			// Reset win counters when returning to menu (optional)
			// whiteWins.Value = 0;
			// blackWins.Value = 0;
		}

		// Update display
		UpdateWinCounterDisplay();

		// REMOVE CALLBACK
		if (networkCallbackSetup && NetworkManager.Singleton != null) {
			NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
			networkCallbackSetup = false;
		}

		Debug.Log("Game cleaned up and ready for new session");
	}

	// Classic gamemode
	// Method to set game mode - call this from MenuController
	public void SetGameMode(bool classic) {
		localGameModeChoice = classic;
		Debug.Log("Local game mode choice set to: " + (classic ? "Classic" : "Chess Kune Do"));
	}

	public bool IsClassicMode() {
		return isClassicMode.Value;
	}

	// Check if it's the current player's turn (only matters in classic mode)
	public bool IsMyTurn() {
		if (!isClassicMode.Value) {
			return true; // In Chess Kune Do, it's always your turn
		}

		// In classic mode, check if your color matches the current turn
		string myColor = GetCurrentPlayerColor();
		bool whiteTurn = isWhiteTurn.Value;

		if (myColor == "white" && whiteTurn) {
			return true;
		} else if (myColor == "black" && !whiteTurn) {
			return true;
		}

		return false;
	}

	// Method to switch turns after a move (only in classic mode)
	public void SwitchTurn() {
		if (!isClassicMode.Value) {
			return; // Don't switch turns in Chess Kune Do
		}

		if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) {
			isWhiteTurn.Value = !isWhiteTurn.Value;
			Debug.Log("Turn switched to: " + (isWhiteTurn.Value ? "White" : "Black"));
		} else {
			Debug.Log("Client called SwitchTurn - requesting server to switch");
			// If client calls this, request the server to switch
			SwitchTurnServerRpc();
		}
	}

	private void OnEnable() {
		whiteWins.OnValueChanged += OnWinCountChanged;
		blackWins.OnValueChanged += OnWinCountChanged;
	}

	private void OnDisable() {
		whiteWins.OnValueChanged -= OnWinCountChanged;
		blackWins.OnValueChanged -= OnWinCountChanged;
	}

	// Update display when win count changes
	private void OnWinCountChanged(int oldValue, int newValue) {
		UpdateWinCounterDisplay();
	}

	// Method to update the win counter display
	private void UpdateWinCounterDisplay() {
		if (whiteWinsText != null) {
			whiteWinsText.text = "White: " + whiteWins.Value;
		}
		if (blackWinsText != null) {
			blackWinsText.text = "Black: " + blackWins.Value;
		}
	}


	// ---	NETWORK ---
	public void Starthost() {
		NetworkManager.Singleton.StartHost();
	}

	public void StartClient() {
		NetworkManager.Singleton.StartClient();
	}

	[ServerRpc(RequireOwnership = false)]
	public void RequestDestroyPieceServerRpc(int x, int y) {
		GameObject piece = GetPosition(x, y);
		if (piece != null) {
			// Check if it's a king before destroying
			CheckForKingCapture(piece);

			NetworkObject netObj = piece.GetComponent<NetworkObject>();
			if (netObj != null) {
				netObj.Despawn();
			}
			Destroy(piece);
			SetPositionEmpty(x, y);
		}
	}

	[ClientRpc]
	public void UpdatePositionClientRpc(NetworkObjectReference pieceRef, int x, int y) {
		if (pieceRef.TryGet(out NetworkObject netObj)) {
			GameObject piece = netObj.gameObject;
			positions[x, y] = piece;
		}
	}

	[ClientRpc]
	public void SetPositionEmptyClientRpc(int x, int y) {
		positions[x, y] = null;
	}

	[ClientRpc]
	private void GameOverClientRpc(string winner) {
		gameOver = true;
		if (gameOverPanel != null) {
			gameOverPanel.SetActive(true);
		}
		if (winnerText != null) {
			winnerText.text = winner + " Wins!";
		}
	}

	private void OnNewGameClicked() {
		ResetGameServerRpc();

		/*if (NetworkManager.Singleton.IsServer) { // This is only in case I want to not allow clients to reset the game
			ResetGameServerRpc();
		}
		*/
	}

	[ServerRpc(RequireOwnership = false)]
	private void ResetGameServerRpc() {
		ResetGameClientRpc();
	}

	[ClientRpc]
	private void ResetGameClientRpc() {
		// Destroy all pieces
		for (int x = 0; x < 8; x++) {
			for (int y = 0; y < 8; y++) {
				if (positions[x, y] != null) {
					GameObject piece = positions[x, y];
					NetworkObject netObj = piece.GetComponent<NetworkObject>();
					if (netObj != null && NetworkManager.Singleton.IsServer) {
						netObj.Despawn();
					}
					Destroy(piece);
					positions[x, y] = null;
				}
			}
		}

		// Destroy all move plates
		GameObject[] movePlates = GameObject.FindGameObjectsWithTag("MovePlate");
		foreach (GameObject mp in movePlates) {
			Destroy(mp);
		}

		// Reset game state
		gameOver = false;
		if (gameOverPanel != null) {
			gameOverPanel.SetActive(false);
		}

		// Set up new board (only host)
		if (NetworkManager.Singleton.IsServer) {
			SetUpBoard();
		}

		// Reset turn to white
		if (NetworkManager.Singleton.IsServer) {
			isWhiteTurn.Value = true;
		}

	}

	[ServerRpc(RequireOwnership = false)]
	private void SwitchTurnServerRpc() {
		if (isClassicMode.Value) {
			isWhiteTurn.Value = !isWhiteTurn.Value;
			Debug.Log("Server switched turn to: " + (isWhiteTurn.Value ? "White" : "Black"));
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void RequestSetGameModeServerRpc(bool classic, ServerRpcParams rpcParams = default) {
		// Server recieves request from client
		Debug.Log("Client requested game mode: " + (classic ? "Classic" : "Chess Kune Do"));

		// verify if the requested mode matches the server's mode
		if (isClassicMode.Value != classic) {
			Debug.LogWarning("Game mode mismatch! Server is " +
				(isClassicMode.Value ? "Classic" : "Chess Kune Do") +
				" but client wants " + (classic ? "Classic" : "Chess Kune Do"));

			// Notify the client about the mismatch
			NotifyGameModeMismatchClientRpc(rpcParams.Receive.SenderClientId);
			return;
		}

		Debug.Log("Game modes match! Proceeding...");
	}

	[ClientRpc]
	private void NotifyGameModeMismatchClientRpc(ulong clientId) {
		// Only notify the specific client
		if (NetworkManager.Singleton.LocalClientId == clientId) {
			Debug.LogError("Cannot join: Host is playing a different game mode!");

			// disconnect from server
			if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost) {
				NetworkManager.Singleton.Shutdown();
			}

		}
	}

	private IEnumerator VerifyGameModeAfterConnection() {
		// Wait for network variables to sync
		float waitTime = 0f;
		float maxWait = 3f; // Maximum 3 seconds

		// Keep waiting until isClassicMode has been set by the server OR timeout
		while (waitTime < maxWait) {
			yield return new WaitForSeconds(0.2f);
			waitTime += 0.2f;

			Debug.Log("Checking sync... Client wants: " + (localGameModeChoice ? "Classic" : "Chess Kune Do") +
					  ", Server has: " + (isClassicMode.Value ? "Classic" : "Chess Kune Do"));

			// Check if modes match
			if (localGameModeChoice == isClassicMode.Value) {
				Debug.Log("Game modes match! Ready to play.");
				yield break; // Exit successfully
			}
		}

		// If we get here, there was a mismatch
		Debug.LogError("Game mode mismatch after " + maxWait + " seconds!");
		Debug.LogError("You selected " + (localGameModeChoice ? "Classic" : "Chess Kune Do") +
					  " but the host is playing " + (isClassicMode.Value ? "Classic" : "Chess Kune Do"));

		// Disconnect
		if (NetworkManager.Singleton != null) {
			NetworkManager.Singleton.Shutdown();
		}

		// Return to menu
		if (MenuController.Instance != null) {
			MenuController.Instance.ShowMainMenuAfterError("Game mode mismatch! Host is playing " +
				(isClassicMode.Value ? "Classic" : "Chess Kune Do") + " mode.");
		}
	}
}
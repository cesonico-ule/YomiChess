using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using Unity.Collections;
// using UnityEngine.Debug.log;
using Unity.Netcode;
using UnityEngine;
// using System.Diagnostics; Collides with debug.log

public class Game : NetworkBehaviour {

	public GameObject chesspiece;
	public static Game Instance;

    // Positions and team for each piece
    private GameObject[,] positions = new GameObject[8, 8];
    private GameObject[] playerBlack = new GameObject[16];
	private GameObject[] playerWhite = new GameObject[16];

    private string currentPlayer = "white"; // This might be useless or work to determine the player in a match against a bot

    private bool gameOver = false;

	private void Awake() { // Make sure there is only one instance of the game controller
		if(Instance!=null && Instance != this) {
			Destroy(gameObject);
		} else {
			Instance = this;
		}
	}


	// Start is called before the first frame update
	void Start() {
		NetworkManager.Singleton.OnClientConnectedCallback += (clientId) => {
			Debug.Log("Client with id " + clientId + " connected to the server");
			if (NetworkManager.Singleton.IsHost && NetworkManager.Singleton.ConnectedClients.Count == 2) {
				Debug.Log("Game is ready to begin");
				currentPlayer = "white";
				SetUpBoard();
			} else {
				currentPlayer = "black";
			}
		};
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

	public void SetPositionEmpty (int x, int y) {
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

}
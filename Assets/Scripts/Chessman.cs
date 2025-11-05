using System.Collections;
using System.Collections.Generic;
// using System.Diagnostics;
using System.Security.Cryptography;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Chessman : NetworkBehaviour {
	// References
	public GameObject controller;
	public GameObject movePlate;

	// Positions
	private NetworkVariable<int> xBoard = new NetworkVariable<int>(-1);
	private NetworkVariable<int> yBoard = new NetworkVariable<int>(-1);

	//Variable to keep track of black or white player
	private string player;

	//References for all the sprites that the chesspiece can be
	public Sprite black_king, black_queen, black_rook, black_bishop, black_knight, black_pawn, white_king, white_queen, white_rook, white_bishop, white_knight, white_pawn;

	// I replaced the name variable to make it Network compatible
	public NetworkVariable<FixedString32Bytes> pieceName = new NetworkVariable<FixedString32Bytes>("white_king");


	public void Activate() {

		if (!IsServer) return; // Only the server/host sets NetworkVariables!

		controller = GameObject.FindGameObjectWithTag("GameController");
		
		// Take the instantiated location and adjust the transform
		UpdatePosition();

		// Updates sprite (from default)
		UpdateSprite(this.pieceName.ToString());

	}
	
	// Deprecated old method
	public void SetCoordinates() {

		/*
		// Get the position of the chess piece and set the coordinates
		Vector3 pos = this.transform.position;
		xBoard = (int)pos.x;
		yBoard = (int)pos.y;
		// Set the position of the move plate
		movePlate.transform.position = new Vector3(xBoard, yBoard, 0);
		*/

	}

	private void OnMouseUp()  {
		string currentPlayerColor = controller.GetComponent<Game>().GetCurrentPlayerColor();

		// Check if game is over before allowing piece selection
		if (controller != null && controller.GetComponent<Game>().IsGameOver()) {
			return;
		}

		// Only allow moving pieces that match the current player's color
		if (player != currentPlayerColor) {
			Debug.Log("Not your piece! You control " + currentPlayerColor + " pieces.");
			return;
		}

		// Debug.Log("ow!");
		DestroyMovePlates();
		InitiateMovePlates();
	}

	public void DestroyMovePlates() {
		GameObject[] movePlates = GameObject.FindGameObjectsWithTag("MovePlate");
		for (int i = 0; i < movePlates.Length; i++) {
			Destroy(movePlates[i]);
		}
	}

	public void InitiateMovePlates() {
		// Debug.Log("Soy " + this.pieceName.Value.ToString());
		string nombreCuerda = this.pieceName.Value.ToString();
		// Debug.Log("También soy " + nombreCuerda);
		// Debug.Log("Mis cordenadas son: " + xBoard.Value + "|" + yBoard.Value);

		switch (nombreCuerda) {
			case "black_king":
			case "white_king":
				SurroundMovePlate();
				break;
			case "black_queen":
			case "white_queen":
				LineMovePlate(1, 0);
				LineMovePlate(0, 1);
				LineMovePlate(-1, 0);
				LineMovePlate(0, -1);
				LineMovePlate(1, 1);
				LineMovePlate(-1, 1);
				LineMovePlate(1, -1);
				LineMovePlate(-1, -1);
				break;
			case "black_rook":
			case "white_rook":
				LineMovePlate(1, 0);
				LineMovePlate(0, 1);
				LineMovePlate(-1, 0);
				LineMovePlate(0, -1);
				break;
			case "black_bishop":
			case "white_bishop":
				LineMovePlate(1, 1);
				LineMovePlate(-1, 1);
				LineMovePlate(1, -1);
				LineMovePlate(-1, -1);
				break;
			case "black_knight":
			case "white_knight":
				LMovePlate();
				break;
			case "black_pawn":
				PawnMovePlate(xBoard.Value, yBoard.Value - 1);
				break;
			case "white_pawn":
				PawnMovePlate(xBoard.Value, yBoard.Value + 1);
				break;
		}
	}


	// Getters and Setters for the board coordinates

	public int GetXBoard() {
		return xBoard.Value;
	}

	public int GetYBoard() {
		return yBoard.Value;
	}

	public void SetXBoard(int x) {
		xBoard.Value = x;
	}

	public void SetYBoard(int y) {
		yBoard.Value = y;
	}

	// GH post
	public void LineMovePlate(int xIncrement, int yIncrement) {
		Game sc = controller.GetComponent<Game>();

		int x = xBoard.Value + xIncrement;
		int y = yBoard.Value + yIncrement;

		while (sc.PositionOnBoard(x, y) && sc.GetPosition(x, y) == null) {
			MovePlateSpawn(x, y);
			x += xIncrement;
			y += yIncrement;
		}

		if (sc.PositionOnBoard(x, y) && sc.GetPosition(x, y).GetComponent<Chessman>().player != player) {
			MovePlateAttackSpawn(x, y);
		}
	}

	public void LMovePlate() {
		PointMovePlate(xBoard.Value + 1, yBoard.Value + 2);
		PointMovePlate(xBoard.Value - 1, yBoard.Value + 2);
		PointMovePlate(xBoard.Value + 2, yBoard.Value + 1);
		PointMovePlate(xBoard.Value + 2, yBoard.Value - 1);
		PointMovePlate(xBoard.Value + 1, yBoard.Value - 2);
		PointMovePlate(xBoard.Value - 1, yBoard.Value - 2);
		PointMovePlate(xBoard.Value - 2, yBoard.Value + 1);
		PointMovePlate(xBoard.Value - 2, yBoard.Value - 1);
	}

	public void SurroundMovePlate() {
		PointMovePlate(xBoard.Value, yBoard.Value + 1);
		PointMovePlate(xBoard.Value, yBoard.Value - 1);
		PointMovePlate(xBoard.Value - 1, yBoard.Value + 0);
		PointMovePlate(xBoard.Value - 1, yBoard.Value - 1);
		PointMovePlate(xBoard.Value - 1, yBoard.Value + 1);
		PointMovePlate(xBoard.Value + 1, yBoard.Value + 0);
		PointMovePlate(xBoard.Value + 1, yBoard.Value - 1);
		PointMovePlate(xBoard.Value + 1, yBoard.Value + 1);
	}

	public void PointMovePlate(int x, int y) {
		Game sc = controller.GetComponent<Game>();
		if (sc.PositionOnBoard(x, y)) {
			GameObject cp = sc.GetPosition(x, y);

			if (cp == null) {
				MovePlateSpawn(x, y);
			} else if (cp.GetComponent<Chessman>().player != player) {
				MovePlateAttackSpawn(x, y);
			}
		}
	}

	public void PawnMovePlate(int x, int y) {
		Game sc = controller.GetComponent<Game>();
		if (sc.PositionOnBoard(x, y)) {
			if (sc.GetPosition(x, y) == null) {
				MovePlateSpawn(x, y);

				// Double advance for pawns in starting position
				int startRow = player == "white" ? 1 : 6;
				int direction = player == "white" ? 1 : -1;

				if (yBoard.Value == startRow) {
					int doubleY = y + direction;
					// Check if both squares ahead are empty
					if (sc.PositionOnBoard(x, doubleY) && sc.GetPosition(x, doubleY) == null) {
						MovePlateSpawn(x, doubleY);
					}

				}

			}

			// Pawn eating moves

			if (sc.PositionOnBoard(x + 1, y) && sc.GetPosition(x + 1, y) != null && sc.GetPosition(x + 1, y).GetComponent<Chessman>().player != player) {
				MovePlateAttackSpawn(x + 1, y);
			}

			if (sc.PositionOnBoard(x - 1, y) && sc.GetPosition(x - 1, y) != null && sc.GetPosition(x - 1, y).GetComponent<Chessman>().player != player) {
				MovePlateAttackSpawn(x - 1, y);
			}
			
		}
	}

	public void MovePlateSpawn(int matrixX, int matrixY) {
		//Get the board value in order to convert to xy coords
		float x = matrixX;
		float y = matrixY;

		//Adjust by variable offset
		x *= 0.66f;
		y *= 0.66f;

		//Add constants (pos 0,0)
		x += -2.3f;
		y += -2.19f;

		//Set actual unity values
		GameObject mp = Instantiate(movePlate, new Vector3(x, y, -3.0f), Quaternion.identity);

		MovePlate mpScript = mp.GetComponent<MovePlate>();
		mpScript.SetReference(gameObject);
		mpScript.SetCoords(matrixX, matrixY);
	}

	public void MovePlateAttackSpawn(int matrixX, int matrixY) {
		//Get the board value in order to convert to xy coords
		float x = matrixX;
		float y = matrixY;

		//Adjust by variable offset
		x *= 0.66f;
		y *= 0.66f;

		//Add constants (pos 0,0)
		x += -2.3f;
		y += -2.19f;

		//Set actual unity values
		GameObject mp = Instantiate(movePlate, new Vector3(x, y, -3.0f), Quaternion.identity);

		MovePlate mpScript = mp.GetComponent<MovePlate>();
		mpScript.attack = true;
		mpScript.SetReference(gameObject);
		mpScript.SetCoords(matrixX, matrixY);
	}

	public void CheckPromotion() {
		string pieceType = pieceName.Value.ToString().Trim();

		// Check if it's a pawn that reached the final rank
		if (pieceType == "white_pawn" && yBoard.Value == 7) {
			PromoteToQueenServerRpc();
		} else if (pieceType == "black_pawn" && yBoard.Value == 0) {
			PromoteToQueenServerRpc();
		}
	}

	// Network handler
	private void OnEnable() {
		pieceName.OnValueChanged += OnPieceNameChanged;
		xBoard.OnValueChanged += OnPositionChanged;
		yBoard.OnValueChanged += OnPositionChanged;
	}

	private void OnDisable() {
		pieceName.OnValueChanged -= OnPieceNameChanged;
		xBoard.OnValueChanged -= OnPositionChanged;
		yBoard.OnValueChanged -= OnPositionChanged;
	}

	public override void OnNetworkSpawn() {

		// Set up the controller reference for both server and client
		controller = GameObject.FindGameObjectWithTag("GameController");

		UpdateSprite(pieceName.Value.ToString());
		UpdatePosition();
	}

	private void OnPieceNameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue) {
		UpdateSprite(newValue.ToString());
	}
	private void OnPositionChanged(int oldValue, int newValue) {
		UpdatePosition();
	}

	[ServerRpc(RequireOwnership = false)]
	public void RequestMoveServerRpc(int newX, int newY, int oldX, int oldY) {
		// Only server executes this code
		// Update the NetworkVariables (only server can do this)
		xBoard.Value = newX;
		yBoard.Value = newY;

		// Update the game board state
		controller.GetComponent<Game>().SetPositionEmpty(oldX, oldY);
		controller.GetComponent<Game>().SetPosition(gameObject);

		// Check for pawn promotion
		CheckPromotion();
	}

	// Updates in case of new position
	public void UpdatePosition() {
		float x = xBoard.Value;
		float y = yBoard.Value;

		// Adjust the coordinates to fit the board
		x *= 0.66f;
		y *= 0.66f;

		x -= 2.3f;
		y -= 2.19f;

		this.transform.position = new Vector3(x, y, -1.0f);
	}

	[ServerRpc(RequireOwnership = false)]
	private void PromoteToQueenServerRpc() {
		string queenName = player == "white" ? "white_queen" : "black_queen";
		pieceName.Value = new FixedString32Bytes(queenName);
	}

	// Changes the piece (from default or not)
	public void UpdateSprite(string newPiece) {
		// Debug.Log(newPiece); // Test for string conversion
		switch (newPiece.Trim()) {
			case "black_king": this.GetComponent<SpriteRenderer>().sprite = black_king; player = "black";break;
			case "black_queen": this.GetComponent<SpriteRenderer>().sprite = black_queen; player = "black"; break;
			case "black_rook": this.GetComponent<SpriteRenderer>().sprite = black_rook; player = "black"; break;
			case "black_bishop": this.GetComponent<SpriteRenderer>().sprite = black_bishop; player = "black"; break;
			case "black_knight": this.GetComponent<SpriteRenderer>().sprite = black_knight; player = "black"; break;
			case "black_pawn": this.GetComponent<SpriteRenderer>().sprite = black_pawn; player = "black"; break;

			case "white_king": this.GetComponent<SpriteRenderer>().sprite = white_king; player = "white"; break;
			case "white_queen": this.GetComponent<SpriteRenderer>().sprite = white_queen; player = "white"; break;
			case "white_rook": this.GetComponent<SpriteRenderer>().sprite = white_rook; player = "white"; break;
			case "white_bishop": this.GetComponent<SpriteRenderer>().sprite = white_bishop; player = "white"; break;
			case "white_knight": this.GetComponent<SpriteRenderer>().sprite = white_knight; player = "white"; break;
			case "white_pawn": this.GetComponent<SpriteRenderer>().sprite = white_pawn; player = "white"; break;
		}
	}

	

	
}

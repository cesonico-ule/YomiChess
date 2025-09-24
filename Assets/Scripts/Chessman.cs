using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class Chessman : MonoBehaviour
{
	// References
	public GameObject controller;
	public GameObject movePlate;

	// Positions
	private int xBoard = -1;
	private int yBoard = -1;

	//Variable to keep track of black or white player
	private string player;

	//References for all the sprites that the chesspiece can be
	public Sprite black_king, black_queen, black_rook, black_bishop, black_knight, black_pawn, white_king, white_queen, white_rook, white_bishop, white_knight, white_pawn;

	public void Activate() {

		controller = GameObject.FindGameObjectWithTag("GameController");

		
		// Take the instantiated location and adjust the transform
		SetCoordinates();

		switch (this.name) {
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
	
	public void SetCoordinates() {
		float x = xBoard;
		float y = yBoard;

		// Adjust the coordinates to fit the board
		x *= 0.66f;
		y *= 0.66f;

		x -= 2.3f;
		y -= 2.19f;

		this.transform.position = new Vector3(x, y, -1.0f);

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
		switch (this.name) {
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
				PawnMovePlate(xBoard, yBoard - 1);
				break;
			case "white_pawn":
				PawnMovePlate(xBoard, yBoard + 1);
				break;
		}
	}


	// Getters and Setters for the board coordinates

	public int GetXBoard() {
		return xBoard;
	}

	public int GetYBoard() {
		return yBoard;
	}

	public void SetXBoard(int x) {
		xBoard = x;
	}

	public void SetYBoard(int y) {
		yBoard = y;
	}

	// GH post
	public void LineMovePlate(int xIncrement, int yIncrement) {
		Game sc = controller.GetComponent<Game>();

		int x = xBoard + xIncrement;
		int y = yBoard + yIncrement;

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
		PointMovePlate(xBoard + 1, yBoard + 2);
		PointMovePlate(xBoard - 1, yBoard + 2);
		PointMovePlate(xBoard + 2, yBoard + 1);
		PointMovePlate(xBoard + 2, yBoard - 1);
		PointMovePlate(xBoard + 1, yBoard - 2);
		PointMovePlate(xBoard - 1, yBoard - 2);
		PointMovePlate(xBoard - 2, yBoard + 1);
		PointMovePlate(xBoard - 2, yBoard - 1);
	}

	public void SurroundMovePlate() {
		PointMovePlate(xBoard, yBoard + 1);
		PointMovePlate(xBoard, yBoard - 1);
		PointMovePlate(xBoard - 1, yBoard + 0);
		PointMovePlate(xBoard - 1, yBoard - 1);
		PointMovePlate(xBoard - 1, yBoard + 1);
		PointMovePlate(xBoard + 1, yBoard + 0);
		PointMovePlate(xBoard + 1, yBoard - 1);
		PointMovePlate(xBoard + 1, yBoard + 1);
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
			}

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

}

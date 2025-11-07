using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Netcode;



public class MovePlate : MonoBehaviour
{
	public GameObject controller;
	GameObject reference = null;

	int matrixX;
	int matrixY;

	// False = Empty square, True = Threat
	public bool attack = false;


	public void Start() {
		if (attack) {
			// Change to red
			gameObject.GetComponent<SpriteRenderer>().color = new Color(1.0f, 0.0f, 0.0f, 0.5f); ;
		}
	}
	/*
	// OnMouseUp is called when the user has released the mouse button (previous way)
	public void OnMouseUp() {
		controller = GameObject.FindGameObjectWithTag("GameController");

		if (attack)
		{
			GameObject cp = controller.GetComponent<Game>().GetPosition(matrixX, matrixY);

			Destroy(cp);
		}

		controller.GetComponent<Game>().SetPositionEmpty(reference.GetComponent<Chessman>().GetXBoard(), reference.GetComponent<Chessman>().GetYBoard());

		reference.GetComponent<Chessman>().SetXBoard(matrixX);
		reference.GetComponent<Chessman>().SetYBoard(matrixY);
		reference.GetComponent<Chessman>().UpdatePosition();

		controller.GetComponent<Game>().SetPosition(reference);

		reference.GetComponent<Chessman>().DestroyMovePlates();
	}
	*/

	public void OnMouseUp() {
		controller = GameObject.FindGameObjectWithTag("GameController");

		// Check if game is over
		if (controller.GetComponent<Game>().IsGameOver()) {
			return; // Don't allow moves if game is over
		}

		Chessman chessman = reference.GetComponent<Chessman>();
		int oldX = chessman.GetXBoard();
		int oldY = chessman.GetYBoard();

		if (attack) {
			GameObject cp = controller.GetComponent<Game>().GetPosition(matrixX, matrixY);
			if (cp != null) {
				// Request the server to destroy the piece
				NetworkObject netObj = cp.GetComponent<NetworkObject>();
				if (netObj != null && NetworkManager.Singleton.IsServer) {
					controller.GetComponent<Game>().CheckForKingCapture(cp);
					netObj.Despawn();
					Destroy(cp);
				} else if (netObj != null) {
					// Client needs to request destruction via ServerRpc
					controller.GetComponent<Game>().RequestDestroyPieceServerRpc(matrixX, matrixY);
				}
			}
		}

		// Request the move from the server instead of doing it directly (maybe)
		chessman.RequestMoveServerRpc(matrixX, matrixY, oldX, oldY);

		// Switch turn after move is complete (only in classic mode)
		controller.GetComponent<Game>().SwitchTurn();

		chessman.DestroyMovePlates();
	}

	public void SetCoords(int x, int y) {
		matrixX = x;
		matrixY = y;
	}

	public void SetReference(GameObject obj) {
		reference = obj;
	}
	public void GetReference(GameObject obj) {
		reference = obj;
	}

}

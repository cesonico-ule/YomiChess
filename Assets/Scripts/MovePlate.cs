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

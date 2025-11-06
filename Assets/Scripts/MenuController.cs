using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour {

	// Menu Panels
	public GameObject mainMenuPanel;
	public GameObject jugarMenuPanel;
	public GameObject gameCanvas; // The canvas with the game board and pieces (I merged all objects in here just in case)

	// Buttons
	public Button jugarButton;
	public Button salirButton;
	public Button clasicoButton;
	public Button chessKuneDoButton;
	public Button atrasButton;
	public Button menuButtonInGame; // New button to return to menu from game


	void Start() {
		// Set up button listeners
		jugarButton.onClick.AddListener(OnJugarClicked);
		salirButton.onClick.AddListener(OnSalirClicked);
		clasicoButton.onClick.AddListener(OnClasicoClicked);
		chessKuneDoButton.onClick.AddListener(OnChessKuneDoClicked);
		atrasButton.onClick.AddListener(OnAtrasClicked);

		if (menuButtonInGame != null) {
			menuButtonInGame.onClick.AddListener(OnReturnToMenuFromGame);
		}

		// Show main menu at start, hide everything else
		ShowMainMenu();
	}

	private void ShowMainMenu() {
		mainMenuPanel.SetActive(true);
		jugarMenuPanel.SetActive(false);
		if (gameCanvas != null) {
			gameCanvas.SetActive(false);
		}
	}

	private void ShowJugarMenu() {
		mainMenuPanel.SetActive(false);
		jugarMenuPanel.SetActive(true);
		if (gameCanvas != null) {
			gameCanvas.SetActive(false);
		}
	}

	private void ShowGame() {
		mainMenuPanel.SetActive(false);
		jugarMenuPanel.SetActive(false);
		if (gameCanvas != null) {
			gameCanvas.SetActive(true);
		}
	}

	// Button handlers
	private void OnJugarClicked() {
		ShowJugarMenu();
	}

	private void OnSalirClicked() {
		// Exit the game
		#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
		#else
            Application.Quit();
		#endif
	}

	private void OnClasicoClicked() {
		// TODO: Implement classic mode
		Debug.Log("Clásico mode - not implemented yet");
	}

	private void OnChessKuneDoClicked() {
		ShowGame();
		// Optionally, you can tell Game.cs to start the game
		Game.Instance.StartGameSetup();
	}

	private void OnAtrasClicked() {
		ShowMainMenu();
	}

	private void OnReturnToMenuFromGame() {
		// Clean up the game state
		if (Game.Instance != null) {
			Game.Instance.CleanupGame();
		}

		// Disconnect from network
		if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) {
			NetworkManager.Singleton.Shutdown();
		}

		// Return to main menu
		ShowMainMenu();
	}

}
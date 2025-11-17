using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuController : MonoBehaviour {

	public static MenuController Instance; // Singleton so Game.cs can access it

	// Menu Panels
	public GameObject mainMenuPanel;
	public GameObject jugarMenuPanel;
	public GameObject gameCanvas;

	// Optional: Error message panel
	public GameObject errorPanel;
	public TextMeshProUGUI errorText;

	// Buttons
	public Button jugarButton;
	public Button salirButton;
	public Button clasicoButton;
	public Button chessKuneDoButton;
	public Button atrasButton;
	public Button menuButtonInGame;

	void Awake() {
		// Set up singleton
		if (Instance != null && Instance != this) {
			Destroy(gameObject);
		} else {
			Instance = this;
		}
	}

	void Start() {
		// Hide error panel
		if (errorPanel != null) {
			errorPanel.SetActive(false);
		}

		// Set up button listeners
		jugarButton.onClick.AddListener(OnJugarClicked);
		salirButton.onClick.AddListener(OnSalirClicked);
		clasicoButton.onClick.AddListener(OnClasicoClicked);
		chessKuneDoButton.onClick.AddListener(OnChessKuneDoClicked);
		atrasButton.onClick.AddListener(OnAtrasClicked);

		if (menuButtonInGame != null) {
			menuButtonInGame.onClick.AddListener(OnReturnToMenuFromGame);
		}

		ShowMainMenu();
	}

	// Public method that Game.cs can call
	public void ShowMainMenuAfterError(string errorMessage) {
		ShowMainMenu();
		ShowError(errorMessage);
	}

	public void ShowError(string message) {
		Debug.LogError(message);
		if (errorPanel != null && errorText != null) {
			errorText.text = message;
			errorPanel.SetActive(true);
			StartCoroutine(HideErrorAfterDelay(5f));
		}
	}

	private IEnumerator HideErrorAfterDelay(float delay) {
		yield return new WaitForSeconds(delay);
		if (errorPanel != null) {
			errorPanel.SetActive(false);
		}
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
		ShowGame();
		if (Game.Instance != null) {
			Game.Instance.SetGameMode(true); // true = classic mode
			Game.Instance.StartGameSetup();
		}
	}

	private void OnChessKuneDoClicked() {
		ShowGame();
		if (Game.Instance != null) {
			Game.Instance.SetGameMode(false); // false = simultaneous mode
			Game.Instance.StartGameSetup();
		}
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
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level : MonoBehaviour {
	[Header("Spawn")]
	public Transform respawn;
	public float dramaTime;
	public float respawnTime;
	public Bounds levelBounds;

	[Header("Tokens")]
	public LayerMask tokenCollisionMask;
	
	[Header("Prefabs")]
	public Player playerPrefab;
	public Token dropTokenPrefab;

	private CameraController cameraController;
	private Player player;

	private int collectedTokens = 0;
	private List<Token> tokens;
	private List<Token> droppedTokens;

	public void Awake() {
		this.cameraController = Camera.main.GetComponent<CameraController>();

		this.player = GameObject.Instantiate<Player>(this.playerPrefab);

		this.tokens = new List<Token>(GameObject.FindObjectsOfType<Token>());
		this.droppedTokens = new List<Token>();
	}

	public void Start() {
		this.player.Spawn(this.respawn.position);
		this.cameraController.SetTarget(this.player.transform, false);
		this.cameraController.bounds = this.levelBounds;
	}

	public void PlayerDied() {
		DropTokens();

		StartCoroutine(WaitForRespawn());
	}

	public void Pickup(bool spawnNew) {
		this.collectedTokens++;

		if(spawnNew) {
			StartCoroutine(SpawnNewToken());
		}
	}

	private IEnumerator WaitForRespawn() {
		Time.timeScale = 0.025f;
		this.cameraController.StartDrama(this.dramaTime);

		yield return new WaitForSecondsRealtime(this.dramaTime);
		
		Time.timeScale = 1f;
		this.cameraController.StopDrama();

		this.cameraController.SetFocus(false, false);
		this.cameraController.SetTarget(this.respawn, true);

		yield return new WaitForSeconds(this.respawnTime - this.dramaTime);

		this.player.Spawn(this.respawn.position);
		this.cameraController.SetTarget(this.player.transform, false);
	}

	private IEnumerator SpawnNewToken() {
		yield return new WaitForSeconds(5f);

		Token selected = null;
		List<Token> availableTokens = new List<Token>(this.tokens.Count);
		float totalWeights = 0;

		while(selected == null) {
			totalWeights = 0;
			availableTokens.Clear();

			foreach(var token in this.tokens) {
				if(!token.canBeActivated) {
					continue;
				}

				availableTokens.Add(token);
				totalWeights += token.weight;
			}

			if(availableTokens.Count == 0) {
				yield return new WaitForSeconds(0.5f);
				continue;
			}

			float r = Random.value * totalWeights;
			float i = 0;
			foreach(var token in availableTokens) {
				if(r > i && r <= i + token.weight) {
					selected = token;
					break;
				} else {
					i += token.weight;
				}
			}
		}

		selected.Activate();
	}

	private void DropTokens() {
		foreach(var token in this.droppedTokens) {
			if(token == null || token.gameObject == null) {
				continue;
			}

			GameObject.Destroy(token.gameObject); // TODO: pool
		}
		this.droppedTokens.Clear();

		int leftovers = Mathf.CeilToInt(this.collectedTokens / 2f);
		this.collectedTokens = 0;

		for(int i = 0; i < leftovers; i++) {
			Token token = GameObject.Instantiate<Token>(this.dropTokenPrefab, this.player.transform.position, Quaternion.identity); // TODO: pool
			
			Vector2 safePos = FindDropTokenSafeLocation(token);
			token.transform.position = this.player.transform.position + Vector3.up;
			token.MoveTo(safePos);

			this.droppedTokens.Add(token);
		}
	}

	private Vector2 FindDropTokenSafeLocation(Token token) {
		Collider2D coll = token.gameObject.GetComponent<Collider2D>();
		ContactFilter2D filter2D = new ContactFilter2D();
		filter2D.layerMask = this.tokenCollisionMask;
		Collider2D[] results = new Collider2D[8];

		Vector2 startPos = token.transform.position;

		for(int i = 0; i < 64; i++) {
			Vector2 pos = startPos + Random.insideUnitCircle * 6f;
			token.transform.position = pos;

			int c = Physics2D.OverlapCollider(coll, filter2D, results);
			if(c == 0) {
				return pos;
			}
		}

		return startPos;
	}

#if UNITY_EDITOR
    
	public void OnDrawGizmosSelected() {
        Gizmos.color = Color.green;
		Gizmos.DrawWireCube(this.levelBounds.center, this.levelBounds.extents);	
	}

#endif
}

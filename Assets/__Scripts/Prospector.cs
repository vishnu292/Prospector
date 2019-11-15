﻿using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


public class Prospector : MonoBehaviour {

	static public Prospector 	S;

	[Header("Set in Inspector")]
	public TextAsset			deckXML;
	public TextAsset layoutXML;
	public float xOffset = 3;
	public float yOffset = -2.5f;
	public Vector3 layoutCenter;
	public Vector2 fsPosMid = new Vector2(.5f, .9f);
	public Vector2 fsPosRun = new Vector2(.5f, .75f);
	public Vector2 fsPosMid2 = new Vector2(.4f, 1.0f);
	public Vector2 fsPosEnd = new Vector2(.5f, .95f);
	public float reloadDelay = 5f; // changed from 2f to 5f
	public Text gameOverText, roundResultText, highScoreText;

	[Header("Set Dynamically")]
	public Deck					deck;
	public Layout layout;
	public List<CardProspector> drawPile;
	public Transform layoutAnchor;
	public CardProspector target;
	public List<CardProspector> tableau;
	public List<CardProspector> discardPile;
	public FloatingScore fsRun;

	void Awake(){
		S = this;
		SetUpUITexts();
	}

	void SetUpUITexts() {
		GameObject go = GameObject.Find("HighScore");
		if (go != null) {
			highScoreText = go.GetComponent<Text>();
		}
		int highScore = ScoreManager.HIGH_SCORE;
		string hScore = "High Score: " + Utils.AddCommasToNumber(highScore);
		go.GetComponent<Text>().text = hScore;

		go = GameObject.Find("GameOver");
		if (go != null) {
			gameOverText = go.GetComponent<Text>();

		}

		go = GameObject.Find("RoundResult");
		if (go != null) {
			roundResultText = go.GetComponent<Text>();
		}
		ShowResultsUI(false);
	}

	void ShowResultsUI(bool show) {
		gameOverText.gameObject.SetActive(show);
		roundResultText.gameObject.SetActive(show);
	}


	void Start() {
		Scoreboard.S.score = ScoreManager.SCORE;

		deck = GetComponent<Deck> ();
		deck.InitDeck (deckXML.text);
		Deck.Shuffle(ref deck.cards);

		layout = GetComponent<Layout>();
		layout.ReadLayout(layoutXML.text);
		drawPile = ConvertListCardsToListCardProspectors(deck.cards);
		LayoutGame();
	}

	List<CardProspector> ConvertListCardsToListCardProspectors(List<Card> lCD)
	{
		List<CardProspector> lCP = new List<CardProspector>();
		CardProspector tCP;
		foreach (Card tCD in lCD)
		{
			tCP = tCD as CardProspector;
			lCP.Add(tCP);
		}
		return (lCP);
	}

	CardProspector Draw() {
		CardProspector cd = drawPile[0];
		drawPile.RemoveAt(0);
		return (cd);
	}

	void LayoutGame() {
		if (layoutAnchor == null) {
			GameObject  tGO = new GameObject("_LayoutAnchor");
			layoutAnchor = tGO.transform;
			layoutAnchor.transform.position = layoutCenter;

		}
		CardProspector cp;
		foreach (SlotDef tSD in layout.slotDefs) {
			cp = Draw();
			cp.faceUp = tSD.faceUp;
			cp.transform.parent = layoutAnchor;
			cp.transform.localPosition = new Vector3(
				layout.multiplier.x * tSD.x,
				layout.multiplier.y * tSD.y,
				-tSD.layerID
				);
			cp.layoutID = tSD.id;
			cp.slotDef = tSD;
			cp.state = eCardstate.tableau;
			cp.SetSortingLayerName(tSD.layerName);
			tableau.Add(cp);
		}
		foreach (CardProspector tCP in tableau) {
			foreach (int hid in tCP.slotDef.hiddenBy) {
				cp = FindCardByLayoutID(hid);
				tCP.hiddenBy.Add(cp);
			}
		}
		MoveToTarget(Draw());
		UpdateDrawPile();
	}

	CardProspector FindCardByLayoutID(int layoutID) {
		foreach (CardProspector tCP in tableau) {
			if (tCP.layoutID == layoutID) {
				return (tCP);
			}
		}
		return null;
	}

	void SetTableauFaces() {
		foreach (CardProspector cd in tableau) {
			bool faceUp = true;
			foreach (CardProspector cover in cd.hiddenBy) {
				if (cover.state == eCardstate.tableau) {
					faceUp = false;
				}

			}
			cd.faceUp = faceUp;
		}
	}


	void MoveToDiscard(CardProspector cd) {

		cd.state = eCardstate.discard;
		discardPile.Add(cd);
		cd.transform.parent = layoutAnchor;

		cd.transform.localPosition = new Vector3(
			layout.multiplier.x * layout.discardPile.x,
			layout.multiplier.y * layout.discardPile.y,
			-layout.discardPile.layerID+.5f
			);
		cd.faceUp = true;
		cd.SetSortingLayerName(layout.discardPile.layerName);
		cd.SetSortOrder(-100 + discardPile.Count);

	}

	void MoveToTarget(CardProspector cd) {
		if (target != null) MoveToDiscard(target);
		target = cd;
		cd.state = eCardstate.target;
		cd.transform.parent = layoutAnchor;
		cd.transform.localPosition = new Vector3(
			layout.multiplier.x * layout.discardPile.x,
			layout.multiplier.y * layout.discardPile.y,
			-layout.discardPile.layerID
			);
		cd.faceUp = true;
		cd.SetSortingLayerName(layout.discardPile.layerName);
		cd.SetSortOrder(0);
	}

	void UpdateDrawPile() {
		CardProspector cd;
		for (int i = 0; i < drawPile.Count; i++) {
			cd = drawPile[i];
			cd.transform.parent = layoutAnchor;

			Vector2 dpStagger = layout.drawPile.stagger;
			cd.transform.localPosition = new Vector3(
				layout.multiplier.x * (layout.drawPile.x + i * dpStagger.x),
				layout.multiplier.y * (layout.drawPile.y + i * dpStagger.y),
				-layout.drawPile.layerID +.1f * i
				);
			cd.faceUp = false;
			cd.state = eCardstate.drawpile;
			cd.SetSortingLayerName(layout.drawPile.layerName);
			cd.SetSortOrder(-10 * i);
		}
	}

	public void CardClicked(CardProspector cd) {
		switch (cd.state) {
			case eCardstate.target:
				break;
			case eCardstate.drawpile:
				MoveToDiscard(target);
				MoveToTarget(Draw());
				UpdateDrawPile();
				ScoreManager.EVENT(eScoreEvent.draw);
				FloatingScoreHandler(eScoreEvent.draw);
				break;
			case eCardstate.tableau:
				bool validMatch = true;
				if (!cd.faceUp) {
					validMatch = false;
				}
				if (!AdjacentRank(cd, target)) {
					validMatch = false;
				}
				if (!validMatch) return;

				tableau.Remove(cd);
				MoveToTarget(cd);
				SetTableauFaces();
				ScoreManager.EVENT(eScoreEvent.mine);
				FloatingScoreHandler(eScoreEvent.mine);
				break;

		}
		CheckForGameOver();
	}

	void CheckForGameOver() {
		if (tableau.Count == 0) {
			GameOver(true);
			return;
		}
		if (drawPile.Count > 0) {
			return;
		}
		foreach (CardProspector cd in tableau) {
			if (AdjacentRank(cd, target)) { return; }
		}

		GameOver(false);
	}
	void GameOver(bool won) {
		int score = ScoreManager.SCORE;
		if (fsRun != null) score += fsRun.score;
		if (won)
		{
			gameOverText.text = "Round Over";
			roundResultText.text = "You won this round! \nRound Score: " + score;
			ShowResultsUI(true);
			//print("GameOver, you won! :)");
			ScoreManager.EVENT(eScoreEvent.gameWin);
			FloatingScoreHandler(eScoreEvent.gameWin);
		}
		else {
			gameOverText.text = "Game Over";
			if (ScoreManager.HIGH_SCORE <= score)
			{
				string str = "You got the high score! \nHigh score: " + score;
				roundResultText.text = str;
			}
			else {
				roundResultText.text = "Your final score was: " + score;
			}
			ShowResultsUI(true);
			//print("GameOver, you lost! :(");
			ScoreManager.EVENT(eScoreEvent.gameLoss);
			FloatingScoreHandler(eScoreEvent.gameLoss);

		}
		//SceneManager.LoadScene("_Scene_6"); //most up to date scene
		Invoke("ReloadLevel", reloadDelay);
	}

	void ReloadLevel() {
		SceneManager.LoadScene("_Scene_6");
	}


	public bool AdjacentRank(CardProspector c0, CardProspector c1) {
		if (!c0.faceUp || !c1.faceUp) return false;
		if (Mathf.Abs(c0.rank - c1.rank) == 1) {
			return (true);
		}
		if (c0.rank == 1 && c1.rank == 13) return (true);
		if (c0.rank == 13 && c1.rank == 1) return (true);

		return (false);
	}

	void FloatingScoreHandler(eScoreEvent evt) {
		List<Vector2> fsPts;
		switch (evt) {

			case eScoreEvent.draw:
			case eScoreEvent.gameWin:
			case eScoreEvent.gameLoss:
				if (fsRun != null) {
					fsPts = new List<Vector2>();
					fsPts.Add(fsPosRun);
					fsPts.Add(fsPosMid2);
					fsPts.Add(fsPosEnd);
					fsRun.reportFinishTo = Scoreboard.S.gameObject;
					fsRun.Init(fsPts, 0, 1);
					fsRun.fontSizes = new List<float>(new float[] { 28, 36, 4 });
					fsRun = null;


				}
				break;
			case eScoreEvent.mine:
				FloatingScore fs;
				Vector2 p0 = Input.mousePosition;
				p0.x /= Screen.width;
				p0.y /= Screen.height;
				fsPts = new List<Vector2>();
				fsPts.Add(p0);
				fsPts.Add(fsPosMid);
				fsPts.Add(fsPosRun);
				fs = Scoreboard.S.CreateFloatingScore(ScoreManager.CHAIN, fsPts);
				fs.fontSizes = new List<float>(new float[] { 4, 50, 28 });
				if (fsRun == null)
				{
					fsRun = fs;
					fsRun.reportFinishTo = null;
				}
				else {
					fs.reportFinishTo = fsRun.gameObject;
				}
				break;


		}
	}

}

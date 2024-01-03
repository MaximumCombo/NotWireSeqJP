using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NotVanillaModulesLib;
using UnityEngine;
using Random = UnityEngine.Random;

public class NotWireSequence : NotVanillaModule<NotWireSequenceConnector> {
	private static readonly string[][] paragraphs = new[] {
		new[] { "それぞれ", "の", "ワイヤ", "の", "色", "は", "この", "マニュアル",
			"の", "段落", "を", "表し", "右", "側", "に", "書か", "れて",
			"いる", "各", "番号", "は", "その", "段落", "に", "ある", "その",
			"位置", "の", "単語", "に", "対応", "し", "ゼロ", "から", "始まる", "番号", "が", "付け", "られて", "いる"},
		new[] { "各", "パネル", "に", "は", "必ず", "三", "本", "の", "ワイヤ", "が", "ついて", "いる", "ワイヤ", "で",
			"繋が", "れた", "文字", "と", "数字", "の", "ペア", "ごと", "に", "ワイヤ", "の", "色", "で", "示さ", "れた",
			"段落", "に", "その", "数", "だけ", "単語", "を", "割り出し", "その", "単語", "に", "ワイヤ", "で", "繋が",
			"れた", "文字", "と", "同じ", "文字", "が", "含ま", "れて", "いる", "場合", "それ", "を", "切る", "そう",
			"で", "ない", "場合", "そのまま", "に", "して", "おく", "この", "手順", "を", "十二", "本", "すべて", "の",
			"ワイヤ", "に", "ついて", "繰り返す"},
		new[] { "切る", "べき", "ワイヤ", "を", "すべて", "切った", "後", "下", "の", "ボタン", "を", "押して", "次の",
			"パネル", "に", "進む", "四", "つ", "の", "パネル", "すべて", "が", "切る", "べき", "ワイヤ", "を", "切った",
			"後", "モジュール", "は", "解除", "さ", "れる", "間違った", "ワイヤ", "を", "切る", "と",
			"ミス", "が", "記録", "さ", "れる"},
		new[] { "それぞれ", "の", "段落", "の", "最初の", "単語", "は", "太字", "で", "書か", "れ", "最後", "の", "単語",
			"に", "は", "下線", "が", "引か", "れて", "いる", "これ", "は", "この", "マニュアル",
			"の", "本文", "の", "境界", "を", "明確に", "する", "ため", "の", "もの", "である" },
		new[] { "段落", "から", "単語", "を", "探し出す", "に", "は", "最初の", "単語", "を", "ゼロ", "と", "して",
			"その", "数", "に", "到達", "する", "まで", "数える", "指定", "さ", "れた", "数字", "が", "その",
			"段落", "の", "単語", "の", "数", "より", "大きい", "場合", "段落", "の", "最後に", "到達", "した",
			"後", "その", "段落", "の", "最初の", "単語", "に", "戻る" }
	};

	private readonly string[] words = new string[12];
	private readonly bool[] shouldCut = new bool[12];

	public override void Start() {
		base.Start();
		this.Connector.KMBombModule.OnActivate = this.KMBombModule_OnActivate;
		this.Connector.WireCut += this.Connector_WireCut;
		this.Connector.UpPressed += this.Connector_UpPressed;
		this.Connector.DownPressed += this.Connector_DownPressed;
		this.RandomiseWires();
		this.Connector.InitialisePages();
	}

	private void KMBombModule_OnActivate() {
		this.Connector.MoveToPage(0);
	}

	private void RandomiseWires() {
		for (int i = 0; i < this.Connector.Pages.Count; ++i) {
			var page = this.Connector.Pages[i];
			var toIndices = new[] { 0, 1, 2 };
			toIndices.Shuffle();
			for (int j = 0; j < page.Wires.Count; ++j) {
				var wire = page.Wires[j];
				wire.To = toIndices[j];
				wire.Colour = (WireSequenceColour) Random.Range(0, 5);
				int index = Random.Range(0, 50);
				page.Wires[wire.To].Number = index.ToString();

				var paragraph = paragraphs[(int) wire.Colour];
				var word = paragraph[index % paragraph.Length];
				this.words[i * 3 + j] = word.ToLowerInvariant();
				// 50% chance to guarantee that the wire should be cut.
				if (Random.Range(0, 2) == 0) {
					wire.Letter = char.ToUpper(word.PickRandom()).ToString();
					this.shouldCut[i * 3 + j] = true;
				} else {
					try
                    {
						string[] color = NotWireSequence.paragraphs[Random.Range(0, 5)];
						string word2 = color[Random.Range(0, color.Length)];
						char moji = word2.PickRandom();
						wire.Letter = moji.ToString();
						this.shouldCut[i * 3 + j] = word.ContainsIgnoreCase(wire.Letter);
					}
					catch (Exception e)
                    {
						Debug.LogError(e);
                    }
				}

				this.Log("Panel {0} wire {1}: '{2}' in the {3} paragraph, word {4} ('{5}'): {6}.",
					i + 1, j + 1, wire.Letter, wire.Colour.ToString().ToLowerInvariant(), index, this.words[i * 3 + j], this.shouldCut[i * 3 + j] ? "cut" : "do not cut");
			}
		}
	}

	private void Connector_UpPressed(object sender, EventArgs e) {
		if (this.Solved || this.Connector.Animating || this.Connector.CurrentPage == 0) return;
		this.Connector.MoveToPage(this.Connector.CurrentPage - 1);
	}

	private void Connector_DownPressed(object sender, EventArgs e) {
		if (this.Solved || this.Connector.Animating) return;

		if (this.Connector.CurrentPage >= this.Connector.Stage) {
			for (int i = 0; i < 3; ++i) {
				if (this.shouldCut[this.Connector.CurrentPage * 3 + i] && !this.Connector.Pages[this.Connector.CurrentPage].Wires[i].Cut) {
					this.Log("Attempted to move past panel {0} when wire {1} still needs to be cut.", this.Connector.CurrentPage + 1, i + 1);
					this.Connector.KMBombModule.HandleStrike();
					return;
				}
			}
			++this.Connector.Stage;
			if (this.Connector.Stage >= 4) this.Disarm();
		}
		this.Connector.MoveToPage(this.Connector.CurrentPage + 1);
	}

	private void Connector_WireCut(object sender, WireCutEventArgs e) {
		if (!this.shouldCut[e.WireIndex]) {
			this.Log("Wire {0} on panel {1} was incorrectly cut.", e.WireIndex % 3 + 1, e.WireIndex / 3 + 1);
			this.Connector.KMBombModule.HandleStrike();
		}
	}
		// Twitch Plays support
	public static readonly string TwitchHelpMessage
		= "!{0} cut 1 - cuts the wire at the first letter on the current panel | !{0} cut E - cuts the wire with letter E | !{0} down | !{0} up | | !{0} d | !{0} u | !{0} cut 1 2 3 d";
	public IEnumerator ProcessTwitchCommand(string command) {
		if (this.TwitchColourblindModeCommand(command)) { yield return null; yield break; }

		var tokens = command.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
		if (tokens.Length == 0) yield break;

		switch (tokens[0].ToLowerInvariant()) {
			case "down": case "d":
				yield return "strikemessage pressing down";
				this.Connector.TwitchMoveDown();
				yield return new WaitForSeconds(1.5f);
				break;
			case "up": case "u":
				yield return null;
				this.Connector.TwitchMoveUp();
				yield return new WaitForSeconds(1.5f);
				break;
			case "cut": case "c":
				bool down = false; var wireIndices = new List<int>();
				for (int i = 1; i < tokens.Length; ++i) {
					var token = tokens[i];
					if (token.Length == 1) {
						if (char.IsDigit(token[0])) {
							if (token[0] < '1' || token[0] > '3') yield break;
							wireIndices.Add(token[0] - '1');
						} else if ((token[0] == 'd' || token[0] == 'D') && i == tokens.Length - 1 && i > 1) {
							down = true;
						} else {
							var index = this.Connector.Pages[this.Connector.CurrentPage].Wires.IndexOf(w => w.Letter[0] == char.ToUpperInvariant(token[0]));
							if (index < 0) {
								yield return string.Format("sendtochaterror Letter {0} was not found on this panel.", char.ToUpperInvariant(token[0]));
								yield break;
							}
							if (this.Connector.Pages[this.Connector.CurrentPage].Wires.Skip(index + 1).Any(w => w.Letter[0] == char.ToUpperInvariant(token[0]))) {
								yield return string.Format("sendtochaterror Letter {0} appears multiple times on this panel.", char.ToUpperInvariant(token[0]));
								yield break;
							}
							wireIndices.Add(index);
						}
					} else if (token.EqualsIgnoreCase("down") && i == tokens.Length - 1 && i > 1)
						down = true;
					else
						yield break;
				}
				if (wireIndices.Count == 0) yield break;
				foreach (var index in wireIndices) {
					yield return string.Format("strikemessage cutting wire {0}", index + 1);
					this.Connector.TwitchCut(index);
					yield return new WaitForSeconds(0.1f);
				}
				if (down) {
					yield return "strikemessage pressing down";
					this.Connector.TwitchMoveDown();
					yield return new WaitForSeconds(1.5f);
				}
				break;
		}

	}

	public IEnumerator TwitchHandleForcedSolve() {
		while (!this.Solved) {
			for (int i = 0; i < 3; ++i) {
				if (this.shouldCut[this.Connector.CurrentPage * 3 + i] && !this.Connector.Pages[this.Connector.CurrentPage].Wires[i].Cut) {
					this.Connector.TwitchCut(i);
					yield return new WaitForSeconds(0.1f);
				}
			}
			this.Connector.TwitchMoveDown();
			yield return new WaitWhile(() => this.Connector.Animating);
		}
	}
}

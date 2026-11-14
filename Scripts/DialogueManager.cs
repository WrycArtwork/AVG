using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AVG.Scripts;

public partial class DialogueManager : Control, ISaveableContext
{
	private readonly Dictionary<string, int> _gameConditions = new();
	private readonly Dictionary<string, string> _conditionLibrary = new();
	private readonly Dictionary<string, string> _chapterLibrary = new();
	
	public class DialogueData
	{
		public string Id {get; set; }			// 識別碼(除錯/存檔用)
		public string Command { get; set; }		//特殊指令
		public string Name { get; set; }		//說話者名字
		public string Content { get; set; }		//文本內容
		public string Background { get; set; }		//場景
		public string Cg { get; set; }			//CG插畫
		public string Transition { get; set; }	//轉場特效
		public string Char01 { get; set; }	//角色一立繪檔名
		public string Char02 { get; set; }	//角色二立繪檔名
		public string Char03 { get; set; }	//角色三立繪檔名
		public string Char04 { get; set; }	//角色四立繪檔名
		public string Char05 { get; set; }	//角色五立繪檔名
		public string Bgm { get; set; }			//背景音樂
		public string Voice { get; set; }		//角色配音
		public string Sfx  { get; set; }		//音效
		public string Option01 { get; set; }	//選項一
		public string Option02 { get; set; }	//選項二
		public string Option03 { get; set; }	//選項三
		public string Option04 { get; set; }	//選項四
		public string Option05 { get; set; }	//選項五
	}
	
	[ExportGroup("Dialogues")]
	[Export] public Label NameLabel;
	[Export] public RichTextLabel ContentLabel;
	[Export] public TextureRect SceneNode;
	[Export] public TextureRect CgNode;
	[Export] public TextureRect Char01Node;
	[Export] public TextureRect Char02Node;
	[Export] public TextureRect Char03Node;
	[Export] public TextureRect Char04Node;
	[Export] public TextureRect Char05Node;
	[Export] public AudioStreamPlayer BgmPlayerA;
	[Export] public AudioStreamPlayer BgmPlayerB;
	[Export] public AudioStreamPlayer VoicePlayer;
	[Export] public AudioStreamPlayer SfxPlayer;
	[Export] public VBoxContainer OptionsContainer;
	[Export] public PanelContainer DialoguePanel;
	[Export] public Control NextIndicator;
	private Tween _indicatorTween;
	private Vector2 _indicatorOrigPos;
	private Tween _indicatorFadeTween;
	
	[ExportGroup("Quick Buttons")]
	[Export] public Button AutoButton;
	[Export] public Button SkipButton;
	[Export] public Button SaveButton;
	[Export] public Button LoadButton;
	[Export] public Button LogButton;
	[Export] public Button ConfigButton;
	
	[ExportGroup("Quick Panels")]
	[Export] public LogPanel LogPanelUI;

	[ExportGroup("DialogueConfigs")]
	[Export] public float TypeSpeed = 0.05f;
	[Export] public double AutoDelayTime = 1.5;
	[Export] public float SkipDelayTime = 0.04f;
	private const double VoiceBufferDelay = 0.1;

	private List<DialogueData> _dialoguesList = [];
	private readonly Dictionary<string, DialogueData> _dialoguesDict = new();
	//Option
	private string _currentNodeId = "";
	private bool _isWaitingForOption = false;
	//Dialogue
	private Tween _typeTween;
	private int _currentIndex = -1;
	private string _currentSceneName = "";
	private string _currentCgName = "";
	//Character
	private string _currentCharacterLeft = "";
	private string _currentCharacterRight = "";
	private string _currentCharacterMiddle = "";
	//BGM
	private string _currentBgmName = "";
	private Tween _bgmFadeOutTween;
	private Tween _bgmFadeInTween;
	private AudioStreamPlayer ActivePlayer => (BgmPlayerA !=null && BgmPlayerA.Playing) ? BgmPlayerA : BgmPlayerB;
	private AudioStreamPlayer InactivePlayer => (ActivePlayer == BgmPlayerA) ? BgmPlayerB :  BgmPlayerA;
	//Wait
	private bool _isWaiting = false;
	private bool _isSkipping = false;
	//Panel
	private bool _isMenuOpen = false;
	//Auto
	private bool _isAuto = false;
	private double _autoWaitTimer = 0.0;
	private double _voiceBufferTimer = 0.0;
	private bool _hasPlayedVoiceCurrentLine = false;
	
	//__Custom Settings Function__
	private void StartIndicatorTween()
	{
		_indicatorTween = CreateTween().SetLoops();
		
		//Jump
		_indicatorTween.TweenProperty(NextIndicator, "position:y", _indicatorOrigPos.Y + 6.0f, 0.4f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		_indicatorTween.TweenProperty(NextIndicator, "position:y", _indicatorOrigPos.Y, 0.4f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
	}
	
	//__Code__
	public override void _Ready()
	{
		SaveLoadManager.Instance.RegisterContext(this);
		
		if (SaveLoadManager.Instance != null)
		{
			SaveLoadManager.Instance.OnPanelClosed += () =>
			{
				_isMenuOpen = false;
			};
		}
		BindQuickButtons();
		LoadJson();

		if (SaveLoadManager.PendingLoadData != null)
		{
			RestoreFromSaveData(SaveLoadManager.PendingLoadData);
			SaveLoadManager.PendingLoadData = null;
		}
		else
		{
			NextLine();
		}

		SetupAudioBuses();
		SetupSfxPlayer();
		
		LogItem.PlayVoice += PlayReplayVoice;
		
		if (ConfigManager.Instance != null)
		{
			ApplyConfig(ConfigManager.Instance.CurrentConfig);
			ConfigManager.Instance.OnConfigChanged += ApplyConfig;
			ConfigManager.Instance.ConfigPanelClose += () => { _isMenuOpen = false; };
		}
		
		StartIndicatorTween();
	}

	public override async void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
		{
			if (GetTree()?.CurrentScene is DialogueManager dialogue)
			{
				await dialogue.CreateAutoSaveDataAsync();
			}
			GetTree().Quit();
		}
	}

	public override void _ExitTree()
	{
		SaveLoadManager.Instance?.UnregisterContext(this);
		LogItem.PlayVoice -= PlayReplayVoice;

		if (ConfigManager.Instance != null)
		{
			ConfigManager.Instance.OnConfigChanged -= ApplyConfig;
		}
	}

	public override void _Process(double delta)
	{
		if(!_isAuto || _isWaitingForOption || _isWaiting || _isMenuOpen) return;
		if (_typeTween != null && _typeTween.IsRunning()) return;

		bool isVoicePlaying = _hasPlayedVoiceCurrentLine && VoicePlayer != null && VoicePlayer.Playing;

		_autoWaitTimer += delta;

		if (isVoicePlaying)
		{
			_voiceBufferTimer = 0.0;
			return;
		}

		if (_hasPlayedVoiceCurrentLine)
		{
			_voiceBufferTimer += delta;
			if (_voiceBufferTimer < VoiceBufferDelay) return;
		}

		if (_autoWaitTimer >= AutoDelayTime)
		{
			_autoWaitTimer = 0.0;
			_voiceBufferTimer = 0.0;
			_hasPlayedVoiceCurrentLine = false;
			AdvanceDialogue();
		}
	}

	private void BindQuickButtons()
	{
		if (SaveButton != null) SaveButton.Pressed += OpenSavePanel;
		if (LoadButton != null) LoadButton.Pressed += OpenLoadPanel;

		if (AutoButton != null)
		{
			AutoButton.Pressed += () =>
			{
				_isAuto = !_isAuto;
				_isSkipping = false;
				_autoWaitTimer = 0.0;
			};
		}

		if (SkipButton != null)
		{
			SkipButton.Pressed += () =>
			{
				var currentLine = (_currentIndex >= 0 && _currentIndex < _dialoguesList.Count) ? _dialoguesList[_currentIndex] : null;

				if (currentLine != null && !ReadHistory.IsRead(currentLine.Id))
				{
					_isSkipping = false;
					return;
				}
				
				_isSkipping = !_isSkipping;
				_isAuto = false;

				if (_isSkipping)
				{
					if (_typeTween != null && _typeTween.IsRunning())
					{
						_typeTween.Kill();
						ContentLabel.VisibleRatio = 1.0f;
						CallDeferred(nameof(AdvanceDialogue));
					}
					else
					{
						AdvanceDialogue();
					}
				}
			};
		}

		if (LogButton != null)
		{
			LogButton.Pressed += () =>
			{
				if (LogPanelUI != null)
				{
					_isMenuOpen = true;
					_isAuto = false;
					_isSkipping = false;
					LogPanelUI.OpenPanel();
				}
			};
		}

		if (LogPanelUI != null)
		{
			LogPanelUI.OnPanelClosed += () =>
			{
				_isMenuOpen = false;
			};
		}

		if (ConfigButton != null)
		{
			ConfigButton.Pressed += () =>
			{
				_isMenuOpen = true;
				_isAuto = false;
				_isSkipping = false;
				ConfigManager.Instance.OpenConfigPanel();
			};
		}
	}

	public void OpenSavePanel()
	{
		_isMenuOpen = true;
		_isAuto = false;
		_isSkipping = false;
		SaveLoadManager.Instance.OpenSaveMenu();
	}

	public void OpenLoadPanel()
	{
		_isMenuOpen = true;
		_isAuto = false;
		_isSkipping = false;
		SaveLoadManager.Instance.OpenLoadMenu();
	}
	
	private void LoadJson()
	{
		const string conditionPath =  GamePaths.ConditionsDataDir;
		if (!FileAccess.FileExists(conditionPath)) return;
		
		using var condFile = FileAccess.Open(conditionPath, FileAccess.ModeFlags.Read);
		var condJsonText = condFile.GetAsText();
		var condDict = JsonSerializer.Deserialize<Dictionary<string, string>>(condJsonText);
		if (condDict != null)
		{
			_conditionLibrary.Clear();
			foreach (var cond in condDict)
			{
				_conditionLibrary[cond.Key] = cond.Value;
			}
		}
		
		const string chapterPath =  GamePaths.ChaptersMappingData;
		if (FileAccess.FileExists(chapterPath))
		{
			using var chapterFile = FileAccess.Open(chapterPath, FileAccess.ModeFlags.Read);
			var chapterDict = JsonSerializer.Deserialize<Dictionary<string, string>>(chapterFile.GetAsText());
			if (chapterDict != null)
			{
				_chapterLibrary.Clear();
				foreach (var kv in chapterDict)
				{
					_chapterLibrary[kv.Key] = kv.Value;
				}
			}
		}
		
		string currentLanguage = ConfigManager.Instance?.CurrentConfig.Language ?? "zh_TW";
		string dialoguePath = GamePaths.DialogueData(currentLanguage);
		if (!FileAccess.FileExists(dialoguePath))
		{
			dialoguePath = GamePaths.DialogueData("zh_TW");
		}

		using var diaFile = FileAccess.Open(dialoguePath, FileAccess.ModeFlags.Read);
		var jsonText = diaFile.GetAsText();
		
		var options = new JsonSerializerOptions {PropertyNameCaseInsensitive = true};
		_dialoguesList = JsonSerializer.Deserialize<List<DialogueData>>(jsonText, options);
		
		_dialoguesDict.Clear();
		foreach (var line in _dialoguesList.Where(line => !string.IsNullOrEmpty(line.Id)))
		{
			_dialoguesDict[line.Id] = line;
		}
	}
	
	private void AdvanceDialogue()
	{
		if (!_isMenuOpen)
		{
			if (_currentIndex >= 0 && _currentIndex < _dialoguesList.Count)
			{
				var currentLine = _dialoguesList[_currentIndex];
				if (PostInputCommand(currentLine)) return;
			}
       
			NextLine();
		}
	}
	
	private void NextLine()
	{
		_currentIndex++;
		if (_currentIndex >= _dialoguesList.Count)
		{
			GD.Print("End");
			_isSkipping = false;
			return;
		}
		
		var line =  _dialoguesList[_currentIndex];

		if (_isSkipping && !ReadHistory.IsRead(line.Id))
		{
			_isSkipping = false;
		}
		
		ExecutePerformance(line);
	}
	// Performance Core 
	private async void ExecutePerformance(DialogueData line)
	{
		// Reset Timer
		_autoWaitTimer = 0.0;
		_voiceBufferTimer = 0.0;
		
		_hasPlayedVoiceCurrentLine = !string.IsNullOrEmpty(line.Voice);
		
		// Update
		UpdateImage(line.Background, ref _currentSceneName, SceneNode, GamePaths.BackgroundDir);
		UpdateImage(line.Cg, ref _currentCgName, CgNode, GamePaths.CgDir);
		
		UpdateImage(line.Char01, ref _currentCharacterLeft, Char01Node, GamePaths.CharacterDir);
		UpdateImage(line.Char02, ref _currentCharacterRight, Char02Node, GamePaths.CharacterDir);
		UpdateImage(line.Char03, ref _currentCharacterMiddle, Char03Node, GamePaths.CharacterDir);
		UpdateImage(line.Char04, ref _currentCharacterMiddle, Char04Node, GamePaths.CharacterDir);
		UpdateImage(line.Char05, ref _currentCharacterMiddle, Char05Node, GamePaths.CharacterDir);

		if (!_isSkipping)
		{
			UpdateBgm(line.Bgm, 7.0f);
			UpdateSfx(line.Sfx);
			UpdateVoice(line.Voice);
		}
		
		// Mark as read history for skip. 
		ReadHistory.MarkAsRead(line.Id);
		
		// Unlock Gallery
		if (!string.IsNullOrEmpty(line.Cg))
		{
			GalleryManager.Instance?.UnlockCg(line.Cg);
		}

		if (!string.IsNullOrEmpty(line.Background))
		{
			GalleryManager.Instance?.UnlockCg(line.Background);
		}

		if (!string.IsNullOrEmpty(line.Bgm))
		{
			GalleryManager.Instance?.UnlockCg(line.Bgm);
		}
		
		// If in cutscene or choice, hide the dialogue panel
		if (string.IsNullOrEmpty(line.Name) && string.IsNullOrEmpty(line.Content))
		{
			DialoguePanel.Visible = false;
			CheckAndCreateOptions(line);
			
			await PreInputCommand(line);

			if (!_isWaitingForOption)
			{
				if (_isSkipping && _isMenuOpen)
				{
					if (SkipDelayTime > 0)
						await ToSignal(GetTree().CreateTimer(SkipDelayTime), SceneTreeTimer.SignalName.Timeout);
					if (_isSkipping && !_isMenuOpen && !_isWaitingForOption)
					{
						AdvanceDialogue();
					}
				}
			}
			return;
		}
		
		// Display dialogue panel
		DialoguePanel.Visible = true;
		NameLabel.Text = line.Name;
		ContentLabel.Text = line.Content;
		
		// Stop and hide NextIndicator
		if (NextIndicator != null)
		{
			_indicatorFadeTween?.Kill();
			_indicatorFadeTween = CreateTween();
			_indicatorFadeTween.TweenProperty(NextIndicator, "modulate:a", 0.0f, 0.1f);
		}

		string voicePath = GamePaths.GetValidAudio(GamePaths.VoiceDir, line.Voice);
		LogPanelUI?.AddLogItem(line.Name, line.Content, voicePath);
		
		// Reset and start typewriter animation
		ContentLabel.VisibleRatio = 0;
		if (_typeTween != null && _typeTween.IsRunning()) _typeTween.Kill();
		
		float duration = _isSkipping ? 0.0f : (line.Content.Length * TypeSpeed);
		_typeTween = CreateTween();
		_typeTween.TweenProperty(ContentLabel, "visible_ratio", 1.0f, duration);
		
		_typeTween.Finished += async () =>
		{
			CheckAndCreateOptions(line); //Create Option
			
			// Show NextIndicator
			if (!_isWaitingForOption && NextIndicator != null)
			{
				_indicatorFadeTween?.Kill();
				_indicatorFadeTween = CreateTween();
				_indicatorFadeTween.TweenProperty(NextIndicator, "modulate:a", 1.0f, 0.1f);
			}
			
			if (_isSkipping && !_isWaitingForOption && !_isMenuOpen)
			{
				if (SkipDelayTime > 0)
				{
					await ToSignal(GetTree().CreateTimer(SkipDelayTime), SceneTreeTimer.SignalName.Timeout);
					if (!_isSkipping || _isWaitingForOption || _isMenuOpen) return;
				}
				AdvanceDialogue();
			}
		}; 
		// Command execute before click
		await PreInputCommand(line);
	}
	
	public override void _Input(InputEvent @event)
	{
		if (_isMenuOpen) return;

		if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp })
		{
			LogButton?.EmitSignal(Button.SignalName.Pressed);
			return;
		}
		
		if (_isWaitingForOption || _isWaiting) return;

		if (!@event.IsActionPressed("ui_accept") &&
		    @event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
		
		var hoveredControl = GetViewport().GuiGetHoveredControl();
		if (hoveredControl is Button)
		{
			return;
		}
		
		if (@event is InputEventMouseButton)
		{
			_isAuto = false;
		}
		
		if (_typeTween != null && _typeTween.IsRunning())
		{
			_typeTween.Kill();
			ContentLabel.VisibleRatio = 1.0f;
			return;
		}
		
		AdvanceDialogue();
	}
	private async Task<bool> PreInputCommand(DialogueData line)
	{
		if (string.IsNullOrEmpty(line.Command)) return false;
		
		var subCommands = line.Command.Split(';');

		foreach (var subCmd in subCommands)
		{
			var rawCmd = subCmd.Trim();
			if (string.IsNullOrEmpty(rawCmd)) continue;

			var parts = rawCmd.Split(':');
			var type = parts[0];

			if (type == "Wait")
			{
				if (_isSkipping) continue;
				
				var second = parts.Length > 1 ? parts[1].Trim() : "1.0";
				if (float.TryParse(second, CultureInfo.InvariantCulture, out var seconds))
				{
					_isWaiting = true;
					
					await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);

					_isWaiting = false;
				}
			}
		}

		return false;
	}
	private bool PostInputCommand(DialogueData line)
	{
		if (string.IsNullOrEmpty(line.Command)) return false;
		
		var subCommands = line.Command.Split(';');

		foreach (var subCmd in subCommands)
		{
			var rawCmd = subCmd.Trim();
			if (string.IsNullOrEmpty(rawCmd)) continue;
					
			var parts = rawCmd.Split(':');
			var type = parts[0].Trim();

			if (type == "GoTo")
			{
				var target = parts.Length > 1 ? parts[1].Trim() : "";
				var condition = parts.Length > 2 ? parts[2].Trim() : "";

				if (GoToNode(target, condition))
				{
					return true;
				}
			}
			//Another Command...
		}
		return false;
	}
	private bool GoToNode(string nodeId, string conditionId = "")
	{
		if (!EvaluateCondition(conditionId)) return false;
    
		if (string.IsNullOrEmpty(nodeId) || !_dialoguesDict.TryGetValue(nodeId, out var targetNode)) 
			return false;
    
		_currentIndex = _dialoguesList.IndexOf(targetNode);
		ExecutePerformance(targetNode);
		return true;
	}
	private void CheckAndCreateOptions(DialogueData line)
	{
		foreach (var child in OptionsContainer.GetChildren())
		{
			child.QueueFree();
		}

		List<string> rawOption = [];
		if (!string.IsNullOrEmpty(line.Option01)) rawOption.Add(line.Option01);
		if (!string.IsNullOrEmpty(line.Option02)) rawOption.Add(line.Option02);
		if (!string.IsNullOrEmpty(line.Option03)) rawOption.Add(line.Option03);
		if (!string.IsNullOrEmpty(line.Option04)) rawOption.Add(line.Option04);
		if (!string.IsNullOrEmpty(line.Option05)) rawOption.Add(line.Option05);

		if (rawOption.Count == 0)
		{
			if (OptionsContainer != null) OptionsContainer.Visible = false;
			_isWaitingForOption = false;
			return;
		}
		
		_isWaitingForOption = true;
		OptionsContainer.Visible = true;
		DialoguePanel.Visible = false;

		foreach (var option in rawOption)
		{
			var parts = option.Split(':');

			var buttonText = parts.Length > 0 ? parts[0] : "";
			var targetNodeId = parts.Length > 1 ? parts[1] : "";
			var conditionId =parts.Length > 2 ? parts[2] : "";
			var stateChange =parts.Length > 3 ? parts[3] : "";
			
			if (!EvaluateCondition(conditionId)) continue;

			var optionButton = new Button();
			optionButton.Text = buttonText;

			optionButton.Pressed += () =>
			{
				OptionsContainer.Visible = false;
				_isWaitingForOption = false;
				_isSkipping = false;
				
				LogPanelUI?.AddOptionsItem(buttonText);
				ApplyStateChange(stateChange);
				CreateAutoSaveData();
				
				if (!string.IsNullOrEmpty(targetNodeId))
				{
					if (GoToNode(targetNodeId)) return;
				}
				
				_currentIndex++;
				if (_currentIndex < _dialoguesList.Count)
				{
					ExecutePerformance(_dialoguesList[_currentIndex]);
				}
			};
			
			OptionsContainer.AddChild(optionButton);
		}
	}
	private void UpdateImage(string newImageName, ref string currentImageName, TextureRect imageNode, string targetDir)
	{
		if (imageNode == null) return;
		if (newImageName == currentImageName) return;
		currentImageName = newImageName;
		
		if (string.IsNullOrEmpty(currentImageName))
		{
			imageNode.Texture = null;
			imageNode.Modulate = new Color(1, 1, 1, 1);
		}
		else
		{
			var filePath = GamePaths.GetValidImage(targetDir, newImageName);
			if (string.IsNullOrEmpty(filePath)) return;
			var newtexture = GD.Load<Texture2D>(filePath);
			imageNode.Texture = newtexture;
			imageNode.Modulate = new Color(1, 1, 1, 1);
			var tween = CreateTween();
			tween.TweenProperty(imageNode, "modulate:a", 1.0f, 0.0f);
		}
	}
	
	private void SetupAudioBuses()
	{
		if (BgmPlayerA != null) BgmPlayerA.Bus = "Music";
		if (BgmPlayerB != null) BgmPlayerB.Bus = "Music";
		if (VoicePlayer != null) VoicePlayer.Bus = "Voice";
		if (SfxPlayer != null) SfxPlayer.Bus = "Sfx";
	}

	private void SetupSfxPlayer()
	{
		if (SfxPlayer == null) return;

		if (SfxPlayer.Stream is not AudioStreamPolyphonic)
		{
			var polyStream = new AudioStreamPolyphonic {Polyphony = 16};
		}

		if (!SfxPlayer.Playing)
		{
			SfxPlayer.Play();
		}
	}
	
	private void UpdateBgm(string newBgmName, float fadeDuration)
	{
		if (BgmPlayerA == null || BgmPlayerB == null) return;
		
		if (newBgmName == _currentBgmName) return;
		_currentBgmName = newBgmName;

		_bgmFadeOutTween?.Kill();
		_bgmFadeInTween?.Kill();

		if (string.IsNullOrEmpty(newBgmName))
		{
			FadeOutAndStopBgm(fadeDuration);
			return;
		}
		
		var bgmPath = GamePaths.GetValidAudio(GamePaths.BgmDir, newBgmName);

		if (string.IsNullOrEmpty(bgmPath)) return;
		var newBgm = GD.Load<AudioStream>(bgmPath);

		if (ActivePlayer.Playing)
		{
			var fadeOutTime = fadeDuration * 1.5f;
			var fadeInTime = fadeDuration * 0.5f;
			
			var playerToFadeOut = ActivePlayer;
			
			_bgmFadeOutTween = CreateTween();
			_bgmFadeOutTween.TweenProperty(playerToFadeOut, "volume_db", -80.0f, fadeOutTime);
			_bgmFadeOutTween.Finished += () => { playerToFadeOut.Stop(); };
			
			var playerToFadeIn = InactivePlayer;
			playerToFadeIn.Stream = newBgm;
			playerToFadeIn.VolumeDb = -80.0f;
			playerToFadeIn.Play();
			
			_bgmFadeInTween = CreateTween();
			_bgmFadeInTween.TweenProperty(playerToFadeIn, "volume_db", 0.0f, fadeInTime);
		}
		else
		{
			var fadeInTime = fadeDuration * 0.3f;
			BgmPlayerA.Stream = newBgm;
			BgmPlayerA.VolumeDb = -80.0f;
			BgmPlayerA.Play();
				
			_bgmFadeInTween = CreateTween();
			_bgmFadeInTween.TweenProperty(BgmPlayerA, "volume_db", 0.0f, fadeInTime);
		}
	}
	private void UpdateSfx(string newSfxName)
	{
		if (SfxPlayer == null) return;
		
		if (string.IsNullOrEmpty(newSfxName)) return;
		var sfxPath = GamePaths.GetValidAudio(GamePaths.SfxDir, newSfxName);
		var newSfx = GD.Load<AudioStream>(sfxPath);
		
		if (SfxPlayer.GetStreamPlayback() is AudioStreamPlaybackPolyphonic playback)
		{
			playback.PlayStream(newSfx);
		}
		else
		{
			SetupSfxPlayer();
			if (SfxPlayer.GetStreamPlayback() is AudioStreamPlaybackPolyphonic retryPlayback)
			{
				retryPlayback.PlayStream(newSfx);
			}
		}
	}
	private void UpdateVoice(string newVoiceName)
	{
		if (VoicePlayer == null) return;

		if (!VoicePlayer.Playing)
		{
			VoicePlayer.Stop();
			VoicePlayer.Stream = null;
		}
		
		if (_isSkipping) return;
		
		if (string.IsNullOrEmpty(newVoiceName)) return;
		var voicePath = GamePaths.GetValidAudio(GamePaths.VoiceDir, newVoiceName);
		
		var newVoice = GD.Load<AudioStream>(voicePath);
		VoicePlayer.Stream = newVoice;
		VoicePlayer.VolumeDb = 0.0f;
		VoicePlayer.Play();
	}
	
	//Function
	private void FadeOutAndStopBgm(float fadeDuration)
	{
		if (!ActivePlayer.Playing) return;
		
		var playerToFadeOut = ActivePlayer;
		
		_bgmFadeOutTween = CreateTween();
		_bgmFadeOutTween.TweenProperty(playerToFadeOut, "volume_db", -80.0f, fadeDuration);
		_bgmFadeOutTween.Finished += () =>
		{
			playerToFadeOut.Stop();
			playerToFadeOut.Stream = null;
		};
	}
	private bool EvaluateCondition(string conditionId)
	{
		if (string.IsNullOrWhiteSpace(conditionId)) return true;
		
		conditionId = conditionId.Trim();

		if (_conditionLibrary.TryGetValue(conditionId, out string condition))
		{
			conditionId = condition.Trim();
		}

		if (conditionId.Contains(">="))
		{
			var kv = conditionId.Split(">=");
			_gameConditions.TryGetValue(kv[0], out int cur);
			return cur >= int.Parse(kv[1].Trim());
		}

		if (conditionId.Contains("<="))
		{
			var kv = conditionId.Split("<=");
			_gameConditions.TryGetValue(kv[0], out int cur);
			return cur <= int.Parse(kv[1].Trim());
		}
		if (conditionId.Contains("=="))
		{
			var kv = conditionId.Split("==");
			_gameConditions.TryGetValue(kv[0], out int cur);
			return cur == int.Parse(kv[1].Trim());
		}

		if (conditionId.Contains(">"))
		{
			var kv = conditionId.Split(">");
			_gameConditions.TryGetValue(kv[0], out int cur);
			return cur > int.Parse(kv[1].Trim());
		}
		if (conditionId.Contains("<"))
		{
			var kv = conditionId.Split("<");
			_gameConditions.TryGetValue(kv[0], out int cur);
			return cur < int.Parse(kv[1].Trim());
		}
		
		_gameConditions.TryGetValue(conditionId, out int val);
		return val > 0;
	}
	private void ApplyStateChange(string stateChange)
	{
		if (string.IsNullOrWhiteSpace(stateChange)) return;

		if (stateChange.Contains('+'))
		{
			var kv = stateChange.Split('+');
			string key = kv[0].Trim();
			int val = int.Parse(kv[1].Trim());
			_gameConditions[key] = _gameConditions.GetValueOrDefault(key, 0) + val;
		}
		else if (stateChange.Contains('-'))
		{
			var kv = stateChange.Split('-');
			string key = kv[0].Trim();
			int val = int.Parse(kv[1].Trim());
			_gameConditions[key] = _gameConditions.GetValueOrDefault(key, 0) - val;
		}
		else if (stateChange.Contains('='))
		{
			var kv = stateChange.Split('=');
			_gameConditions[kv[0].Trim()] = int.Parse(kv[1].Trim());
		}
	}
	
	//Panel
	
	//Log
	private void PlayReplayVoice(string voicePath)
	{
		if (VoicePlayer == null || string.IsNullOrEmpty(voicePath)) return;
		
		var voiceStream = GD.Load<AudioStream>(voicePath);
		if (voiceStream != null)
		{
			VoicePlayer.Stop();
			VoicePlayer.Stream = voiceStream;
			VoicePlayer.VolumeDb = 0.0f;
			VoicePlayer.Play();
		}
	}
	
	//Config
	private void ApplyConfig(ConfigData config)
	{
		TypeSpeed = config.TextSpeed;
		AutoDelayTime = config.AutoDelayTime;
	}

	//Save/Load
	public SaveData CreateSaveData()
	{
		if (_currentIndex < 0 || _currentIndex >= _dialoguesList.Count) return null;

		var currentLine = _dialoguesList[_currentIndex];

		return new SaveData
		{
			ChapterName = GetChapterTitle(currentLine.Id),
			DialogueId = currentLine.Id,
			GameConditions = new Godot.Collections.Dictionary<string, int>(_gameConditions)
		};
	}
	
	public void CreateAutoSaveData()
	{
		if (_currentIndex < 0 || _currentIndex >= _dialoguesList.Count) return;
		var autoData = CreateSaveData();
		HideAllOverlayPanels();
		SaveSystem.SaveGame(0, autoData, GetViewport());
	}

	public async System.Threading.Tasks.Task CreateAutoSaveDataAsync()
	{
		if (_currentIndex < 0 || _currentIndex >= _dialoguesList.Count) return;
		var autoData = CreateSaveData();
		if (autoData == null) return;
		HideAllOverlayPanels();
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		SaveSystem.SaveGame(0, autoData, GetViewport());
	}

	private void HideAllOverlayPanels()
	{
		if (ConfigManager.Instance?.ConfigPanelUI != null)
		{
			ConfigManager.Instance.ConfigPanelUI.Visible = false;
		}
		if (LogPanelUI != null)
		{
			LogPanelUI.Visible = false;
		}

		if (SaveLoadManager.Instance?.SaveLoadPanelUI != null)
		{
			SaveLoadManager.Instance.SaveLoadPanelUI.Visible = false;
		}
	}

	public void RestoreFromSaveData(SaveData data)
	{
		var targetNode = _dialoguesDict[data.DialogueId];
		
		_gameConditions.Clear();
		if (data.GameConditions != null)
		{
			foreach (var kv in data.GameConditions)
			{
				_gameConditions[kv.Key] = kv.Value;
			}
		}

		int targetIndex = _dialoguesList.IndexOf(targetNode);
		
		LogPanelUI?.ClearLogItems();
		for (int i = 0; i < targetIndex; i++)
		{
			var prevLine = _dialoguesList[i];
			if (!string.IsNullOrEmpty(prevLine.Content))
			{
				string voice = GamePaths.GetValidAudio(GamePaths.VoiceDir, prevLine.Voice);
				LogPanelUI?.AddLogItem(prevLine.Name, prevLine.Content, voice);
			}
		}
		
		RestoreEnvirommentState(targetIndex);
		_currentIndex = targetIndex;
		ExecutePerformance(targetNode);

		_isAuto = false;
		_isSkipping =  false;
		_isMenuOpen = false;
	}

	private void RestoreEnvirommentState(int targetIndex)
	{
		string lastBackground = "";
		string lastBgm = "";

		for (int i = targetIndex; i >= 0; i--)
		{
			var line = _dialoguesList[i];
			if (string.IsNullOrEmpty(lastBackground) && !string.IsNullOrWhiteSpace(line.Background))
				lastBackground = line.Background.Trim();
			if (string.IsNullOrEmpty(lastBgm) && !string.IsNullOrWhiteSpace(line.Bgm))
				lastBgm = line.Bgm.Trim();
			if (string.IsNullOrEmpty(lastBackground) && !string.IsNullOrEmpty(lastBgm))
				break;
		}
		
		if (!string.IsNullOrWhiteSpace(lastBackground)) UpdateImage(lastBackground, ref _currentSceneName, SceneNode, "Scene");
		
		if (!string.IsNullOrWhiteSpace(lastBgm)) UpdateBgm(lastBgm, 1.0f);
	}

	private string GetChapterTitle(string dialogueId)
	{
		if (string.IsNullOrEmpty(dialogueId)) return "";

		foreach (var kv in _chapterLibrary)
		{
			if (dialogueId.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase))
			{
				return kv.Value;
			}
		}
		return "";
	}
}
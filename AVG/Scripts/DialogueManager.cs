using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

namespace AVG.Scripts;

public partial class DialogueManager : Control
{
	public class DialogueData
	{
		public string Id {get; set; }			// 識別碼(除錯/存檔用)
		public string Command { get; set; }		//特殊指令
		public string Name { get; set; }		//說話者名字
		public string Content { get; set; }		//文本內容
		public string Scene { get; set; }		//場景
		public string Cg { get; set; }			//CG插畫
		public string Transition { get; set; }	//轉場特效
		public string LeftChar { get; set; }	//左邊角色立繪檔名
		public string MiddleChar { get; set; }	//中間角色立繪檔名
		public string RightChar { get; set; }	//右邊角色立繪檔名
		public string Bgm { get; set; }			//背景音樂
		public string Voice { get; set; }		//角色配音
		public string Sfx  { get; set; }		//音效
		public string Option01 { get; set; }	//選項一
		public string Option02 { get; set; }	//選項二
		public string Option03 { get; set; }	//選項三
	}
	
	[Export] public Label NameLabel;
	[Export] public RichTextLabel ContentLabel;
	[Export] public float TypeSpeed = 0.05f;
	[Export] public TextureRect SceneNode;
	[Export] public TextureRect CgNode;
	[Export] public TextureRect LeftCharNode;
	[Export] public TextureRect MiddleCharNode;
	[Export] public TextureRect RightCharNode;
	[Export] public AudioStreamPlayer BgmPlayerA;
	[Export] public AudioStreamPlayer BgmPlayerB;
	[Export] public AudioStreamPlayer VoicePlayer;
	[Export] public AudioStreamPlayer SfxPlayer;
	[Export] public VBoxContainer OptionsVbox;
	[Export] public PanelContainer DialoguePanel;
	
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
	
	public override void _Ready()
	{
	
		LoadJson();
		NextLine();
	}
	
	public override void _Input(InputEvent @event)
	{
		if (_isWaitingForOption) return;

		if (!@event.IsActionPressed("ui_accept") &&
		    @event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
		if (_typeTween != null && _typeTween.IsRunning())
		{
			_typeTween.Kill();
			ContentLabel.VisibleRatio = 1.0f;
		}
		else
		{
			NextLine();
		}
	}

	private void LoadJson()
	{
		const string path = "res://Data/Dialogue/DialogueData.json";
		if (!FileAccess.FileExists(path)) return;

		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		var jsonText = file.GetAsText();
		
		var options = new JsonSerializerOptions {PropertyNameCaseInsensitive = true};
		_dialoguesList = JsonSerializer.Deserialize<List<DialogueData>>(jsonText, options);
		
		_dialoguesDict.Clear();
		foreach (var line in _dialoguesList.Where(line => !string.IsNullOrEmpty(line.Id)))
		{
			_dialoguesDict[line.Id] = line;
		}
	}

	private void NextLine()
	{
		_currentIndex++;
		if (_currentIndex >= _dialoguesList.Count)
		{
			GD.Print("End");
			return;
		}
		
		var line =  _dialoguesList[_currentIndex];
		ExecutePerformance(line);
	}
	
	private void ExecutePerformance(DialogueData line)
	{
		//Update
		UpdateImage(line.Scene, ref _currentSceneName, SceneNode, "Scene");
		UpdateImage(line.Cg, ref _currentCgName, CgNode, "CG");
		
		UpdateImage(line.LeftChar, ref _currentCharacterLeft, LeftCharNode, "Character");
		UpdateImage(line.RightChar, ref _currentCharacterRight, RightCharNode, "Character");
		UpdateImage(line.MiddleChar, ref _currentCharacterMiddle, MiddleCharNode, "Character");

		UpdateBgm(line.Bgm, 7.0f, "Audio/BGM");
		UpdateSfx(line.Sfx, "Audio/SFX");
		UpdateVoice(line.Voice, "Audio/Voice");
		
		// If in cutscene or choice, hide the dialogue panel.
		if (string.IsNullOrEmpty(line.Name) && string.IsNullOrEmpty(line.Content))
		{
			DialoguePanel.Visible = false;
			CheckAndCreateOptions(line);
			return;
		}
		
		// Display dialogue panel.
		DialoguePanel.Visible = true;
		NameLabel.Text = line.Name;
		ContentLabel.Text = line.Content;
		
		// Reset and start typewriter animation.
		ContentLabel.VisibleRatio = 0;
		if (_typeTween != null && _typeTween.IsRunning()) _typeTween.Kill();
		_typeTween = CreateTween();
		_typeTween.TweenProperty(ContentLabel, "visible_ratio", 1.0f, line.Content.Length * TypeSpeed);
		
		//Check is next line have options?
		_typeTween.Finished += () => CheckAndCreateOptions(line);
		
		
	}

	private void GoToNode(string nodeId)
	{
		if (!_dialoguesDict.TryGetValue(nodeId, out var targetNode)) return;
		_currentIndex = _dialoguesList.IndexOf(targetNode);
		ExecutePerformance(targetNode);
	}
	
	private void CheckAndCreateOptions(DialogueData line)
	{
		foreach (var child in OptionsVbox.GetChildren())
		{
			child.QueueFree();
		}

		List<string> rawOption = [];
		if (!string.IsNullOrEmpty(line.Option01)) rawOption.Add(line.Option01);
		if (!string.IsNullOrEmpty(line.Option02)) rawOption.Add(line.Option02);
		if (!string.IsNullOrEmpty(line.Option03)) rawOption.Add(line.Option03);

		if (rawOption.Count == 0)
		{
			if (OptionsVbox != null) OptionsVbox.Visible = false;
			_isWaitingForOption = false;
			return;
		}
		
		_isWaitingForOption = true;
		OptionsVbox.Visible = true;
		DialoguePanel.Visible = false;

		foreach (var option in rawOption)
		{
			var parts = option.Split(':');

			var buttonText = parts[0];
			var targetNodeId = parts[1];

			var optionButton = new Button();
			optionButton.Text = buttonText;

			optionButton.Pressed += () =>
			{
				OptionsVbox.Visible = false;
				_isWaitingForOption = false;
				GoToNode(targetNodeId);
			};
			
			OptionsVbox.AddChild(optionButton);
		}
	}
	
	private void UpdateImage(string newImageName, ref string currentImageName, TextureRect imageNode, string folderName)
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
			var basePath = $"res://Assets/{folderName}/{newImageName}";
			var filePath = GetValidImagePath(basePath);

			if (string.IsNullOrEmpty(filePath)) return;
			var newtexture = GD.Load<Texture2D>(filePath);
			imageNode.Texture = newtexture;
			imageNode.Modulate = new Color(1, 1, 1, 1);
			var tween = CreateTween();
			tween.TweenProperty(imageNode, "modulate:a", 1.0f, 0.0f);
		}
	}
	private void UpdateBgm(string newBgmName, float fadeDuration, string folderName)
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
		
		var bgmPath = GetValidAudioPath($"res://Assets/{folderName}/{newBgmName}");

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
	private void UpdateSfx(string newSfxName, string folderName)
	{
		if (SfxPlayer == null) return;
		
		if (string.IsNullOrEmpty(newSfxName)) return;
		var sfxPath = GetValidAudioPath($"res://Assets/{folderName}/{newSfxName}");
		var newSfx = GD.Load<AudioStream>(sfxPath);
		
		if (SfxPlayer.GetStreamPlayback() is AudioStreamPlaybackPolyphonic playback)
		{
			playback.PlayStream(newSfx);
		}
	}
	private void UpdateVoice(string newVoiceName, string folderName)
	{
		if (VoicePlayer == null) return;

		if (!VoicePlayer.Playing)
		{
			VoicePlayer.Stop();
			VoicePlayer.Stream = null;
		}
		
		if (string.IsNullOrEmpty(newVoiceName)) return;
		var voicePath = GetValidAudioPath($"res://Assets/{folderName}/{newVoiceName}");
		
		var newVoice = GD.Load<AudioStream>(voicePath);
		VoicePlayer.Stream = newVoice;
		VoicePlayer.VolumeDb = 0.0f;
		VoicePlayer.Play();
	}
	
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
	
	private static string GetValidImagePath(string imagePath)
	{
		if (FileAccess.FileExists(imagePath + ".png")) return imagePath + ".png";
		if (FileAccess.FileExists(imagePath + ".jpg")) return imagePath + ".jpg";
		
		return null;
	}
	private static string GetValidAudioPath(string audioPath)
	{
		if (FileAccess.FileExists(audioPath + ".wav")) return audioPath + ".wav";
		if (FileAccess.FileExists(audioPath + ".mp3")) return audioPath + ".mp3";
		if (FileAccess.FileExists(audioPath + ".ogg")) return audioPath + ".ogg";
		
		return null;
	}
	
}
﻿using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;
using System.Linq;
using System.Text.RegularExpressions;

namespace UICatalog {
	[ScenarioMetadata (Name: "Editor", Description: "A Terminal.Gui Text Editor via TextView")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Dialogs")]
	[ScenarioCategory ("Text")]
	[ScenarioCategory ("Dialogs")]
	[ScenarioCategory ("TopLevel")]
	class Editor : Scenario {
		private string _fileName = "demo.txt";
		private TextView _textView;
		private bool _saved = true;
		private ScrollBarView _scrollBar;
		private byte [] _originalText;
		private string _textToFind;
		private string _textToReplace;
		private bool _matchCase;
		private bool _matchWholeWord;
		private Window winDialog;
		private TabView _tabView;

		public override void Init (Toplevel top, ColorScheme colorScheme)
		{
			Application.Init ();
			Top = top;
			if (Top == null) {
				Top = Application.Top;
			}

			Win = new Window (_fileName ?? "Untitled") {
				X = 0,
				Y = 1,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				ColorScheme = colorScheme,
			};
			Top.Add (Win);

			_textView = new TextView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				BottomOffset = 1,
				RightOffset = 1
			};

			CreateDemoFile (_fileName);

			LoadFile ();

			Win.Add (_textView);

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_New", "", () => New()),
					new MenuItem ("_Open", "", () => Open()),
					new MenuItem ("_Save", "", () => Save()),
					new MenuItem ("_Save As", "", () => SaveAs()),
					new MenuItem ("_Close", "", () => CloseFile()),
					null,
					new MenuItem ("_Quit", "", () => Quit()),
				}),
				new MenuBarItem ("_Edit", new MenuItem [] {
					new MenuItem ("_Copy", "", () => Copy(),null,null, Key.CtrlMask | Key.C),
					new MenuItem ("C_ut", "", () => Cut(),null,null, Key.CtrlMask | Key.W),
					new MenuItem ("_Paste", "", () => Paste(),null,null, Key.CtrlMask | Key.Y),
					null,
					new MenuItem ("_Find", "", () => Find(),null,null, Key.CtrlMask | Key.S),
					new MenuItem ("Find _Next", "", () => FindNext(),null,null, Key.CtrlMask | Key.ShiftMask | Key.S),
					new MenuItem ("Find P_revious", "", () => FindPrevious(),null,null, Key.CtrlMask | Key.ShiftMask | Key.AltMask | Key.S),
					new MenuItem ("_Replace", "", () => Replace(),null,null, Key.CtrlMask | Key.R),
					new MenuItem ("Replace Ne_xt", "", () => ReplaceNext(),null,null, Key.CtrlMask | Key.ShiftMask | Key.R),
					new MenuItem ("Replace Pre_vious", "", () => ReplacePrevious(),null,null, Key.CtrlMask | Key.ShiftMask | Key.AltMask | Key.R),
					new MenuItem ("Replace _All", "", () => ReplaceAll(),null,null, Key.CtrlMask | Key.ShiftMask | Key.AltMask | Key.A),
					null,
					new MenuItem ("_Select All", "", () => SelectAll(),null,null, Key.CtrlMask | Key.T)
				}),
				new MenuBarItem ("_ScrollBarView", CreateKeepChecked ()),
				new MenuBarItem ("_Cursor", new MenuItem [] {
					new MenuItem ("_Invisible", "", () => SetCursor(CursorVisibility.Invisible)),
					new MenuItem ("_Box", "", () => SetCursor(CursorVisibility.Box)),
					new MenuItem ("_Underline", "", () => SetCursor(CursorVisibility.Underline)),
					new MenuItem ("", "", () => {}, () => { return false; }),
					new MenuItem ("xTerm :", "", () => {}, () => { return false; }),
					new MenuItem ("", "", () => {}, () => { return false; }),
					new MenuItem ("  _Default", "", () => SetCursor(CursorVisibility.Default)),
					new MenuItem ("  _Vertical", "", () => SetCursor(CursorVisibility.Vertical)),
					new MenuItem ("  V_ertical Fix", "", () => SetCursor(CursorVisibility.VerticalFix)),
					new MenuItem ("  B_ox Fix", "", () => SetCursor(CursorVisibility.BoxFix)),
					new MenuItem ("  U_nderline Fix","", () => SetCursor(CursorVisibility.UnderlineFix))
				}),
				new MenuBarItem ("Forma_t", new MenuItem [] {
					CreateWrapChecked (),
					CreateAutocomplete(),
					CreateAllowsTabChecked ()
				}),
				new MenuBarItem ("_Responder", new MenuItem [] {
					CreateCanFocusChecked (),
					CreateEnabledChecked (),
					CreateVisibleChecked ()
				}),
			});
			Top.Add (menu);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.F2, "~F2~ Open", () => Open()),
				new StatusItem(Key.F3, "~F3~ Save", () => Save()),
				new StatusItem(Key.F4, "~F4~ Save As", () => SaveAs()),
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Quit()),
				new StatusItem(Key.Null, $"OS Clipboard IsSupported : {Clipboard.IsSupported}", null)
			});
			Top.Add (statusBar);

			_scrollBar = new ScrollBarView (_textView, true);

			_scrollBar.ChangedPosition += () => {
				_textView.TopRow = _scrollBar.Position;
				if (_textView.TopRow != _scrollBar.Position) {
					_scrollBar.Position = _textView.TopRow;
				}
				_textView.SetNeedsDisplay ();
			};

			_scrollBar.OtherScrollBarView.ChangedPosition += () => {
				_textView.LeftColumn = _scrollBar.OtherScrollBarView.Position;
				if (_textView.LeftColumn != _scrollBar.OtherScrollBarView.Position) {
					_scrollBar.OtherScrollBarView.Position = _textView.LeftColumn;
				}
				_textView.SetNeedsDisplay ();
			};

			_scrollBar.VisibleChanged += () => {
				if (_scrollBar.Visible && _textView.RightOffset == 0) {
					_textView.RightOffset = 1;
				} else if (!_scrollBar.Visible && _textView.RightOffset == 1) {
					_textView.RightOffset = 0;
				}
			};

			_scrollBar.OtherScrollBarView.VisibleChanged += () => {
				if (_scrollBar.OtherScrollBarView.Visible && _textView.BottomOffset == 0) {
					_textView.BottomOffset = 1;
				} else if (!_scrollBar.OtherScrollBarView.Visible && _textView.BottomOffset == 1) {
					_textView.BottomOffset = 0;
				}
			};

			_textView.DrawContent += (e) => {
				_scrollBar.Size = _textView.Lines;
				_scrollBar.Position = _textView.TopRow;
				if (_scrollBar.OtherScrollBarView != null) {
					_scrollBar.OtherScrollBarView.Size = _textView.Maxlength;
					_scrollBar.OtherScrollBarView.Position = _textView.LeftColumn;
				}
				_scrollBar.LayoutSubviews ();
				_scrollBar.Refresh ();
			};

			Win.KeyPress += (e) => {
				var keys = ShortcutHelper.GetModifiersKey (e.KeyEvent);
				if (winDialog != null && (e.KeyEvent.Key == Key.Esc
					|| e.KeyEvent.Key == (Key.Q | Key.CtrlMask))) {
					DisposeWinDialog ();
				} else if (e.KeyEvent.Key == (Key.Q | Key.CtrlMask)) {
					Quit ();
					e.Handled = true;
				} else if (keys == (Key.Tab | Key.CtrlMask)) {
					if (_tabView.SelectedTab == _tabView.Tabs.ElementAt (_tabView.Tabs.Count - 1)) {
						_tabView.SelectedTab = _tabView.Tabs.ElementAt (0);
					} else {
						_tabView.SwitchTabBy (1);
					}
					e.Handled = true;
				} else if (keys == (Key.Tab | Key.CtrlMask | Key.ShiftMask)) {
					if (_tabView.SelectedTab == _tabView.Tabs.ElementAt (0)) {
						_tabView.SelectedTab = _tabView.Tabs.ElementAt (_tabView.Tabs.Count - 1);
					} else {
						_tabView.SwitchTabBy (-1);
					}
					e.Handled = true;
				}
			};
		}

		private void DisposeWinDialog ()
		{
			winDialog.Dispose ();
			Win.Remove (winDialog);
			winDialog = null;
		}

		public override void Setup ()
		{
		}

		private void New (bool checkChanges = true)
		{
			if (checkChanges && !CanCloseFile ()) {
				return;
			}

			Win.Title = "Untitled.txt";
			_fileName = null;
			_originalText = new System.IO.MemoryStream ().ToArray ();
			_textView.Text = _originalText;
		}

		private void LoadFile ()
		{
			if (_fileName != null) {
				// FIXED: BUGBUG: #452 TextView.LoadFile keeps file open and provides no way of closing it
				_textView.LoadFile (_fileName);
				//_textView.Text = System.IO.File.ReadAllText (_fileName);
				_originalText = _textView.Text.ToByteArray ();
				Win.Title = _fileName;
				_saved = true;
			}
		}

		private void Paste ()
		{
			if (_textView != null) {
				_textView.Paste ();
			}
		}

		private void Cut ()
		{
			if (_textView != null) {
				_textView.Cut ();
			}
		}

		private void Copy ()
		{
			if (_textView != null) {
				_textView.Copy ();
			}
		}

		private void SelectAll ()
		{
			_textView.SelectAll ();
		}

		private void Find ()
		{
			CreateFindReplace ();
		}

		private void FindNext ()
		{
			ContinueFind ();
		}

		private void FindPrevious ()
		{
			ContinueFind (false);
		}

		private void ContinueFind (bool next = true, bool replace = false)
		{
			if (!replace && string.IsNullOrEmpty (_textToFind)) {
				Find ();
				return;
			} else if (replace && (string.IsNullOrEmpty (_textToFind)
				|| (winDialog == null && string.IsNullOrEmpty (_textToReplace)))) {
				Replace ();
				return;
			}

			bool found;
			bool gaveFullTurn;

			if (next) {
				if (!replace) {
					found = _textView.FindNextText (_textToFind, out gaveFullTurn, _matchCase, _matchWholeWord);
				} else {
					found = _textView.FindNextText (_textToFind, out gaveFullTurn, _matchCase, _matchWholeWord,
						_textToReplace, true);
				}
			} else {
				if (!replace) {
					found = _textView.FindPreviousText (_textToFind, out gaveFullTurn, _matchCase, _matchWholeWord);
				} else {
					found = _textView.FindPreviousText (_textToFind, out gaveFullTurn, _matchCase, _matchWholeWord,
						_textToReplace, true);
				}
			}
			if (!found) {
				MessageBox.Query ("Find", $"The following specified text was not found: '{_textToFind}'", "Ok");
			} else if (gaveFullTurn) {
				MessageBox.Query ("Find", $"No more occurrences were found for the following specified text: '{_textToFind}'", "Ok");
			}
		}

		private void Replace ()
		{
			CreateFindReplace (false);
		}

		private void ReplaceNext ()
		{
			ContinueFind (true, true);
		}

		private void ReplacePrevious ()
		{
			ContinueFind (false, true);
		}

		private void ReplaceAll ()
		{
			if (string.IsNullOrEmpty (_textToFind) || (string.IsNullOrEmpty (_textToReplace) && winDialog == null)) {
				Replace ();
				return;
			}

			if (_textView.ReplaceAllText (_textToFind, _matchCase, _matchWholeWord, _textToReplace)) {
				MessageBox.Query ("Replace All", $"All occurrences were replaced for the following specified text: '{_textToReplace}'", "Ok");
			} else {
				MessageBox.Query ("Replace All", $"None of the following specified text was found: '{_textToFind}'", "Ok");
			}
		}

		private void SetCursor (CursorVisibility visibility)
		{
			_textView.DesiredCursorVisibility = visibility;
		}

		private bool CanCloseFile ()
		{
			if (_textView.Text == _originalText) {
				return true;
			}

			var r = MessageBox.ErrorQuery ("Save File",
				$"Do you want save changes in {Win.Title}?", "Yes", "No", "Cancel");
			if (r == 0) {
				return Save ();
			} else if (r == 1) {
				return true;
			}

			return false;
		}

		private void Open ()
		{
			if (!CanCloseFile ()) {
				return;
			}
			var aTypes = new List<string> () { ".txt;.bin;.xml;.json", ".txt", ".bin", ".xml", ".*" };
			var d = new OpenDialog ("Open", "Choose the path where to open the file.", aTypes) { AllowsMultipleSelection = false };
			Application.Run (d);

			if (!d.Canceled && d.FilePaths.Count > 0) {
				_fileName = d.FilePaths [0];
				LoadFile ();
			}
		}

		private bool Save ()
		{
			if (_fileName != null) {
				// FIXED: BUGBUG: #279 TextView does not know how to deal with \r\n, only \r 
				// As a result files saved on Windows and then read back will show invalid chars.
				return SaveFile (Win.Title.ToString (), _fileName);
			} else {
				return SaveAs ();
			}
		}

		private bool SaveAs ()
		{
			var aTypes = new List<string> () { ".txt", ".bin", ".xml", ".*" };
			var sd = new SaveDialog ("Save file", "Choose the path where to save the file.", aTypes);
			sd.FilePath = System.IO.Path.Combine (sd.FilePath.ToString (), Win.Title.ToString ());
			Application.Run (sd);

			if (!sd.Canceled) {
				if (System.IO.File.Exists (sd.FilePath.ToString ())) {
					if (MessageBox.Query ("Save File",
						"File already exists. Overwrite any way?", "No", "Ok") == 1) {
						return SaveFile (sd.FileName.ToString (), sd.FilePath.ToString ());
					} else {
						_saved = false;
						return _saved;
					}
				} else {
					return SaveFile (sd.FileName.ToString (), sd.FilePath.ToString ());
				}
			} else {
				_saved = false;
				return _saved;
			}
		}

		private bool SaveFile (string title, string file)
		{
			try {
				Win.Title = title;
				_fileName = file;
				System.IO.File.WriteAllText (_fileName, _textView.Text.ToString ());
				_originalText = _textView.Text.ToByteArray ();
				_saved = true;
				MessageBox.Query ("Save File", "File was successfully saved.", "Ok");

			} catch (Exception ex) {
				MessageBox.ErrorQuery ("Error", ex.Message, "Ok");
				return false;
			}

			return true;
		}

		private void CloseFile ()
		{
			if (!CanCloseFile ()) {
				return;
			}

			try {
				_textView.CloseFile ();
				New (false);
			} catch (Exception ex) {
				MessageBox.ErrorQuery ("Error", ex.Message, "Ok");
			}
		}

		private void Quit ()
		{
			if (!CanCloseFile ()) {
				return;
			}

			Application.RequestStop ();
		}

		private void CreateDemoFile (string fileName)
		{
			var sb = new StringBuilder ();
			// FIXED: BUGBUG: #279 TextView does not know how to deal with \r\n, only \r
			sb.Append ("Hello world.\n");
			sb.Append ("This is a test of the Emergency Broadcast System.\n");

			for (int i = 0; i < 30; i++) {
				sb.Append ($"{i} - This is a test with a very long line and many lines to test the ScrollViewBar against the TextView. - {i}\n");
			}
			var sw = System.IO.File.CreateText (fileName);
			sw.Write (sb.ToString ());
			sw.Close ();
		}

		private MenuItem [] CreateKeepChecked ()
		{
			var item = new MenuItem ();
			item.Title = "Keep Content Always In Viewport";
			item.CheckType |= MenuItemCheckStyle.Checked;
			item.Checked = true;
			item.Action += () => _scrollBar.KeepContentAlwaysInViewport = item.Checked = !item.Checked;

			return new MenuItem [] { item };
		}

		private MenuItem CreateWrapChecked ()
		{
			var item = new MenuItem {
				Title = "Word Wrap"
			};
			item.CheckType |= MenuItemCheckStyle.Checked;
			item.Checked = _textView.WordWrap;
			item.Action += () => {
				_textView.WordWrap = item.Checked = !item.Checked;
				if (_textView.WordWrap) {
					_scrollBar.AutoHideScrollBars = false;
					_scrollBar.OtherScrollBarView.ShowScrollIndicator = false;
					_textView.BottomOffset = 0;
				} else {
					_scrollBar.AutoHideScrollBars = true;
					_textView.BottomOffset = 1;
				}
			};

			return item;
		}

		private MenuItem CreateAutocomplete ()
		{
			var auto = new MenuItem ();
			auto.Title = "Autocomplete";
			auto.CheckType |= MenuItemCheckStyle.Checked;
			auto.Checked = false;
			auto.Action += () => {
				if (auto.Checked = !auto.Checked) {
					// setup autocomplete with all words currently in the editor
					_textView.Autocomplete.AllSuggestions =

					Regex.Matches (_textView.Text.ToString (), "\\w+")
					.Select (s => s.Value)
					.Distinct ().ToList ();
				} else {
					_textView.Autocomplete.AllSuggestions.Clear ();

				}
			};

			return auto;
		}

		private MenuItem CreateAllowsTabChecked ()
		{
			var item = new MenuItem {
				Title = "Allows Tab"
			};
			item.CheckType |= MenuItemCheckStyle.Checked;
			item.Checked = _textView.AllowsTab;
			item.Action += () => {
				_textView.AllowsTab = item.Checked = !item.Checked;
			};

			return item;
		}

		private MenuItem CreateCanFocusChecked ()
		{
			var item = new MenuItem {
				Title = "CanFocus"
			};
			item.CheckType |= MenuItemCheckStyle.Checked;
			item.Checked = _textView.CanFocus;
			item.Action += () => {
				_textView.CanFocus = item.Checked = !item.Checked;
				if (_textView.CanFocus) {
					_textView.SetFocus ();
				}
			};

			return item;
		}

		private MenuItem CreateEnabledChecked ()
		{
			var item = new MenuItem {
				Title = "Enabled"
			};
			item.CheckType |= MenuItemCheckStyle.Checked;
			item.Checked = _textView.Enabled;
			item.Action += () => {
				_textView.Enabled = item.Checked = !item.Checked;
				if (_textView.Enabled) {
					_textView.SetFocus ();
				}
			};

			return item;
		}

		private MenuItem CreateVisibleChecked ()
		{
			var item = new MenuItem {
				Title = "Visible"
			};
			item.CheckType |= MenuItemCheckStyle.Checked;
			item.Checked = _textView.Visible;
			item.Action += () => {
				_textView.Visible = item.Checked = !item.Checked;
				if (_textView.Visible) {
					_textView.SetFocus ();
				}
			};

			return item;
		}

		private void CreateFindReplace (bool isFind = true)
		{
			if (winDialog != null) {
				winDialog.SetFocus ();
				return;
			}

			winDialog = new Window (isFind ? "Find" : "Replace") {
				X = Win.Bounds.Width / 2 - 30,
				Y = Win.Bounds.Height / 2 - 10,
				ColorScheme = Colors.TopLevel
			};
			winDialog.Border.Effect3D = true;

			_tabView = new TabView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};

			_tabView.AddTab (new TabView.Tab ("Find", FindTab ()), isFind);
			var replace = ReplaceTab ();
			_tabView.AddTab (new TabView.Tab ("Replace", replace), !isFind);
			_tabView.SelectedTabChanged += (s, e) => _tabView.SelectedTab.View.FocusFirst ();
			winDialog.Add (_tabView);

			Win.Add (winDialog);

			winDialog.Width = replace.Width + 4;
			winDialog.Height = replace.Height + 4;

			winDialog.SuperView.BringSubviewToFront (winDialog);
			winDialog.SetFocus ();
		}

		private void SetFindText ()
		{
			_textToFind = !_textView.SelectedText.IsEmpty
				? _textView.SelectedText.ToString ()
				: string.IsNullOrEmpty (_textToFind) ? "" : _textToFind;

			_textToReplace = string.IsNullOrEmpty (_textToReplace) ? "" : _textToReplace;
		}

		private View FindTab ()
		{
			var d = new View () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			d.DrawContent += (e) => {
				foreach (var v in d.Subviews) {
					v.SetNeedsDisplay ();
				}
			};

			var lblWidth = "Replace:".Length;

			var label = new Label (0, 1, "Find:") {
				Width = lblWidth,
				TextAlignment = TextAlignment.Right,
				LayoutStyle = LayoutStyle.Computed
			};
			d.Add (label);

			SetFindText ();
			var txtToFind = new TextField (_textToFind) {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = 20
			};
			txtToFind.Enter += (_) => txtToFind.Text = _textToFind;
			d.Add (txtToFind);

			var btnFindNext = new Button ("Find _Next") {
				X = Pos.Right (txtToFind) + 1,
				Y = Pos.Top (label),
				Width = 20,
				Enabled = !txtToFind.Text.IsEmpty,
				TextAlignment = TextAlignment.Centered,
				IsDefault = true
			};
			btnFindNext.Clicked += () => FindNext ();
			d.Add (btnFindNext);

			var btnFindPrevious = new Button ("Find _Previous") {
				X = Pos.Right (txtToFind) + 1,
				Y = Pos.Top (btnFindNext) + 1,
				Width = 20,
				Enabled = !txtToFind.Text.IsEmpty,
				TextAlignment = TextAlignment.Centered
			};
			btnFindPrevious.Clicked += () => FindPrevious ();
			d.Add (btnFindPrevious);

			txtToFind.TextChanged += (e) => {
				_textToFind = txtToFind.Text.ToString ();
				_textView.FindTextChanged ();
				btnFindNext.Enabled = !txtToFind.Text.IsEmpty;
				btnFindPrevious.Enabled = !txtToFind.Text.IsEmpty;
			};

			var btnCancel = new Button ("Cancel") {
				X = Pos.Right (txtToFind) + 1,
				Y = Pos.Top (btnFindPrevious) + 2,
				Width = 20,
				TextAlignment = TextAlignment.Centered
			};
			btnCancel.Clicked += () => {
				DisposeWinDialog ();
			};
			d.Add (btnCancel);

			var ckbMatchCase = new CheckBox ("Match c_ase") {
				X = 0,
				Y = Pos.Top (txtToFind) + 2,
				Checked = _matchCase
			};
			ckbMatchCase.Toggled += (e) => _matchCase = ckbMatchCase.Checked;
			d.Add (ckbMatchCase);

			var ckbMatchWholeWord = new CheckBox ("Match _whole word") {
				X = 0,
				Y = Pos.Top (ckbMatchCase) + 1,
				Checked = _matchWholeWord
			};
			ckbMatchWholeWord.Toggled += (e) => _matchWholeWord = ckbMatchWholeWord.Checked;
			d.Add (ckbMatchWholeWord);

			d.Width = label.Width + txtToFind.Width + btnFindNext.Width + 2;
			d.Height = btnFindNext.Height + btnFindPrevious.Height + btnCancel.Height + 4;

			return d;
		}

		private View ReplaceTab ()
		{
			var d = new View () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			d.DrawContent += (e) => {
				foreach (var v in d.Subviews) {
					v.SetNeedsDisplay ();
				}
			};

			var lblWidth = "Replace:".Length;

			var label = new Label (0, 1, "Find:") {
				Width = lblWidth,
				TextAlignment = TextAlignment.Right,
				LayoutStyle = LayoutStyle.Computed
			};
			d.Add (label);

			SetFindText ();
			var txtToFind = new TextField (_textToFind) {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = 20
			};
			txtToFind.Enter += (_) => txtToFind.Text = _textToFind;
			d.Add (txtToFind);

			var btnFindNext = new Button ("Replace _Next") {
				X = Pos.Right (txtToFind) + 1,
				Y = Pos.Top (label),
				Width = 20,
				Enabled = !txtToFind.Text.IsEmpty,
				TextAlignment = TextAlignment.Centered,
				IsDefault = true
			};
			btnFindNext.Clicked += () => ReplaceNext ();
			d.Add (btnFindNext);

			label = new Label ("Replace:") {
				X = Pos.Left (label),
				Y = Pos.Top (label) + 1,
				Width = lblWidth,
				TextAlignment = TextAlignment.Right
			};
			d.Add (label);

			SetFindText ();
			var txtToReplace = new TextField (_textToReplace) {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = 20
			};
			txtToReplace.TextChanged += (e) => _textToReplace = txtToReplace.Text.ToString ();
			d.Add (txtToReplace);

			var btnFindPrevious = new Button ("Replace _Previous") {
				X = Pos.Right (txtToFind) + 1,
				Y = Pos.Top (btnFindNext) + 1,
				Width = 20,
				Enabled = !txtToFind.Text.IsEmpty,
				TextAlignment = TextAlignment.Centered
			};
			btnFindPrevious.Clicked += () => ReplacePrevious ();
			d.Add (btnFindPrevious);

			var btnReplaceAll = new Button ("Replace _All") {
				X = Pos.Right (txtToFind) + 1,
				Y = Pos.Top (btnFindPrevious) + 1,
				Width = 20,
				Enabled = !txtToFind.Text.IsEmpty,
				TextAlignment = TextAlignment.Centered
			};
			btnReplaceAll.Clicked += () => ReplaceAll ();
			d.Add (btnReplaceAll);

			txtToFind.TextChanged += (e) => {
				_textToFind = txtToFind.Text.ToString ();
				_textView.FindTextChanged ();
				btnFindNext.Enabled = !txtToFind.Text.IsEmpty;
				btnFindPrevious.Enabled = !txtToFind.Text.IsEmpty;
				btnReplaceAll.Enabled = !txtToFind.Text.IsEmpty;
			};

			var btnCancel = new Button ("Cancel") {
				X = Pos.Right (txtToFind) + 1,
				Y = Pos.Top (btnReplaceAll) + 1,
				Width = 20,
				TextAlignment = TextAlignment.Centered
			};
			btnCancel.Clicked += () => {
				DisposeWinDialog ();
			};
			d.Add (btnCancel);

			var ckbMatchCase = new CheckBox ("Match c_ase") {
				X = 0,
				Y = Pos.Top (txtToFind) + 2,
				Checked = _matchCase
			};
			ckbMatchCase.Toggled += (e) => _matchCase = ckbMatchCase.Checked;
			d.Add (ckbMatchCase);

			var ckbMatchWholeWord = new CheckBox ("Match _whole word") {
				X = 0,
				Y = Pos.Top (ckbMatchCase) + 1,
				Checked = _matchWholeWord
			};
			ckbMatchWholeWord.Toggled += (e) => _matchWholeWord = ckbMatchWholeWord.Checked;
			d.Add (ckbMatchWholeWord);

			d.Width = lblWidth + txtToFind.Width + btnFindNext.Width + 2;
			d.Height = btnFindNext.Height + btnFindPrevious.Height + btnCancel.Height + 4;

			return d;
		}
	}
}
